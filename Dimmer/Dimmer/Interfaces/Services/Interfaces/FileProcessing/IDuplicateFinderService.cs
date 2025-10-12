namespace Dimmer.Interfaces.Services.Interfaces.FileProcessing;
public interface IDuplicateFinderService
{

    Task RemoveSongsFromDbAsync(IEnumerable<ObjectId> songIds);

    /// <summary>
    /// Processes the user's selected actions (e.g., deleting files and database entries).
    /// </summary>
    Task<int> ResolveDuplicatesAsync(IEnumerable<DuplicateItemViewModel> itemsToDelete);
    Task<LibraryValidationResult> ValidateMultipleFilesPresenceAsync(IList<SongModelView>? allSongs);

    /// <summary>
    /// Intelligently validates the library. Finds missing files and attempts
    /// to migrate their data to existing files with the same identity.
    /// </summary>
    Task<LibraryReconciliationResult> ReconcileLibraryAsync(IEnumerable<SongModelView> allSongs);
    DuplicateSearchResult FindDuplicates(DuplicateCriteria criteria, IProgress<string>? progress = null);
    DuplicateSearchResult FindDuplicatesForSong(SongModelView targetSong, DuplicateCriteria criteria);
}