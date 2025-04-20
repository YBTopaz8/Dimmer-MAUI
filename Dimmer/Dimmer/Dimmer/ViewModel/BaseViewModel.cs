using Dimmer.Services;

namespace Dimmer.ViewModel;

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
        _subs.Add(_stateService.AllSongs.DistinctUntilChanged().Take(1)
    .Subscribe(songs =>
    {
        MasterSongs ??= new ObservableCollection<SongModelView>();
        MasterSongs.Clear();
        foreach (var m in songs)
            MasterSongs.Add(_mapper.Map<SongModelView>(m));
    }));
    }

    private void SubscribeToCurrentSong()
    {
        _subs.Add(_stateService.CurrentSong
            .DistinctUntilChanged()
            .Subscribe(song =>
            {
                if(string.IsNullOrEmpty(song.FilePath))
                {
                    TemporarilyPickedSong=new();
                    return;
                }

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
        _subs.Add(
            SongsMgtFlow.IsPlaying
                .DistinctUntilChanged()                    
                .Subscribe(s =>
                {
                    IsPlaying = s;
                    TemporarilyPickedSong.IsCurrentPlayingHighlight = s;
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


    public void PlaySong(SongModelView song)
    {
        if (TemporarilyPickedSong != null)
            TemporarilyPickedSong.IsCurrentPlayingHighlight = false;

        TemporarilyPickedSong = song;
        song.IsCurrentPlayingHighlight = true;
        
        _stateService.SetCurrentSong(_mapper.Map<SongModel>(song));

        _stateService.SetCurrentState(DimmerPlaybackState.Loading);
        _stateService.SetCurrentState(DimmerPlaybackState.Playing);

    }

    public void PlayNext(bool IsByUser)
    {
        if (IsByUser)
        {
            SongsMgtFlow.NextInQueue();
        }
    }

    public void PlayPrevious()
    {
        SongsMgtFlow.PrevInQueue();
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
        var seconds = percentage * TemporarilyPickedSong.DurationInSeconds;
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
