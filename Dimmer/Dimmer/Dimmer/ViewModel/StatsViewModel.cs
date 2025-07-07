using AutoMapper;

using CommunityToolkit.Mvvm.ComponentModel;

using Dimmer.Data.Models;
using Dimmer.Data.ModelView;

using DynamicData;
using DynamicData.Binding;

using ReactiveUI;

using Realms;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel; // Required for this new pattern
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Dimmer.ViewModel;

public class StatsViewModel : ObservableObject, IDisposable
{
    // --- Private Fields ---
    private readonly IMapper _mapper;
    private readonly SourceList<DimmerPlayEventView> _playEventSource = new();
    private readonly CompositeDisposable _disposables = new();
    private IDisposable? _realmSubscription;
    private bool _isDisposed;

    // === START OF NEW PATTERN ===

    // 1. A private, WRITABLE collection. This is what DynamicData will update.
    private readonly ObservableCollectionExtended<DimmerPlayEventView> _allLivePlayEventsBackingList = new();

    // 2. The public, READ-ONLY property for the UI. It's a wrapper around our private list.
    public ReadOnlyObservableCollection<DimmerPlayEventView> AllLivePlayEvents { get; }

    // === END OF NEW PATTERN ===

    public StatsViewModel(IRealmFactory realmFactory, IMapper mapper)
    {
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));


        // 3. Initialize the public property, wrapping our private DynamicData list. This works perfectly.
        AllLivePlayEvents = new ReadOnlyObservableCollection<DimmerPlayEventView>(_allLivePlayEventsBackingList);

        var realm = realmFactory.GetRealmInstance();
        var liveRealmEvents = realm.All<DimmerPlayEvent>().AsRealmCollection();
        _realmSubscription = liveRealmEvents.SubscribeForNotifications(OnRealmPlayEventsChanged);

        var eventStream = _playEventSource.Connect()
           .Publish()
           .RefCount();

        // 4. THIS WILL NOW COMPILE.
        //    The .Bind() method is now receiving the correct collection type it expects.
        eventStream
           .ObserveOn(RxApp.MainThreadScheduler)
           .Bind(_allLivePlayEventsBackingList)
           .Subscribe()
           .DisposeWith(_disposables);
    }

    private void OnRealmPlayEventsChanged(IRealmCollection<DimmerPlayEvent> sender, ChangeSet? changes)
    {
        // This method does not need to change. It is correct.
        if (changes is null)
        {
            var initialItems = _mapper.Map<IEnumerable<DimmerPlayEventView>>(sender);
            _playEventSource.Edit(innerList =>
            {
                innerList.Clear();
                innerList.AddRange(initialItems);
            });
            return;
        }

        _playEventSource.Edit(innerList =>
        {
            // =========================================================================
            //  THE NEW, FOOLPROOF CODE. NO MORE .Reverse()
            // =========================================================================

            // Process deletions with a standard 'for' loop, counting backwards.
            for (int i = changes.DeletedIndices.Length - 1; i >= 0; i--)
            {
                var indexToDelete = changes.DeletedIndices[i];
                innerList.RemoveAt(indexToDelete);
            }

            // These loops were always correct.
            foreach (var i in changes.InsertedIndices)
            {
                var newEventView = _mapper.Map<DimmerPlayEventView>(sender[i]);
                innerList.Insert(i, newEventView);
            }

            foreach (var i in changes.NewModifiedIndices)
            {
                var updatedEventView = _mapper.Map<DimmerPlayEventView>(sender[i]);
                innerList[i] = updatedEventView;
            }
        });

    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed)
            return;
        if (disposing)
        {
            _realmSubscription?.Dispose();
            _disposables.Dispose();
            _playEventSource.Dispose();
        }
        _isDisposed = true;
    }
}