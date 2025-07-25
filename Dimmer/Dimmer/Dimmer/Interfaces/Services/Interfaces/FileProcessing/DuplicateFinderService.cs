using Dimmer.Data.ModelView.LibSanityModels;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        if (!songIdsToDelete.Any())
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
        await Parallel.ForEachAsync(allSongs, async (song, cancellationToken) =>
        {
            if (string.IsNullOrEmpty(song.FilePath) || !File.Exists(song.FilePath))
            {
                missingSongs.Add(song);
            }
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

        // --- Step 1: Partition songs into 'existing' and 'potential ghosts' ---
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

        if (!potentialGhosts.Any())
        {
            _logger.LogInformation("Reconciliation complete. No missing files found.");
            return new LibraryReconciliationResult { ScannedCount = allSongsList.Count };
        }

        // --- Step 2: Create a high-speed lookup dictionary of existing songs ---
        // The key is the song's identity (Title|Duration). This is crucial for performance.
        var existingSongsLookup = existingSongs
            .ToDictionary(s => $"{s.Title.Trim()}|{s.DurationInSeconds}", s => s);

        using var realm = _realmFactory.GetRealmInstance();

        // --- Step 3: Process the ghosts and try to find them a new home ---
        foreach (var ghostSong in potentialGhosts)
        {
            var songIdentityKey = $"{ghostSong.Title.Trim()}|{ghostSong.DurationInSeconds}";

            // Look for an existing song with the same identity.
            if (existingSongsLookup.TryGetValue(songIdentityKey, out var replacementSong))
            {
                // MATCH FOUND! Perform the data migration.
                await realm.WriteAsync(() =>
                {
                    var ghostRealmObj = realm.Find<SongModel>(ghostSong.Id);
                    var replacementRealmObj = realm.Find<SongModel>(replacementSong.Id);

                    if (ghostRealmObj == null || replacementRealmObj == null)
                        return;

                    _logger.LogTrace("Migrating data from '{Path}' to '{NewPath}'", ghostRealmObj.FilePath, replacementRealmObj.FilePath);

                    // Migrate play history, user notes, tags, etc.
                    foreach (var playEvent in ghostRealmObj.PlayHistory)
                    {
                        replacementRealmObj.PlayHistory.Add(playEvent);
                    }
                    foreach (var note in ghostRealmObj.UserNotes)
                    {
                        replacementRealmObj.UserNotes.Add(note);
                    }
                    // You can add more migration logic here (tags, ratings, etc.)

                    // CRITICAL: After migrating, remove the old ghost entry.
                    realm.Remove(ghostRealmObj);
                });

                // Update the ViewModel object with the new data to send back to the UI
                var updatedReplacementView = _mapper.Map<SongModelView>(realm.Find<SongModel>(replacementSong.Id));
                migratedDetails.Add(new MigrationDetail(ghostSong, updatedReplacementView));
            }
            else
            {
                // NO MATCH FOUND. This is a true ghost.
                unresolvedMissing.Add(ghostSong);
            }
        }

        // --- Step 4: If any true ghosts remain, remove them from the DB ---
        if (unresolvedMissing.Any())
        {
            await realm.WriteAsync(() =>
            {
                foreach (var song in unresolvedMissing)
                {
                    var songInDb = realm.Find<SongModel>(song.Id);
                    if (songInDb != null)
                        realm.Remove(songInDb);
                }
            });
        }

        _logger.LogInformation("Reconciliation complete. Migrated: {MigratedCount}, Unresolved: {UnresolvedCount}", migratedDetails.Count, unresolvedMissing.Count);

        return new LibraryReconciliationResult
        {
            ScannedCount = allSongsList.Count,
            MigratedSongs = migratedDetails,
            UnresolvedMissingSongs = unresolvedMissing
        };
    }
}