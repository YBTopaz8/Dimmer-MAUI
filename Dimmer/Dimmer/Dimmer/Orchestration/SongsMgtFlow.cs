using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Dimmer.Data;
using Dimmer.Utilities.Events;
using Dimmer.Utilities.Enums;
using Dimmer.Services;

namespace Dimmer.Orchestration;

public class SongsMgtFlow : BaseAppFlow, IDisposable
{


    private readonly IDimmerAudioService _audio;
    private readonly IQueueManager<SongModelView> _queue;
    private readonly SubscriptionManager _subs;

    // Exposed streams
    public IObservable<bool> IsPlaying { get; }
    public IObservable<double> Position { get; }
    public IObservable<double> Volume { get; }

    public SongsMgtFlow(
        IPlayerStateService state,
        IRepository<SongModel> songRepo,
        IRepository<PlayDateAndCompletionStateSongLink> pdlRepo,
        IRepository<PlaylistModel> playlistRepo,
        IRepository<ArtistModel> artistRepo,
        IRepository<AlbumModel> albumRepo,
        ISettingsService settings,
        IFolderMonitorService folderMonitor,
        IDimmerAudioService audioService,
        IQueueManager<SongModelView> playQueue,
        SubscriptionManager subs,
        IMapper mapper
    ) : base(state, songRepo, pdlRepo, playlistRepo, artistRepo, albumRepo, settings, folderMonitor, mapper)
    {
        _audio  = audioService;
        _queue  = playQueue;
        _subs   = subs;

        // Map audio‑service events into observables
        var playingChanged = Observable
            .FromEventPattern<PlaybackEventArgs>(
                h => _audio.IsPlayingChanged += h,
                h => _audio.IsPlayingChanged -= h)
            .Select(evt => evt.EventArgs.IsPlaying);

        var positionChanged = Observable
            .FromEventPattern<double>(
                h => _audio.PositionChanged += h,
                h => _audio.PositionChanged -= h)
            .Select(_ => _audio.CurrentPosition);

        IsPlaying = playingChanged.StartWith(_audio.IsPlaying);
        Position  = positionChanged.StartWith(_audio.CurrentPosition);
        Volume    = Observable.Return(_audio.Volume);

        // Wire up play‑end/next/previous
        _audio.PlayEnded    += OnPlayEnded;
        _audio.PlayNext     += (_, _) => NextInQueue();
        _audio.PlayPrevious += (_, _) => PrevInQueue();

        // Auto‑play whenever CurrentSong changes
        _subs.Add(_state.CurrentSong
            .DistinctUntilChanged()
            .Subscribe(async _ => await PlaySongInAudioService()));

      

    }

    public async Task PlaySongInAudioService()
    {
        PlaySong();  // BaseAppFlow: records link
        var cover = PlayBackStaticUtils.GetCoverImage(CurrentlyPlayingSong.FilePath, true);
        CurrentlyPlayingSong.ImageBytes = cover;
        await _audio.InitializeAsync(CurrentlyPlayingSongDB, cover);
        await _audio.PlayAsync();
    }

    private void OnPlayEnded(object? s, PlaybackEventArgs e)
    {
        PlayEnded();   // BaseAppFlow: records Completed link
        NextInQueue();
    }

    public void NextInQueue()
    {
        if (!_queue.HasNext)
        {
            // refill queue from master list
            var allViews = _state.AllSongs.Value
                .Select(m => _mapper.Map<SongModelView>(m))
                .ToList();
            var idx = allViews.FindIndex(s => s.LocalDeviceId == CurrentlyPlayingSong.LocalDeviceId);
            _queue.Initialize(allViews, idx + 1);
        }

        var next = _queue.Next();
        if (next != null)
            SetCurrentSong(next);  // BaseAppFlow: pushes into state & triggers play
    }

    public void PrevInQueue()
    {
        // Build the full list of views
        var allViews = _state.AllSongs.Value
            .Select(m => _mapper.Map<SongModelView>(m))
            .ToList();

        // Find the index of the current song
        var idx = allViews.FindIndex(s => s.LocalDeviceId == CurrentlyPlayingSong.LocalDeviceId);
        if (idx == -1)
            return;

        // Step backward, wrapping to the end if we were at zero
        idx = idx <= 0
            ? allViews.Count - 1
            : idx - 1;

        // Trigger playback of that song
        var prev = allViews[idx];
        SetCurrentSong(prev);
    }

    public async Task PauseResumeSongAsync(double position, bool isPause = false)
    {
        if (isPause)
        {
            await _audio.PauseAsync();
            PauseSong();    // records pause via BaseAppFlow
        }
        else
        {
            await _audio.SeekAsync(position);
            await _audio.PlayAsync();
            ResumeSong();   // records resume via BaseAppFlow
        }
    }
    public async Task StopSongAsync()
    {
        await _audio.PauseAsync();
        CurrentlyPlayingSong.IsPlaying = false;
    }

    public async Task SeekTo(double position)
    {
        if (!_audio.IsPlaying)
            return;
        await _audio.SeekAsync(position);
        base.UpdatePlaybackState(CurrentlyPlayingSong, PlayType.Seeked, position);
    }

    public void ChangeVolume(double newVolume)
    {
        _audio.Volume = Math.Clamp(newVolume, 0, 1);
    }

    public void IncreaseVolume() => ChangeVolume(_audio.Volume + 0.01);
    public void DecreaseVolume() => ChangeVolume(_audio.Volume - 0.01);

    public double VolumeLevel => _audio.Volume;

    public void Dispose()
    {
        _subs.Dispose();
        base.Dispose();
    }
}
