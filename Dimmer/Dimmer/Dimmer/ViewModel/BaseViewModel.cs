using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using AutoMapper;
using Dimmer.Data;
using Dimmer.Utilities.Enums;
using Dimmer.Services;

namespace Dimmer.ViewModel
{
    public partial class BaseViewModel : ObservableObject, IDisposable
    {
#if DEBUG
        public const string CurrentAppVersion = "Dimmer v1.8a-debug";
#else
        public const string CurrentAppVersion = "Dimmer v1.8a-Release";
#endif

        private readonly IMapper _mapper;
        private readonly IPlayerStateService _stateService;
        private readonly ISettingsService _settingsService;
        private readonly SubscriptionManager _subs;

        public AlbumsMgtFlow AlbumsMgtFlow { get; }
        public PlayListMgtFlow PlaylistsMgtFlow { get; }
        public SongsMgtFlow SongsMgtFlow { get; }

        [ObservableProperty]
        public partial bool IsShuffle { get; set; }

        [ObservableProperty]
        public partial bool IsStickToTop {get;set;}

        [ObservableProperty]
        public partial string AppTitle { get;set;}
        [ObservableProperty]
        public partial bool IsPlaying {get;set;}

        [ObservableProperty]
        public partial double CurrentPositionPercentage {get;set;}

        [ObservableProperty]
        public partial RepeatMode RepeatMode {get;set;}

        [ObservableProperty]
        public partial ObservableCollection<SongModelView>? MasterSongs {get;set;}

        [ObservableProperty]
        public partial ObservableCollection<LyricPhraseModelView>? SynchronizedLyrics {get;set;}

        [ObservableProperty]
        public partial LyricPhraseModelView CurrentLyricPhrase {get;set;}

        [ObservableProperty]
        public partial SongModelView TemporarilyPickedSong {get;set;}

        [ObservableProperty]
        public partial double CurrentPositionInSeconds {get;set;}

        [ObservableProperty]
        public partial double VolumeLevel {get;set;}

        

        [ObservableProperty]
        public partial CurrentPage CurrentlySelectedPage {get;set;}

        public BaseViewModel(
            IMapper mapper,
            AlbumsMgtFlow albumsMgtFlow,
            PlayListMgtFlow playlistsMgtFlow,
            SongsMgtFlow songsMgtFlow,
            IPlayerStateService stateService,
            ISettingsService settingsService,
            SubscriptionManager subs)
        {
            _mapper = mapper;
            AlbumsMgtFlow = albumsMgtFlow;
            PlaylistsMgtFlow = playlistsMgtFlow;
            SongsMgtFlow = songsMgtFlow;
            _stateService = stateService;
            _settingsService = settingsService;
            _subs = subs;

            Initialize();
        }

        private void Initialize()
        {
            SubscribeToMasterSongs();
            SubscribeToCurrentSong();
            SubscribeToIsPlaying();
            SubscribeToPosition();
            SubscribeToVolume();

            CurrentPositionPercentage = 0;
            IsStickToTop = _settingsService.IsStickToTop;
            RepeatMode = _settingsService.RepeatMode;
            IsShuffle = _settingsService.ShuffleOn;
        }

        private void SubscribeToMasterSongs()
        {
            _subs.Add(_stateService.AllSongs
                .Subscribe(list =>
                {
                    var views = _mapper.Map<List<SongModelView>>(list);
                    MasterSongs = views.ToObservableCollection();
                }));
        }

        private void SubscribeToCurrentSong()
        {
            _subs.Add(_stateService.CurrentSong
                .DistinctUntilChanged()
                .Subscribe(song =>
                {
                    if (TemporarilyPickedSong != null)
                        TemporarilyPickedSong.IsCurrentPlayingHighlight = false;

                    TemporarilyPickedSong = _mapper.Map<SongModelView>(song);
                    if (TemporarilyPickedSong != null)
                    {
                        TemporarilyPickedSong.IsCurrentPlayingHighlight = true;
                        AppTitle = $"{TemporarilyPickedSong.Title} - {TemporarilyPickedSong.ArtistName} [{TemporarilyPickedSong.AlbumName}] | {CurrentAppVersion}";
                    }
                    else
                    {
                        AppTitle = CurrentAppVersion;
                    }
                }));
        }

