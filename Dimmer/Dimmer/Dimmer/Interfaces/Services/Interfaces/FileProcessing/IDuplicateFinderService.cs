using Dimmer.Data.ModelView.LibSanityModels;

namespace Dimmer.Interfaces.Services.Interfaces.FileProcessing;
public interface IDuplicateFinderService
{

    List<DuplicateSetViewModel> FindDuplicates();
    Task RemoveSongsFromDbAsync(IEnumerable<ObjectId> songIds);

    /// <summary>
    /// Processes the user's selected actions (e.g., deleting files and database entries).
    /// </summary>
    Task<int> ResolveDuplicatesAsync(IEnumerable<DuplicateItemViewModel> itemsToDelete);
    Task<LibraryValidationResult> ValidateFilePresenceAsync(IList<SongModelView>? allSongs);

    /// <summary>
    /// Intelligently validates the library. Finds missing files and attempts
    /// to migrate their data to existing files with the same identity.
    /// </summary>
    Task<LibraryReconciliationResult> ReconcileLibraryAsync(IEnumerable<SongModelView> allSongs);
}