using Dimmer.Data.ModelView.LibSanityModels;

using DynamicData;

using System.Collections.Concurrent;

namespace Dimmer.Interfaces.Services.Interfaces.FileProcessing;
public class DuplicateFinderService : IDuplicateFinderService
{
    private readonly IRealmFactory _realmFactory;
    private readonly IMapper _mapper;
    private readonly ILogger<DuplicateFinderService> _logger;

    public DuplicateFinderService(IRealmFactory realmFactory, IMapper mapper, ILogger<DuplicateFinderService> logger)
    {
        _realmFactory = realmFactory;
        _mapper = mapper;
        _logger = logger;
    }

    public DuplicateSearchResult FindDuplicates(DuplicateCriteria criteria, IProgress<string>? progress = null)
    {
        if (criteria == DuplicateCriteria.None)
        {
            return new DuplicateSearchResult(); // Nothing to do
        }

        using var realm = _realmFactory.GetRealmInstance();
        // It's better to work with IQueryable as long as possible
        var allSongsQuery = realm.All<SongModel>();

        _logger.LogInformation("Starting duplicate scan with criteria: {Criteria}", criteria);
        progress?.Report("Loading all songs from the database...");

        // Materialize the list now that we need to process it
        List<SongModelView>? allSongsList = _mapper.Map<List<SongModelView>>(allSongsQuery.ToList());

        var result = new DuplicateSearchResult
        {
            CriteriaUsed = criteria,
            TotalSongsScanned = allSongsList.Count,
            ResultSongs = allSongsList
        };

        progress?.Report($"Grouping {allSongsList.Count} songs by selected criteria...");

        // The magic happens here! We group by our dynamically generated key.
        var duplicateGroups = allSongsList
            .AsParallel() // Use PLINQ for performance on large libraries
            .GroupBy(song => GenerateGroupingKey(song, criteria))
            .Where(group => group.Count() > 1)
            .ToList();

        _logger.LogInformation("Found {Count} potential duplicate groups.", duplicateGroups.Count);
        progress?.Report($"Found {duplicateGroups.Count} duplicate sets. Processing results...");

        foreach (var group in duplicateGroups)
        {
            if (!group.Any()) continue; // Safety check

            // --- REFACTORED LOGIC ---
            // Instead of a fixed sort, we now determine the best song based on a score.
            var originalSong = DetermineBestSong(group);

            var set = new DuplicateSetViewModel(
                originalSong.Title,
                originalSong.DurationInSeconds
            );

            // Add our designated "Original" to the set
            set.Items.Add(new DuplicateItemViewModel(_mapper.Map<SongModelView>(originalSong), DuplicateStatus.Original));

            // Add all other songs from the group as "Duplicates"
            foreach (var duplicateSongModel in group.Where(s => s.Id != originalSong.Id))
            {
                set.Items.Add(new DuplicateItemViewModel(_mapper.Map<SongModelView>(duplicateSongModel), DuplicateStatus.Duplicate));
            }
            // --- END REFACTORED LOGIC ---

            result.DuplicateSets.Add(set);
        }

        _logger.LogInformation("Finished duplicate scan. Found {Count} sets.", result.DuplicateSets.Count);
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
        int highestScore = -1;

        foreach (var song in duplicateGroup)
        {
            int currentScore = 0;

            // --- HEURISTICS & SCORING ---
            // You can easily add, remove, or change the weights of these criteria.

            // 1. Bitrate: Higher is better. Give a significant boost for high quality.
            if (song.BitRate.HasValue)
            {
                currentScore += song.BitRate.Value; // e.g., 320kbps adds 320 points
            }

            // 2. File Format: Prefer lossless formats.
            var extension = Path.GetExtension(song.FilePath)?.ToLowerInvariant();
            if (extension == ".flac" || extension == ".wav" || extension == ".alac")
            {
                currentScore += 500; // Major bonus for being lossless
            }

            // 3. Metadata Completeness: Reward songs with more complete data.
            if (song.HasLyrics) currentScore += 50;
            if (song.IsFavorite) currentScore += 100;
            if (song.Rating > 0) currentScore += song.Rating * 20; // A 5-star rating adds 100 points
            if (!string.IsNullOrEmpty(song.CoverImagePath)) currentScore += 75;

            // 4. File Path: Prefer files in a "canonical" or non-temporary location.
            // This is subjective and should be configured based on your library structure.
            if (song.FilePath.Contains(@"D:\Music\Library", StringComparison.OrdinalIgnoreCase))
            {
                currentScore += 200; // Bonus for being in a preferred folder
            }
            if (song.FilePath.Contains(@"Downloads", StringComparison.OrdinalIgnoreCase) || song.FilePath.Contains(@"\Temp\", StringComparison.OrdinalIgnoreCase))
            {
                currentScore -= 300; // Penalty for being in a temporary/download folder
            }

            // 5. File Age: Older might mean it's the original import.
            // This can be a good tie-breaker.
            if (song.DateCreated.HasValue)
            {
                // Give a small bonus for age to act as a tie-breaker
                // The older the file, the smaller the number of days, the higher the score.
                currentScore += (int)(DateTime.UtcNow - song.DateCreated.Value).TotalDays / 100;
            }

            // Compare with the current best song
            if (currentScore > highestScore)
            {
                highestScore = currentScore;
                bestSong = song;
            }
        }

        // Fallback to the first item if for some reason no best song was found (shouldn't happen)
        return bestSong ?? duplicateGroup.First();
    }

    public async Task<int> ResolveDuplicatesAsync(IEnumerable<DuplicateItemViewModel> itemsToDelete)
    {
        var songIdsToDelete = itemsToDelete.Select(i => i.Song.Id).ToList();
        var filePathsToDelete = itemsToDelete.Select(i => i.Song.FilePath).ToList();
        int deletedCount = 0;

        if (songIdsToDelete.Count==0)
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

    
    public async Task<LibraryValidationResult> ValidateMultipleFilesPresenceAsync(IList<SongModelView>? allSongs)
    {
        if (allSongs == null || allSongs.Count == 0)
        {
            return new LibraryValidationResult
            {
                ScannedCount = 0,
                MissingSongs = Enumerable.Empty<SongModelView>().ToList()
            };
        }

        _logger.LogInformation("Starting file presence validation for {Count} songs.", allSongs.Count);

       
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

       
        var missingSongViews =missingSongs.ToList();

        return new LibraryValidationResult
        {
            ScannedCount = allSongs.Count,
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

        if (potentialGhosts.Count==0)
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
                var updatedReplacementView = _mapper.Map<SongModelView>(realm.Find<SongModel>(replacementSong.Id));
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
        survivor.GenreName = string.IsNullOrWhiteSpace(survivor.GenreName)? ghost.GenreName: survivor.GenreName; 
        survivor.TrackNumber = survivor.TrackNumber != 0 ? survivor.TrackNumber : ghost.TrackNumber;
        survivor.DiscNumber = survivor.DiscNumber != 0 ? survivor.DiscNumber : ghost.DiscNumber;
        survivor.ReleaseYear = survivor.ReleaseYear != 0 ? survivor.ReleaseYear : ghost.ReleaseYear;
        survivor.FileFormat = string.IsNullOrWhiteSpace(survivor.FileFormat) ? ghost.FileFormat: survivor.FileFormat;
        survivor.FilePath = string.IsNullOrWhiteSpace(survivor.FilePath) ? ghost.FilePath : survivor.FilePath;
        survivor.FileSize = survivor.FileSize != 0 ? ghost.FileSize : survivor.FileSize;
        survivor.RankInAlbum = survivor.RankInAlbum> 0 ? survivor.RankInAlbum: ghost.RankInAlbum;
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


        var duplicates = _mapper.Map<List<SongModelView>>(potentialDuplicatesQuery.ToList());

        if (duplicates.Count == 0)
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
            TotalSongsScanned = duplicates.Count + 1, // The found songs + the original
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
    public List<SongModelView>? ResultSongs { get; set; } = null;
}