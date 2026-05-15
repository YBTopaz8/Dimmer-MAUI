using System.Collections.Concurrent;

using Dimmer.Utilities.TypeConverters;

namespace Dimmer.Interfaces.Services.Interfaces.FileProcessing;
public class DuplicateFinderService : IDuplicateFinderService
{
    private readonly IRealmFactory _realmFactory;
    private readonly ILogger<DuplicateFinderService> _logger;

    public DuplicateFinderService(IRealmFactory realmFactory,  ILogger<DuplicateFinderService> logger)
    {
        _realmFactory = realmFactory;
        _logger = logger;
    }

    public DuplicateSearchResult FindDuplicates(DuplicateCriteria criteria, IProgress<string>? progress = null)
    {
        if (criteria == DuplicateCriteria.None) return new DuplicateSearchResult();

        using var realm = _realmFactory.GetRealmInstance();
        var allSongsQuery = realm.All<SongModel>();

        _logger.LogInformation("Starting duplicate scan with criteria: {Criteria}", criteria);
        progress?.Report("Loading all songs from the database...");

        var allSongsList = allSongsQuery.AsEnumerable().Select(x => x.ToSongModelView()).ToList();

        var result = new DuplicateSearchResult
        {
            CriteriaUsed = criteria,
            TotalSongsScanned = allSongsList.Count,
            ResultSongs = allSongsList
        };

        progress?.Report($"Grouping {allSongsList.Count} songs by selected criteria...");

        var duplicateGroups = allSongsList
            .AsParallel()
            .GroupBy(song => GenerateGroupingKey(song, criteria))
            .Where(group => group.Count() > 1)
            .ToList();

        _logger.LogInformation("Found {Count} potential duplicate groups.", duplicateGroups.Count);
        progress?.Report($"Found {duplicateGroups.Count} duplicate sets. Processing results...");

        foreach (var group in duplicateGroups)
        {
            if (!group.Any()) continue;

            // 1. Get the best song
            var originalSong = DetermineBestSong(group);

            var set = new DuplicateSetViewModel(originalSong.Title, originalSong.DurationInSeconds);

            // 2. Add Original (Reason is None)
            set.Items.Add(new DuplicateItemViewModel(originalSong, DuplicateStatus.Original, DuplicateReason.None));

            // 3. Add Duplicates and calculate reasons relative to the Original
            foreach (var dupSong in group.Where(s => s.Id != originalSong.Id))
            {
                DuplicateReason reason = DuplicateReason.None;

                // Metadata comparisons
                if (string.Equals(dupSong.Title, originalSong.Title, StringComparison.OrdinalIgnoreCase))
                    reason |= DuplicateReason.SameTitle;
                if (string.Equals(dupSong.ArtistName, originalSong.ArtistName, StringComparison.OrdinalIgnoreCase))
                    reason |= DuplicateReason.SameArtist;
                if (string.Equals(dupSong.AlbumName, originalSong.AlbumName, StringComparison.OrdinalIgnoreCase))
                    reason |= DuplicateReason.SameAlbum;

                // Audio property comparisons (Allow 2 seconds of wiggle room for duration)
                if (Math.Abs(dupSong.DurationInSeconds - originalSong.DurationInSeconds) <= 2.0)
                    reason |= DuplicateReason.SimilarDuration;
                if (dupSong.FileSize == originalSong.FileSize)
                    reason |= DuplicateReason.SameFileSize;

                // Quality & Location differences
                if (dupSong.BitRate < originalSong.BitRate)
                    reason |= DuplicateReason.LowerBitrate;

                var origExt = Path.GetExtension(originalSong.FilePath);
                var dupExt = Path.GetExtension(dupSong.FilePath);
                if (!string.Equals(origExt, dupExt, StringComparison.OrdinalIgnoreCase))
                    reason |= DuplicateReason.DifferentFileFormat;

                var origFolder = Path.GetDirectoryName(originalSong.FilePath);
                var dupFolder = Path.GetDirectoryName(dupSong.FilePath);
                if (!string.Equals(origFolder, dupFolder, StringComparison.OrdinalIgnoreCase))
                    reason |= DuplicateReason.DifferentFolder;

                // Add the duplicate item with calculated reasons
                set.Items.Add(new DuplicateItemViewModel(dupSong, DuplicateStatus.Duplicate, reason));
            }

            result.DuplicateSets.Add(set);
        }

        progress?.Report("Scan complete!");
        return result;
    }

