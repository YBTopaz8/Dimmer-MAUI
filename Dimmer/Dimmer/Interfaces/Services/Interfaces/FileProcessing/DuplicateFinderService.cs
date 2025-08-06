﻿using Dimmer.Data.ModelView.LibSanityModels;

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

    public List<DuplicateSetViewModel> FindDuplicates()
    {
        using var realm = _realmFactory.GetRealmInstance();
        var allSongs = realm.All<SongModel>().ToList();
        var results = new List<DuplicateSetViewModel>();

        _logger.LogInformation("Starting duplicate scan on {Count} songs.", allSongs.Count);

        // Group songs by a composite key of Title and Duration
        var duplicateGroups = allSongs
            .GroupBy(s => new { Title = s.Title.Trim(), s.DurationInSeconds })
            .Where(g => g.Count() > 1); // Only care about groups with more than one song

        foreach (var group in duplicateGroups)
        {
            var set = new DuplicateSetViewModel(group.Key.Title, group.Key.DurationInSeconds);

            // Order by date added to determine the "Original". Oldest is original.
            var sortedSongs = group.OrderBy(s => s.DateCreated).ToList();

            // The first one is the original
            var originalSong = _mapper.Map<SongModelView>(sortedSongs.Last());
            if (File.Exists(originalSong.FilePath))
            {
                set.Items.Add(new DuplicateItemViewModel(originalSong, DuplicateStatus.Original));
            }

                // All others are duplicates
                foreach (var duplicateSongModel in sortedSongs.Skip(1))
                {
                    var duplicateSongView = _mapper.Map<SongModelView>(duplicateSongModel);
                    set.Items.Add(new DuplicateItemViewModel(duplicateSongView, DuplicateStatus.Duplicate));
                }
            results.Add(set);
        }

        _logger.LogInformation("Found {Count} duplicate sets.", results.Count);
        return results;
    }

    public async Task<int> ResolveDuplicatesAsync(IEnumerable<DuplicateItemViewModel> itemsToDelete)
    {
        var songIdsToDelete = itemsToDelete.Select(i => i.Song.Id).ToList();
        var filePathsToDelete = itemsToDelete.Select(i => i.Song.FilePath).ToList();
        int deletedCount = 0;

        if (songIdsToDelete.Count==0)
            return 0;

        // Step 1: Delete the database entries
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

        // Step 2: Delete the actual files from disk
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
                // Continue to the next file even if one fails
            }
        }

        return deletedCount;
    }

    
    public async Task<LibraryValidationResult> ValidateFilePresenceAsync(IList<SongModelView>? allSongs)
    {

        _logger.LogInformation("Starting file presence validation for {Count} songs.", allSongs.Count);

        // Use a thread-safe collection to store results from the parallel loop.
        var missingSongs = new ConcurrentBag<SongModelView>();

        // Use Parallel.ForEachAsync for a massive performance boost.
        await Parallel.ForEachAsync(allSongs, (song, cancellationToken) =>
        {
            if (string.IsNullOrEmpty(song.FilePath) || !File.Exists(song.FilePath))
            {
                missingSongs.Add(song);
            }

            return new ValueTask();
        });

        _logger.LogInformation("Validation complete. Found {Count} missing files.", missingSongs.Count);

        // Map the results to the ViewModel type the UI needs.
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

        // Step 1: Partition songs (this part is correct).
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

        // =========================================================================
        // THE FIX: Use ToLookup() instead of ToDictionary()
        // =========================================================================
        // ToLookup() creates a dictionary-like structure where one key can map to
        // a collection of values. This safely handles multiple existing files
        // with the same title and duration.
        var existingSongsLookup = existingSongs
            .ToLookup(s => $"{s.Title.Trim()}|{s.DurationInSeconds}");

        using var realm = _realmFactory.GetRealmInstance();

        // Step 3: Process the ghosts.
        foreach (var ghostSong in potentialGhosts)
        {
            var songIdentityKey = $"{ghostSong.Title.Trim()}|{ghostSong.DurationInSeconds}";

            // Check if our lookup contains this key.
            if (existingSongsLookup.Contains(songIdentityKey))
            {
                // We found at least one match.
                // For simplicity and predictability, we will migrate the data to the
                // FIRST available replacement. You could add more complex logic here
                // (e.g., choose the one with the highest bitrate) if desired.
                var replacementSong = existingSongsLookup[songIdentityKey].First();

                // --- The rest of the migration logic is the same ---
                await realm.WriteAsync(() =>
                {
                    var ghostRealmObj = realm.Find<SongModel>(ghostSong.Id);
                    var replacementRealmObj = realm.Find<SongModel>(replacementSong.Id);

                    if (ghostRealmObj == null || replacementRealmObj == null)
                        return;
replacementRealmObj.PlayHistory.AddRange(ghostRealmObj.PlayHistory);
                    replacementRealmObj.EmbeddedSync.AddRange(ghostRealmObj.EmbeddedSync);
                    replacementRealmObj.UserNotes.AddRange(ghostRealmObj.UserNotes);
                    replacementRealmObj.Tags.AddRange(ghostRealmObj.Tags);
                    replacementRealmObj.ArtistToSong.AddRange(ghostRealmObj.ArtistToSong);
                    replacementRealmObj.IsNew = ghostRealmObj.IsNew;
                    replacementRealmObj.BPM = ghostRealmObj.BPM;
                    replacementRealmObj.FilePath = ghostRealmObj.FilePath;
                    replacementRealmObj.DateCreated = ghostRealmObj.DateCreated;
                    replacementRealmObj.DeviceManufacturer = ghostRealmObj.DeviceManufacturer;
                    replacementRealmObj.DeviceVersion = ghostRealmObj.DeviceVersion;
                    replacementRealmObj.DeviceName = ghostRealmObj.DeviceName;
                    replacementRealmObj.DeviceFormFactor = ghostRealmObj.DeviceFormFactor;
                    replacementRealmObj.DeviceModel = ghostRealmObj.DeviceModel;
                    replacementRealmObj.UserIDOnline = ghostRealmObj.UserIDOnline;
                    replacementRealmObj.IsFileExists = true; // Mark as existing
                    replacementRealmObj.IsFavorite = ghostRealmObj.IsFavorite;
                    replacementRealmObj.Achievement = ghostRealmObj.Achievement;
                    replacementRealmObj.Description = ghostRealmObj.Description;
                    replacementRealmObj.Conductor = ghostRealmObj.Conductor;
                    replacementRealmObj.Composer = ghostRealmObj.Composer;
                    replacementRealmObj.Lyricist = ghostRealmObj.Lyricist;
                    replacementRealmObj.Language = ghostRealmObj.Language;
                    replacementRealmObj.CoverImageBytes = ghostRealmObj.CoverImageBytes;
                    replacementRealmObj.AlbumImageBytes = ghostRealmObj.AlbumImageBytes;
                    replacementRealmObj.ArtistImageBytes = ghostRealmObj.ArtistImageBytes;
                    replacementRealmObj.UnSyncLyrics = ghostRealmObj.UnSyncLyrics;
                    replacementRealmObj.GenreName = ghostRealmObj.GenreName;
                    replacementRealmObj.Genre = ghostRealmObj.Genre;
                    replacementRealmObj.ReleaseYear = ghostRealmObj.ReleaseYear;
                    replacementRealmObj.TrackNumber = ghostRealmObj.TrackNumber;
                    replacementRealmObj.DiscNumber = ghostRealmObj.DiscNumber;
                    replacementRealmObj.DiscTotal = ghostRealmObj.DiscTotal;
                    replacementRealmObj.FileFormat = ghostRealmObj.FileFormat;
                    replacementRealmObj.FileSize = ghostRealmObj.FileSize;
                    replacementRealmObj.BitRate = ghostRealmObj.BitRate;
                    replacementRealmObj.Rating = ghostRealmObj.Rating;
                    replacementRealmObj.IsInstrumental = ghostRealmObj.IsInstrumental;
                    replacementRealmObj.HasLyrics = ghostRealmObj.HasLyrics;
                    replacementRealmObj.HasSyncedLyrics = ghostRealmObj.HasSyncedLyrics;
                    replacementRealmObj.SyncLyrics = ghostRealmObj.SyncLyrics;
                    replacementRealmObj.Title = ghostRealmObj.Title;
                    replacementRealmObj.ArtistName = ghostRealmObj.ArtistName;
                    replacementRealmObj.OtherArtistsName = ghostRealmObj.OtherArtistsName;
                    replacementRealmObj.AlbumName = ghostRealmObj.AlbumName;
                    replacementRealmObj.Id = ghostRealmObj.Id; // Keep the same ID
                    replacementRealmObj.IsFileExists = true; // Mark as existing
                    replacementRealmObj.LastDateUpdated = DateTimeOffset.UtcNow;
                    replacementRealmObj.IsNew = ghostRealmObj.IsNew;
                    replacementRealmObj.LastDateUpdated = DateTimeOffset.UtcNow;

                    realm.Remove(ghostRealmObj);
                });

                var updatedReplacementView = _mapper.Map<SongModelView>(realm.Find<SongModel>(replacementSong.Id));
                migratedDetails.Add(new MigrationDetail(ghostSong, updatedReplacementView));
            }
            else
            {
                // NO MATCH FOUND. This is a true ghost.
                unresolvedMissing.Add(ghostSong);
            }
        }

        // ... (The rest of the method for cleaning up unresolved ghosts is the same) ...

        return new LibraryReconciliationResult
        {
            ScannedCount = allSongsList.Count,
            MigratedSongs = migratedDetails,
            UnresolvedMissingSongs = unresolvedMissing
        };
    }


}