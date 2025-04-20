using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Dimmer.Interfaces;
using Dimmer.Services;

namespace Dimmer.Services
{
    public class PlayerStateService : IPlayerStateService, IDisposable
    {
        readonly BehaviorSubject<SongModel> _currentSong = new(new SongModel());
        readonly BehaviorSubject<DimmerPlaybackState> _playbackState = new(DimmerPlaybackState.Stopped);
        readonly BehaviorSubject<IReadOnlyList<SongModel>> _allSongs = new(Array.Empty<SongModel>());
        readonly BehaviorSubject<PlaylistModel> _currentPlaylist = new(default!);
        readonly BehaviorSubject<IReadOnlyList<SongModel>> _playlistSongs = new(Array.Empty<SongModel>());
        readonly BehaviorSubject<IReadOnlyList<Window>> _windows = new(Array.Empty<Window>());
        readonly BehaviorSubject<CurrentPage> _page = new(CurrentPage.Unknown);

        readonly IQueueManager<SongModel> _queue;
        readonly CompositeDisposable _subs = new();

        public PlayerStateService(IQueueManager<SongModel>? queue = null)
        {
            _queue = queue ?? new QueueManager<SongModel>();
            // advance when playback state changes
            _subs.Add(_playbackState
                .Skip(1)
                .Subscribe(OnPlaybackStateChanged));
        }

        public IObservable<SongModel> CurrentSong => _currentSong.AsObservable();
        public IObservable<IReadOnlyList<SongModel>> AllSongs => _allSongs.AsObservable();
        public IObservable<DimmerPlaybackState> CurrentPlayBackState
                                                        => _playbackState.AsObservable();
        public IObservable<PlaylistModel> CurrentPlaylist => _currentPlaylist.AsObservable();
        public IObservable<IReadOnlyList<SongModel>> CurrentPlaylistSongs
                                                        => _playlistSongs.AsObservable();
        public IObservable<IReadOnlyList<Window>> CurrentlyOpenWindows
                                                        => _windows.AsObservable();
        public IObservable<CurrentPage>? CurrentPage => _page.AsObservable();

        public void LoadAllSongs(IEnumerable<SongModel> songs)
            => _allSongs.OnNext(songs.ToList().AsReadOnly());

        public void SetCurrentSong(SongModel song)
        {
            if (song is null)
                throw new ArgumentNullException(nameof(song));
            _currentSong.OnNext(song);
        }

        public void SetCurrentState(DimmerPlaybackState state)
            => _playbackState.OnNext(state);

        public void AddWindow(Window w)
            => _windows.OnNext(_windows.Value.Append(w).ToList().AsReadOnly());

        public void RemoveWindow(Window w)
            => _windows.OnNext(_windows.Value.Where(x => x != w).ToList().AsReadOnly());

        public void SetCurrentPlaylist(PlaylistModel p, IEnumerable<SongModel> songs)
        {
            if (p is null)
                throw new ArgumentNullException(nameof(p));
            var list = songs.ToList().AsReadOnly();
            _currentPlaylist.OnNext(p);
            _playlistSongs.OnNext(list);
            _queue.Initialize(list);                // use playlist as queue basis
            _subs.Add(_queue.BatchEnqueued.Subscribe((id, batch) => { /* optional UI hook */ }));
            _subs.Add(_queue.ItemDequeued.Subscribe((id, item) => _currentSong.OnNext(item)));
        }

        public void AddSongToCurrentPlaylist(PlaylistModel _, IEnumerable<SongModel> songs)
            => SetCurrentPlaylist(_currentPlaylist.Value, songs);

        public void RemoveSongFromCurrentPlaylist(PlaylistModel _, IEnumerable<SongModel> songs)
            => SetCurrentPlaylist(_currentPlaylist.Value, songs);

        public void SetCurrentPage(CurrentPage pg)
            => _page.OnNext(pg);

        void OnPlaybackStateChanged(DimmerPlaybackState st)
        {
            switch (st)
            {
                case DimmerPlaybackState.PlayNext:
                case DimmerPlaybackState.Ended:
                    AdvanceQueue();
                    break;
                case DimmerPlaybackState.Playing:
                    if (!_queue.HasNext)
                        _queue.Initialize(_allSongs.Value);
                    break;
                case DimmerPlaybackState.PlayPrevious:
                    // implement if you add a back‑stack
                    AdvanceQueue();
                    break;
            }
        }

        void AdvanceQueue()
        {
            var next = _queue.Next();
            if (next != null)
                _currentSong.OnNext(next);
        }

        public void Dispose()
        {
            _subs.Dispose();
            _currentSong.Dispose();
            _playbackState.Dispose();
            _allSongs.Dispose();
            _currentPlaylist.Dispose();
            _playlistSongs.Dispose();
            _windows.Dispose();
            _page.Dispose();
        }
    }
}
