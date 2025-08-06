
//using DynamicData;

//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Reactive.Disposables;
//using System.Text;
//using System.Threading.Tasks;

//namespace Dimmer.ViewModel.NewFolder;
//internal class LibraryQueryService : ILibraryQueryService
//{
//    private readonly BehaviorSubject<string> _searchQuerySubject;
//    private readonly BehaviorSubject<Func<SongModelView, bool>> _filterPredicate;
//    // ... other subjects

//    private readonly CompositeDisposable _disposables = new();

//    public IObserver<string> SearchQuery => _searchQuerySubject;
//    public IObservable<IChangeSet<SongModelView>> Results { get; }

//    // Inject ONLY the dependencies this service needs
//    public LibraryQueryService(IRealmFactory realmFactory, IMapper mapper, ILogger<LibraryQueryService> logger)
//    {
//        // ... initialize subjects ...

//        // ... PASTE YOUR ENTIRE STEP 2 (CONTROL) AND STEP 3 (DATA) PIPELINES HERE ...
//        // The final line will be something like:
//        Results = finalStream; // Where finalStream is your masterpiece pipeline

//        // Remember to dispose of your subscriptions
//        finalStream.Subscribe().DisposeWith(_disposables);
//        // ... dispose other subscriptions ...
//    }

//    public void Dispose()
//    {
//        _disposables.Dispose();
//    }
