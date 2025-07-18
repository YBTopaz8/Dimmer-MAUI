using DynamicData;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.ViewModel.NewFolder;
public interface ILibraryQueryService
{
    // INPUTS: These are the "control knobs" the ViewModel will set.
    IObserver<string> SearchQuery { get; }
    IObserver<Func<SongModelView, bool>> FilterPredicate { get; }
    IObserver<IComparer<SongModelView>> SortComparer { get; }

    // OUTPUT: This is the final, processed list the ViewModel will bind to.
    IObservable<IChangeSet<SongModelView>> Results { get; }
}