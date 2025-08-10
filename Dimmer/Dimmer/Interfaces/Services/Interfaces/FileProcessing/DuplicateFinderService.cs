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
        List<SongModel>? allSongsList = allSongsQuery.ToList();

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
            // Use the first song as a representative for the set's display info
            var representativeSong = group.First();
            var set = new DuplicateSetViewModel(
                representativeSong.Title,
                representativeSong.DurationInSeconds
            );

            // Sort songs in the group to determine the "original" vs. "duplicates"
            // A good heuristic: best quality (BitRate), then oldest file.
            var sortedSongs = group
                .OrderByDescending(s => s.BitRate ?? 0)
                .ThenByDescending(s => s.FileSize)
                .ThenBy(s => s.DateCreated)
                .ToList();

            // The first item in the sorted list is our designated "Original"
            var originalSongModel = sortedSongs.First();
            set.Items.Add(new DuplicateItemViewModel(_mapper.Map<SongModelView>(originalSongModel), DuplicateStatus.Original));

            // All other items are duplicates
            foreach (var duplicateSongModel in sortedSongs.Skip(1))
            {
                set.Items.Add(new DuplicateItemViewModel(_mapper.Map<SongModelView>(duplicateSongModel), DuplicateStatus.Duplicate));
            }

            result.DuplicateSets.Add(set);
        }

        _logger.LogInformation("Finished duplicate scan. Found {Count} sets.", result.DuplicateSets.Count);
        progress?.Report("Scan complete!");

        return result;
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

    
    public async Task<LibraryValidationResult> ValidateFilePresenceAsync(IList<SongModelView>? allSongs)
    {

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

    private string GenerateGroupingKey(SongModel song, DuplicateCriteria criteria)
    {
        var keyParts = new List<string>();

        if (criteria.HasFlag(DuplicateCriteria.Title))
        {
            // Normalize the title for better matching
            keyParts.Add(song.Title?.Trim().ToLowerInvariant() ?? string.Empty);
        }
        if (criteria.HasFlag(DuplicateCriteria.Artist))
        {
            // Normalize artist name, maybe even sort multiple artists
            keyParts.Add(song.ArtistName?.Trim().ToLowerInvariant() ?? string.Empty);
        }
        if (criteria.HasFlag(DuplicateCriteria.Album))
        {
            keyParts.Add(song.AlbumName?.Trim().ToLowerInvariant() ?? string.Empty);
        }
        if (criteria.HasFlag(DuplicateCriteria.Duration))
        {
            // Round duration to the nearest second to catch minor discrepancies
            keyParts.Add(Math.Round(song.DurationInSeconds).ToString());
        }
        if (criteria.HasFlag(DuplicateCriteria.FileSize))
        {
            keyParts.Add(song.FileSize.ToString());
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
        // Add other properties you want to merge...

        // Ensure the survivor has the earliest creation date.
        if (ghost.DateCreated.HasValue && (!survivor.DateCreated.HasValue || ghost.DateCreated < survivor.DateCreated))
        {
            survivor.DateCreated = ghost.DateCreated;
        }
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
    public List<SongModel>? ResultSongs { get; set; } = null;
}