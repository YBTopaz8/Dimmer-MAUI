using Dimmer.Data.ModelView.LibSanityModels;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
}