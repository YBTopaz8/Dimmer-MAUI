using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Dimmer.Interfaces;
using Dimmer.Services;
using Dimmer.Utilities.Extensions;

namespace Dimmer.Services
{
    public class PlayerStateService : IPlayerStateService, IDisposable
    {
        readonly BehaviorSubject<SongModel> _currentSong = new(new SongModel());
        readonly BehaviorSubject<SongModel> _secondSelectedSong = new(new SongModel());
        readonly BehaviorSubject<DimmerPlaybackState> _playbackState = new(DimmerPlaybackState.Stopped);
        readonly BehaviorSubject<IReadOnlyList<SongModel>> _allSongs = new(Array.Empty<SongModel>());
        readonly BehaviorSubject<PlaylistModel> _currentPlaylist = new(default!);
        
        readonly BehaviorSubject<IReadOnlyList<Window>> _windows = new(Array.Empty<Window>());
        readonly BehaviorSubject<CurrentPage> _page = new(Utilities.Enums.CurrentPage.HomePage);

        readonly IQueueManager<SongModel> _queue;
        readonly CompositeDisposable _subs = new();

        public PlayerStateService(IQueueManager<SongModel>? queue = null)
        {
            _queue = queue ?? new QueueManager<SongModel>();
            // whenever state changes (skip the seed), advance
            _subs.Add(_playbackState.Skip(1).Subscribe(OnPlaybackStateChanged));
            
        }

        // Observables
        public IObservable<SongModel> CurrentSong => _currentSong.AsObservable();
        public IObservable<SongModel> SecondSelectedSong => _secondSelectedSong.AsObservable();
        public IObservable<IReadOnlyList<SongModel>> AllCurrentSongs => _allSongs.AsObservable();
        public IObservable<DimmerPlaybackState> CurrentPlayBackState => _playbackState.AsObservable();
        public IObservable<PlaylistModel> CurrentPlaylist => _currentPlaylist.AsObservable();
        public IObservable<IReadOnlyList<Window>> CurrentlyOpenWindows => _windows.AsObservable();
        public IObservable<CurrentPage> CurrentPage => _page.AsObservable();

        public static bool IsShuffleOn { get; set; } = false;
        // Core setters
        public void LoadAllSongs(IEnumerable<SongModel> songs)
        {
            var list = songs.ToList();
            if (IsShuffleOn)
                list.ShuffleInPlace();

            _allSongs.OnNext(list.AsReadOnly());
        }

        public void SetCurrentSong(SongModel song)
        {
            ArgumentNullException.ThrowIfNull(song);
            _currentSong.OnNext(song);
        }
        
        public void SetSecondSelectdSong(SongModel song)
        {
            ArgumentNullException.ThrowIfNull(song);
            _secondSelectedSong.OnNext(song);
        }

        public void SetCurrentState(DimmerPlaybackState state)
            => _playbackState.OnNext(state);

        public void AddWindow(Window window)
            => _windows.OnNext(_windows.Value.Append(window).ToList().AsReadOnly());

        public void RemoveWindow(Window window)
            => _windows.OnNext(_windows.Value.Where(x => x != window).ToList().AsReadOnly());

        public void SetCurrentPlaylist(IEnumerable<SongModel> songs,PlaylistModel? Playlist=null)
        {
            if (Playlist is null)
            {
                LoadAllSongs(BaseAppFlow.MasterList);
            }
            else
            {
                LoadAllSongs(songs);
                _currentPlaylist.OnNext(Playlist); // from here get a method in baseappflow to create a playlist in db and save
            }
                var list = BaseAppFlow.MasterList;

            
            // clear old subs (queue + state watcher)
            _subs.Clear();
            _subs.Add(_playbackState.Skip(1).Subscribe(OnPlaybackStateChanged));

            // reset & wire queue events

            _queue.BatchEnqueued += HandleBatch;
            _subs.Add(Disposable.Create(() => _queue.BatchEnqueued -= HandleBatch));

            _queue.ItemDequeued += HandleDequeue;
            _subs.Add(Disposable.Create(() => _queue.ItemDequeued -= HandleDequeue));

           
        }
        void HandleBatch(int batchId, IReadOnlyList<SongModel> batch)
        {
            // e.g. raise a UI hook
            Console.WriteLine($"Enqueued batch {batchId}, {batch.Count} items");
        }
        void HandleDequeue(int batchId, SongModel item)
        {
            _currentSong.OnNext(item);
        }


        // add/remove single or multiple songs
        public void AddSingleSongToCurrentPlaylist(PlaylistModel p, SongModel song)
            => AddSongsToCurrentPlaylist(p, new[] { song });

        public void AddSongsToCurrentPlaylist(PlaylistModel p, IEnumerable<SongModel> songs)
        {
            var merged = _allSongs.Value.Concat(songs)
                               .DistinctBy(s => s.LocalDeviceId)
                               .ToList();
            SetCurrentPlaylist(merged,p);
        }

        public void RemoveSongFromCurrentPlaylist(PlaylistModel p, SongModel song)
            => RemoveSongFromCurrentPlaylist(p, new[] { song });

        public void RemoveSongFromCurrentPlaylist(PlaylistModel p, IEnumerable<SongModel> songs)
        {
            var filtered = _allSongs.Value
                              .Where(s => !songs.Select(x => x.LocalDeviceId).Contains(s.LocalDeviceId))
                              .ToList();
            SetCurrentPlaylist(filtered,p  );
        }

        public void SetCurrentPage(CurrentPage page)
        {
            if (page == _page.Value)
                return; // no change

            _page.OnNext(page);
        }

        // queue advancement logic
        void OnPlaybackStateChanged(DimmerPlaybackState st)
        {
            switch (st)
            {
                case DimmerPlaybackState.PlayNext:
                case DimmerPlaybackState.Ended:
                    AdvanceQueue();
                    break;
                case DimmerPlaybackState.Playing:
                    break;
                case DimmerPlaybackState.PlayPrevious:
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
            _windows.Dispose();
            _page.Dispose();
        }

    }
}
