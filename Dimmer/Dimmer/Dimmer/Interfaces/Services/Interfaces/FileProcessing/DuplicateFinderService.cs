using Dimmer.Data.ModelView.LibSanityModels;

using System;
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
            var originalSong = _mapper.Map<SongModelView>(sortedSongs.First());
            set.Items.Add(new DuplicateItemViewModel(originalSong, DuplicateStatus.Original));

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
}