    /// <summary>
    /// Evaluates a group of duplicate songs and returns the one deemed "best" based on a scoring system.
    /// </summary>
    /// <param name="duplicateGroup">A collection of songs that are considered duplicates of each other.</param>
    /// <returns>The SongModelView with the highest score.</returns>
    private SongModelView DetermineBestSong(IEnumerable<SongModelView> duplicateGroup)
    {
        SongModelView? bestSong = null;
        double highestScore = double.MinValue; // Use double to support fine-grained calculation

        foreach (var song in duplicateGroup)
        {
            double currentScore = 0;

            // ==========================================
            // 1. CRITICAL INTEGRITY (Dealbreakers)
            // ==========================================
            if (!song.IsFileExists)
            {
                // Heavily penalize missing files so they are targeted for deletion
                currentScore -= 50000;
            }

            // ==========================================
            // 2. USER ENGAGEMENT & DATA PRESERVATION
            // Extremely important: Deleting songs in playlists or with user notes ruins user data.
            // ==========================================
            currentScore += song.PlayCount * 50;
            currentScore += song.PlayCompletedCount * 100; // Completed plays are higher value

            if (song.IsFavorite) currentScore += 1000;
            currentScore += song.Rating * 200; // 5 stars = +1000 pts

            // Huge bonus if this specific file is attached to custom playlists or user notes
            if (song.PlaylistsHavingSong?.Any() == true)
                currentScore += song.PlaylistsHavingSong.Count * 2500;

            if (song.UserNoteAggregatedCol?.Any() == true)
                currentScore += song.UserNoteAggregatedCol.Count * 1500;

            // ==========================================
            // 3. AUDIO QUALITY
            // ==========================================
            // Bitrate: e.g., 320kbps adds 320 points. 
            // If Bitrate is stored as raw bps (320000), divide by 1000. Adjust based on your DB values.
            currentScore += song.BitRate > 1000 ? (song.BitRate / 1000.0) : song.BitRate;

            // BitDepth: 24-bit adds 600 pts, 16-bit adds 400 pts
            currentScore += song.BitDepth * 25;

            // Preferred Lossless extensions
            var extension = Path.GetExtension(song.FilePath)?.ToLowerInvariant();
            if (extension is ".flac" or ".wav" or ".alac")
            {
                currentScore += 2000;
            }

            // ==========================================
            // 4. METADATA COMPLETENESS
            // ==========================================
            // Synced lyrics are very valuable/hard to get
            if (song.HasSyncedLyrics || song.EmbeddedSync?.Any() == true) currentScore += 1200;
            else if (song.HasLyrics) currentScore += 400;

            if (!string.IsNullOrEmpty(song.CoverImagePath)) currentScore += 500;
            if (!string.IsNullOrEmpty(song.AlbumName)) currentScore += 100;
            if (!string.IsNullOrEmpty(song.GenreName)) currentScore += 100;
            if (song.ReleaseYear > 0) currentScore += 100;

            // ==========================================
            // 5. FILE PATH HEURISTICS
            // ==========================================
            if (!string.IsNullOrEmpty(song.FilePath))
            {
                // Penalize temporary/download directories
                if (song.FilePath.Contains("Downloads", StringComparison.OrdinalIgnoreCase) ||
                    song.FilePath.Contains("\\Temp\\", StringComparison.OrdinalIgnoreCase) ||
                    song.FilePath.Contains("/temp/", StringComparison.OrdinalIgnoreCase))
                {
                    currentScore -= 3000;
                }
            }

            // ==========================================
            // 6. TIE-BREAKER: File Age
            // ==========================================
            if (song.DateCreated.HasValue)
            {
                // Older files get slightly higher points (presumed original import)
                var daysOld = (DateTimeOffset.UtcNow - song.DateCreated.Value).TotalDays;
                currentScore += Math.Min(daysOld / 10.0, 200); // Cap bonus at 200 pts max
            }

            // Check if this song beats the current leader
            if (currentScore > highestScore)
            {
                highestScore = currentScore;
                bestSong = song;
            }
        }

        return bestSong ?? duplicateGroup.First();
    }
    public async Task<int> ResolveDuplicatesAsync(IEnumerable<DuplicateItemViewModel> itemsToDelete)
    {
        var songIdsToDelete = itemsToDelete.Select(i => i.Song.Id).ToList();
        var filePathsToDelete = itemsToDelete.Select(i => i.Song.FilePath).ToList();
        int deletedCount = 0;

        if (songIdsToDelete.Count == 0)
            return 0;


        using var realm = _realmFactory.GetRealmInstance();
        await realm.WriteAsync(() =>
        {
            foreach (var id in songIdsToDelete)
            {
                var songInDb = realm.Find<SongModel>(id);
                if (songInDb != null)
                {
                    realm.Remove(songInDb);
                    deletedCount++;
                }
            }
        });
        _logger.LogInformation("Deleted {Count} song entries from the database.", deletedCount);


        foreach (var path in filePathsToDelete)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                    _logger.LogTrace("Deleted file: {FilePath}", path);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete file: {FilePath}. Please remove it manually.", path);

            }
        }

        return deletedCount;
    }


    public async Task<LibraryValidationResult> ValidateMultipleFilesPresenceAsync(IEnumerable<SongModelView>? allSongs)
    {
        if (allSongs == null || allSongs.Count() == 0)
        {
            return new LibraryValidationResult
            {
                ScannedCount = 0,
                MissingSongs = Enumerable.Empty<SongModelView>().ToList()
            };
        }

        _logger.LogInformation("Starting file presence validation for {Count} songs.", allSongs.Count());


        var missingSongs = new ConcurrentBag<SongModelView>();


        await Parallel.ForEachAsync(allSongs, (song, cancellationToken) =>
        {
            if (string.IsNullOrEmpty(song.FilePath) || !File.Exists(song.FilePath))
            {
                missingSongs.Add(song);
            }

            return new ValueTask();
        });

        _logger.LogInformation("Validation complete. Found {Count} missing files.", missingSongs.Count);


        var missingSongViews = missingSongs.ToList();

        return new LibraryValidationResult
        {
            ScannedCount = allSongs.Count(),
            MissingSongs = missingSongViews
        };
    }

    public async Task RemoveSongsFromDbAsync(IEnumerable<ObjectId> songIds)
    {
        if (!songIds.Any())
            return;

        using var realm = _realmFactory.GetRealmInstance();
        await realm.WriteAsync(() =>
        {
            foreach (var id in songIds)
            {
                var songInDb = realm.Find<SongModel>(id);
                if (songInDb != null)
                {
                    realm.Remove(songInDb);
                }
            }
        });
        _logger.LogInformation("Permanently removed {Count} song entries from the database.", songIds.Count());
    }

    public async Task<LibraryReconciliationResult> ReconcileLibraryAsync(IEnumerable<SongModelView> allSongs)
    {
        var allSongsList = allSongs.ToList();
        _logger.LogInformation("Starting library reconciliation for {Count} songs.", allSongsList.Count);

        var migratedDetails = new List<MigrationDetail>();
        var unresolvedMissing = new List<SongModelView>();


        var existingSongs = new List<SongModelView>();
        var potentialGhosts = new List<SongModelView>();
        foreach (var song in allSongsList)
        {
            if (!string.IsNullOrEmpty(song.FilePath) && File.Exists(song.FilePath))
            {
                existingSongs.Add(song);
            }
            else
            {
                potentialGhosts.Add(song);
            }
        }

        if (potentialGhosts.Count == 0)
        {
            _logger.LogInformation("Reconciliation complete. No missing files found.");
            return new LibraryReconciliationResult { ScannedCount = allSongsList.Count };
        }

        var existingSongsLookup = existingSongs
            .ToLookup(s => $"{s.Title.Trim()}|{s.DurationInSeconds}");

        using var realm = _realmFactory.GetRealmInstance();


        foreach (var ghostSong in potentialGhosts)
        {
            var songIdentityKey = $"{ghostSong.Title.Trim()}|{ghostSong.DurationInSeconds}";


            if (existingSongsLookup.Contains(songIdentityKey))
            {




                var replacementSong = existingSongsLookup[songIdentityKey].First();

                await realm.WriteAsync(() =>
                {
                    var ghostRealmObj = realm.Find<SongModel>(ghostSong.Id);
                    var replacementRealmObj = realm.Find<SongModel>(replacementSong.Id);

                    if (ghostRealmObj == null || replacementRealmObj == null)
                        return;

                    // *** CALL THE NEW MERGE HELPER ***
                    MergeSongData(replacementRealmObj, ghostRealmObj);

                    // Remove the ghost record after its data has been safely merged.
                    realm.Remove(ghostRealmObj);
                });
                var updatedReplacementView = realm.Find<SongModel>(replacementSong.Id).ToSongModelView();
                migratedDetails.Add(new MigrationDetail(ghostSong, updatedReplacementView));
            }
            else
            {

                unresolvedMissing.Add(ghostSong);
            }
        }



        return new LibraryReconciliationResult
        {
            ScannedCount = allSongsList.Count,
            MigratedSongs = migratedDetails,
            UnresolvedMissingSongs = unresolvedMissing
        };
    }

    private string GenerateGroupingKey(SongModelView targetSong, DuplicateCriteria criteria)
    {
        var keyParts = new List<string>();

        if (criteria.HasFlag(DuplicateCriteria.Title))
        {
            // Normalize the title for better matching
            keyParts.Add(targetSong.Title?.Trim().ToLowerInvariant() ?? string.Empty);
        }
        if (criteria.HasFlag(DuplicateCriteria.Artist))
        {
            // Normalize artist name, maybe even sort multiple artists
            keyParts.Add(targetSong.ArtistName?.Trim().ToLowerInvariant() ?? string.Empty);
        }
        if (criteria.HasFlag(DuplicateCriteria.Album))
        {
            keyParts.Add(targetSong.AlbumName?.Trim().ToLowerInvariant() ?? string.Empty);
        }
        if (criteria.HasFlag(DuplicateCriteria.Duration))
        {
            // Round duration to the nearest second to catch minor discrepancies
            keyParts.Add(Math.Round(targetSong.DurationInSeconds).ToString());
        }
        if (criteria.HasFlag(DuplicateCriteria.FileSize))
        {
            keyParts.Add(targetSong.FileSize.ToString());
        }

        // Join the parts with a separator that is unlikely to appear in the data
        return string.Join("|||", keyParts);
    }

    private void MergeSongData(SongModel survivor, SongModel ghost)
    {
        // Merge lists by adding only unique items.
        foreach (var item in ghost.PlayHistory.ToList())
            survivor.PlayHistory.Add(item);
        foreach (var item in ghost.UserNotes.ToList())
            survivor.UserNotes.Add(item);
        // Be careful with Tags and Artists to avoid adding duplicates if they already exist
        foreach (var tag in ghost.Tags.ToList())
            if (!survivor.Tags.Contains(tag))
                survivor.Tags.Add(tag);
        foreach (var artist in ghost.ArtistToSong.ToList())
            if (!survivor.ArtistToSong.Contains(artist))
                survivor.ArtistToSong.Add(artist);

        // Merge simple properties, preferring data from the ghost if the survivor's is missing.
        survivor.Rating = survivor.Rating > ghost.Rating ? survivor.Rating : ghost.Rating; // Keep the highest rating
        survivor.IsFavorite = survivor.IsFavorite || ghost.IsFavorite; // If either is a favorite, it stays a favorite
        survivor.UnSyncLyrics = string.IsNullOrWhiteSpace(survivor.UnSyncLyrics) ? ghost.UnSyncLyrics : survivor.UnSyncLyrics;
        survivor.SyncLyrics = string.IsNullOrWhiteSpace(survivor.SyncLyrics) ? ghost.SyncLyrics : survivor.SyncLyrics;
        survivor.HasLyrics = survivor.HasLyrics || ghost.HasLyrics;
        survivor.HasSyncedLyrics = survivor.HasSyncedLyrics || ghost.HasSyncedLyrics;
        survivor.CoverImagePath = string.IsNullOrWhiteSpace(survivor.CoverImagePath) ? ghost.CoverImagePath : survivor.CoverImagePath;
        survivor.OtherArtistsName = string.IsNullOrWhiteSpace(survivor.OtherArtistsName) ? ghost.OtherArtistsName : survivor.OtherArtistsName;
        survivor.Genre = survivor.Genre is null ? ghost.Genre : survivor.Genre;
        survivor.BitRate = survivor.BitRate > ghost.BitRate ? survivor.BitRate : ghost.BitRate; // Keep the highest bitrate 
        survivor.GenreName = string.IsNullOrWhiteSpace(survivor.GenreName) ? ghost.GenreName : survivor.GenreName;
        survivor.TrackNumber = survivor.TrackNumber != 0 ? survivor.TrackNumber : ghost.TrackNumber;
        survivor.DiscNumber = survivor.DiscNumber != 0 ? survivor.DiscNumber : ghost.DiscNumber;
        survivor.ReleaseYear = survivor.ReleaseYear != 0 ? survivor.ReleaseYear : ghost.ReleaseYear;
        survivor.FileFormat = string.IsNullOrWhiteSpace(survivor.FileFormat) ? ghost.FileFormat : survivor.FileFormat;
        survivor.FilePath = string.IsNullOrWhiteSpace(survivor.FilePath) ? ghost.FilePath : survivor.FilePath;
        survivor.FileSize = survivor.FileSize != 0 ? ghost.FileSize : survivor.FileSize;
        survivor.RankInAlbum = survivor.RankInAlbum > 0 ? survivor.RankInAlbum : ghost.RankInAlbum;
        survivor.RankInArtist = survivor.RankInArtist > 0 ? survivor.RankInArtist : ghost.RankInArtist;
        survivor.GlobalRank = survivor.GlobalRank > 0 ? survivor.GlobalRank : ghost.GlobalRank;
        survivor.IsHidden = survivor.IsHidden && ghost.IsHidden; // Only hidden if both are hidden
        survivor.Description = string.IsNullOrWhiteSpace(survivor.Description) ? ghost.Description : survivor.Description;
        survivor.Lyricist = string.IsNullOrWhiteSpace(survivor.Lyricist) ? ghost.Lyricist : survivor.Lyricist;
        survivor.Composer = string.IsNullOrWhiteSpace(survivor.Composer) ? ghost.Composer : survivor.Composer;
        survivor.Conductor = string.IsNullOrWhiteSpace(survivor.Conductor) ? ghost.Conductor : survivor.Conductor;
        survivor.Language = string.IsNullOrWhiteSpace(survivor.Language) ? ghost.Language : survivor.Language;
        survivor.IsInstrumental ??= ghost.IsInstrumental;


        // Ensure the survivor has the earliest creation date.
        if (ghost.DateCreated.HasValue && (!survivor.DateCreated.HasValue || ghost.DateCreated < survivor.DateCreated))
        {
            survivor.DateCreated = ghost.DateCreated;
        }
    }

    public DuplicateSearchResult FindDuplicatesForSong(SongModelView targetSong, DuplicateCriteria criteria)
    {
        if (criteria == DuplicateCriteria.None)
        {
            return new DuplicateSearchResult();
        }

        using var realm = _realmFactory.GetRealmInstance();
        var allSongsQuery = realm.All<SongModel>();

        IQueryable<SongModel> potentialDuplicatesQuery = realm.All<SongModel>()
                                                         .Where(s => s.Id != targetSong.Id); // Exclude the song itself

        if (criteria.HasFlag(DuplicateCriteria.Title))
        {
            potentialDuplicatesQuery = potentialDuplicatesQuery.Where(s => s.Title == targetSong.Title);
        }
        if (criteria.HasFlag(DuplicateCriteria.Artist))
        {
            potentialDuplicatesQuery = potentialDuplicatesQuery.Where(s => s.ArtistName == targetSong.ArtistName);
        }
        if (criteria.HasFlag(DuplicateCriteria.Album))
        {
            potentialDuplicatesQuery = potentialDuplicatesQuery.Where(s => s.AlbumName == targetSong.AlbumName);
        }
        if (criteria.HasFlag(DuplicateCriteria.Duration))
        {
            const double durationTolerance = 0.5; // Find songs within +/- 0.5 seconds
            double lowerBound = targetSong.DurationInSeconds - durationTolerance;
            double upperBound = targetSong.DurationInSeconds + durationTolerance;

            potentialDuplicatesQuery = potentialDuplicatesQuery
                .Where(s => s.DurationInSeconds > lowerBound && s.DurationInSeconds < upperBound);
        }


        var duplicates = potentialDuplicatesQuery.AsEnumerable().Select(x=>x.ToSongModelView());

        if (duplicates.Count() == 0)
        {
            return new DuplicateSearchResult(); // No duplicates found
        }

        // Since we found duplicates, we need to create a single DuplicateSet
        var allItemsInSet = new List<SongModelView> { targetSong };
        allItemsInSet.AddRange(duplicates);

        // Use your existing logic to find the best one to keep
        var originalSong = DetermineBestSong(allItemsInSet);

        var set = new DuplicateSetViewModel(originalSong.Title, originalSong.DurationInSeconds);
        set.Items.Add(new DuplicateItemViewModel(originalSong, DuplicateStatus.Original));

        foreach (var duplicateSong in allItemsInSet.Where(s => s.Id != originalSong.Id))
        {
            set.Items.Add(new DuplicateItemViewModel(duplicateSong, DuplicateStatus.Duplicate));
        }

        return new DuplicateSearchResult
        {
            CriteriaUsed = criteria,
            TotalSongsScanned = duplicates.Count() + 1, // The found songs + the original
            DuplicateSets = new List<DuplicateSetViewModel> { set }
        };
    }

}
[Flags]
public enum DuplicateCriteria
{
    None = 0,
    Title = 1,
    Artist = 2,
    Album = 4,
    Duration = 8,
    FileSize = 16,
    // Add any other SongModel properties you want to compare
    AudioFingerprint = 32,
    // --- Common Presets ---
    Default = Title | Duration,
    Strict = Title | Artist | Album | Duration,
    AudioSignature = Duration | FileSize // A proxy for acoustic fingerprinting
}

public class DuplicateSearchResult
{
    public DuplicateCriteria CriteriaUsed { get; set; }
    public List<DuplicateSetViewModel> DuplicateSets { get; set; } = new();
    public int TotalSongsScanned { get; set; }
    public List<SongModelView?>? ResultSongs { get; set; } = null;
}