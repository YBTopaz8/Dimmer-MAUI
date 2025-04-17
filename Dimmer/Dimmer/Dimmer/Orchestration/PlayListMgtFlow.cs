using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using AutoMapper;
using Dimmer.Data;
using Dimmer.Services;
using Dimmer.Utilities.Enums;
using Dimmer.Utilities.Events;

namespace Dimmer.Orchestration
{
    public class PlayListMgtFlow : BaseAppFlow, IDisposable
    {
        private readonly IRepository<PlaylistModel> _playlistRepo;
        private readonly IQueueManager<SongModelView> _queue;
        private readonly SubscriptionManager _subs;
        private readonly BehaviorSubject<DimmerPlaybackState> _stateSubj
            = new(DimmerPlaybackState.Stopped);

        public ObservableCollection<PlaylistModel> CurrentSetOfPlaylists { get; }
            = new();

        public IObservable<DimmerPlaybackState> CurrentAppState
            => _stateSubj.AsObservable();

        public PlayListMgtFlow(
            IPlayerStateService state,
            IRepository<SongModel> songRepo,
            IRepository<PlayDateAndCompletionStateSongLink> pdlRepo,
            IRepository<PlaylistModel> playlistRepo,
            IRepository<ArtistModel> artistRepo,
            IRepository<AlbumModel> albumRepo,
            ISettingsService settings,
            IFolderMonitorService folderMonitor,
            IQueueManager<SongModelView> queueManager,
            SubscriptionManager subs,
            IMapper mapper
        ) : base(state, songRepo, pdlRepo, playlistRepo, artistRepo, albumRepo, settings, folderMonitor, mapper)
        {
            _playlistRepo = playlistRepo;
            _queue        = queueManager;
            _subs         = subs;

            // Load saved playlists
            foreach (var pl in _playlistRepo.GetAll())
                CurrentSetOfPlaylists.Add(pl);

            // React to playback-state changes
            _subs.Add(_stateSubj
                .DistinctUntilChanged()
                .Subscribe(OnPlaybackStateChanged));

            // Whenever the current song changes, we could pre-load UI or metadata
            _subs.Add(state.CurrentSong
                .DistinctUntilChanged()
                .Subscribe(_ => { /* no-op or UI hook */ }));
        }

        private void OnPlaybackStateChanged(DimmerPlaybackState st)
        {
            switch (st)
            {
                case DimmerPlaybackState.Ended:
                    AdvanceQueue();
                    break;
                case DimmerPlaybackState.Skipped:
                    InitializeQueue(CurrentlyPlayingSong);
                    break;
                    // other cases as needed...
            }
        }

        private void InitializeQueue(SongModelView start)
        {
            _queue.Initialize(
                items: _state.AllSongs.Value
                    .Select(m => _mapper.Map<SongModelView>(m)),
                startIndex: _state.AllSongs.Value
                    .FindIndex(m => m.LocalDeviceId == start.LocalDeviceId)
            );
            _stateSubj.OnNext(DimmerPlaybackState.PlayNext);
            UpdatePlaybackLink(PlayType.Skipped);
        }

        private void AdvanceQueue()
        {
            var next = _queue.Next();
            if (next != null)
                SetCurrentSong(next);  // pushes into state & triggers play
        }

        /// <summary>
        /// Persist skip/completion/etc on DB via BaseAppFlow helper.
        /// </summary>
        private void UpdatePlaybackLink(PlayType type) =>
           UpdatePlaybackState(CurrentlyPlayingSong, type);

        public void CreatePlaylistOfFiftySongs()
        {
            // your custom logic here…
        }

        public void Dispose()
        {
            _subs.Dispose();
            _stateSubj.Dispose();
            base.Dispose();
        }
    }
}