        private void SubscribeToIsPlaying()
        {
            _subs.Add(SongsMgtFlow.IsPlaying
                .DistinctUntilChanged()
                .Subscribe(isPlaying =>
                {
                    IsPlaying = isPlaying;
                    if (!isPlaying)
                        _ = PlayNextAsync();
                }));
        }

        private void SubscribeToPosition()
        {
            _subs.Add(SongsMgtFlow.Position
                .Subscribe(pos =>
                {
                    CurrentPositionInSeconds = pos;
                    var duration = SongsMgtFlow.CurrentlyPlayingSong?.DurationInSeconds ?? 1;
                    CurrentPositionPercentage = pos / duration;
                }));
        }

        private void SubscribeToVolume()
        {
            _subs.Add(SongsMgtFlow.Volume
                .Subscribe(vol => VolumeLevel = vol * 100));
        }

        public IAsyncRelayCommand PlayNextCommand => new AsyncRelayCommand(PlayNextAsync);
        public IAsyncRelayCommand PlayPreviousCommand => new AsyncRelayCommand(PlayPreviousAsync);
        public IAsyncRelayCommand PlayPauseCommand => new AsyncRelayCommand(PlayPauseAsync);

        public async Task PlaySongAsync(SongModelView song)
        {
            if (TemporarilyPickedSong != null)
                TemporarilyPickedSong.IsCurrentPlayingHighlight = false;

            TemporarilyPickedSong = song;
            song.IsCurrentPlayingHighlight = true;

            SongsMgtFlow.SetCurrentSong(song);
            await SongsMgtFlow.PlaySongInAudioService();
        }

        public async Task PlayNextAsync()
        {
            // shuffle or sequential logic simplified to use flow
            await PlaySongAsync(_mapper.Map<SongModelView>(_stateService.AllSongs.Value.First().LocalDeviceId));
        }

        public async Task PlayPreviousAsync()
        {
            // similar logic for previous
            await PlaySongAsync(TemporarilyPickedSong?? _mapper.Map<SongModelView>(_stateService.AllSongs.Value.First().LocalDeviceId));
        }

        public async Task PlayPauseAsync()
        {
            if (IsPlaying)
                await SongsMgtFlow.PauseResumeSongAsync(CurrentPositionInSeconds, true);
            else
                await SongsMgtFlow.PauseResumeSongAsync(CurrentPositionInSeconds, false);
        }

        public void ToggleShuffle()
        {
            IsShuffle = !IsShuffle;
            SongsMgtFlow.ToggleShuffle(IsShuffle);
            _settingsService.ShuffleOn = IsShuffle;
        }

        public void ToggleRepeatMode()
        {
            RepeatMode = SongsMgtFlow.ToggleRepeatMode();
            _settingsService.RepeatMode = RepeatMode;
        }

        public void IncreaseVolume()
        {
            SongsMgtFlow.IncreaseVolume();
            VolumeLevel = SongsMgtFlow.VolumeLevel * 100;
        }

        public void DecreaseVolume()
        {
            SongsMgtFlow.DecreaseVolume();
            VolumeLevel = SongsMgtFlow.VolumeLevel * 100;
        }

        public void SetVolume(double vol)
        {
            SongsMgtFlow.ChangeVolume(vol);
            VolumeLevel = SongsMgtFlow.VolumeLevel * 100;
        }

        public void SeekTo(double percentage)
        {
            var duration = SongsMgtFlow.CurrentlyPlayingSong?.DurationInSeconds ?? 1;
            var seconds = percentage * duration;
            _ = SongsMgtFlow.SeekTo(seconds);
        }
        public void SeekSongPosition(LyricPhraseModelView? lryPhrase = null, double currPosPer = 0)
        {
            if (lryPhrase is not null)
            {

                CurrentPositionInSeconds = lryPhrase.TimeStampMs * 0.001;
                _=SongsMgtFlow.SeekTo(CurrentPositionInSeconds);
                return;
            }
            
        }
        public bool ToggleStickToTop()
        {
            IsStickToTop = !IsStickToTop;
            _settingsService.IsStickToTop = IsStickToTop;
            return IsStickToTop;
        }

        public void Dispose()
        {
            _subs.Dispose();
        }
    }
}
