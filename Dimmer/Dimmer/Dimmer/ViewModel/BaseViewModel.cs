namespace Dimmer.ViewModel;
public partial class BaseViewModel : ObservableObject
{
#if DEBUG
    public const string CurrentAppVersion = "Dimmer v1.8a-debug";
#elif RELEASE
    public const string CurrentAppVersion = "Dimmer v1.8a-Release";
#endif
    private readonly IMapper mapper;
    public readonly SongsMgtFlow songsMgtFlow;

    #region public properties
    [ObservableProperty]
    public partial bool IsShuffle { get; set; }
    [ObservableProperty]
    public partial bool IsStickToTop { get; set; } = false;
    [ObservableProperty]
    public partial bool IsPlaying { get; set; }
    [ObservableProperty]
    public partial double CurrentPositionPercentage { get; set; }
    [ObservableProperty]
    public partial RepeatMode RepeatMode { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<SongModelView>? MasterSongs { get; internal set; }
    [ObservableProperty]
    public partial ObservableCollection<LyricPhraseModelView>? SynchronizedLyrics { get; internal set; }
    [ObservableProperty]
    public partial LyricPhraseModelView? CurrentLyricPhrase { get; set; }
    [ObservableProperty]
    public partial SongModelView? TemporarilyPickedSong { get; set; }
    [ObservableProperty]
    public partial View? MySelectedSongView { get; set; }
    [ObservableProperty]
    public partial double CurrentPositionInSeconds { get; set; }
    [ObservableProperty]
    public partial double VolumeLevel { get; set; }
    [ObservableProperty]
    public partial string? AppTitle { get; set; }

    [ObservableProperty]
    public partial CurrentPage CurrentlySelectedPage { get; set; }
    
    

    #endregion
    public BaseViewModel(IMapper mapper, SongsMgtFlow songsMgtFlow ,IDimmerAudioService dimmerAudioService)
    {

        this.mapper=mapper;
        this.songsMgtFlow=songsMgtFlow;
        LoadPageViewModel();
        AppTitle = CurrentAppVersion;
    }

    private void LoadPageViewModel()
    {
        SubscribeToMasterSongs();
        SubscribeToCurrentlyPlayingSong();
        SubscribeToIsPlaying();
        SubscribeToCurrentPosition();
        CurrentPositionPercentage = 0;
        IsStickToTop = AppSettingsService.IsSticktoTopPreference.GetIsSticktoTopState();
    }

    void SubscribeToCurrentPosition()
    {
        songsMgtFlow.CurrentSongPosition.Subscribe(position =>
        {
            CurrentPositionInSeconds = position;

            CurrentPositionPercentage = (position / songsMgtFlow.CurrentlyPlayingSong.DurationInSeconds);
        }); 
    }
    public void SubscribeToCurrentVolume()
    {
        songsMgtFlow.CurrentSongVolume.Subscribe(volume =>
        {
            VolumeLevel = volume;
        });
    }

    public void SubscribeToIsPlaying()
    {
        songsMgtFlow.IsPlaying.DistinctUntilChanged()
        .Subscribe(async isPlaying =>
        {
            IsPlaying = isPlaying;
            switch (isPlaying)
            {
                case true:
                    CurrentPositionInSeconds = songsMgtFlow.CurrentPositionInSec;
                    break;
                default:
                    IsPlaying = false;
                    if (songsMgtFlow.IsPlayedCompletely)
                    {
                        await PlayNext();
                    }
                    break;
            }
            
            
        });
    }
    
    public void SubscribeToCurrentlyPlayingSong()
    {
        BaseAppFlow.CurrentSong
            .DistinctUntilChanged()
            .Subscribe(song =>
            {
                if (TemporarilyPickedSong is not null)
                {
                    TemporarilyPickedSong.IsCurrentPlayingHighlight = false;
                }

                TemporarilyPickedSong = mapper.Map<SongModelView>(song);
                
                TemporarilyPickedSong.IsCurrentPlayingHighlight = true;
                if (TemporarilyPickedSong != null)
                {
                    AppTitle = $"{TemporarilyPickedSong.Title} - {TemporarilyPickedSong.ArtistName} [{TemporarilyPickedSong.AlbumName}] | {CurrentAppVersion}";
                }
                else
                {
                    AppTitle = CurrentAppVersion;
                }
            });
    }
    private void SubscribeToMasterSongs()
    {
        BaseAppFlow.AllSongs.Subscribe(songs =>
        {
            MasterSongs = mapper.Map<List<SongModelView>>(songs).ToObservableCollection();
        });
    }
    Random random = new Random();

    public SongModelView MySelectedSong { get; set; } = new SongModelView();
    public void SetSelectedSong(SongModelView song)
    {
        song.IsCurrentPlayingHighlight = false;
        songsMgtFlow.CurrentlyPlayingSong ??=song;
        MySelectedSong = song;        
    }
    #region playback controls 
    public async Task PlayPrevious()
    {
        if (MasterSongs is not null)
        {
            if (IsShuffle)
        {

           bool isInCol =  IsFoundInCollection(TemporarilyPickedSong!, MasterSongs);
            if (isInCol)
            {
                int index = MasterSongs.IndexOf(TemporarilyPickedSong!);
                int newIndex = random.Next(0, MasterSongs.Count);
                while (newIndex == index)
                {
                    newIndex = random.Next(0, MasterSongs.Count);
                }
                var song = MasterSongs[newIndex];
                await PlaySong(song);
            }
            else
            {
               await PlaySong(TemporarilyPickedSong!);
                }
        }
        else
        {
            int index = MasterSongs.IndexOf(TemporarilyPickedSong!);
            if (index <= 0)
            {
                index = MasterSongs.Count - 1;
            }
            else
            {
                index--;

            }

            var song = MasterSongs[index];
            if (song != null)
            {
                await PlaySong(song);
            }
            }
        }
    }
     public async Task PlayNext()
     {
        if (MasterSongs is not null)
        {

            if (IsShuffle)
            {
               bool isInCol =  IsFoundInCollection(TemporarilyPickedSong!, MasterSongs);
                if (isInCol)
                {
                    int index = MasterSongs.IndexOf(TemporarilyPickedSong!);
                    int newIndex = random.Next(0, MasterSongs.Count);
                    while (newIndex == index)
                    {
                        newIndex = random.Next(0, MasterSongs.Count);
                    }
                    var song = MasterSongs[newIndex];
                    await PlaySong(song);
                }
                else
                {
                    await PlaySong(TemporarilyPickedSong!);
                }
            }
            else
            {
                int index = MasterSongs.IndexOf(TemporarilyPickedSong!);
                index++;
                var song = MasterSongs[index];
                if (song != null)
                {
                  await PlaySong(song);
                }
            }
        }
    }

    public async Task PlaySong(SongModelView song)
    {
        if(TemporarilyPickedSong is not null)
        {
            TemporarilyPickedSong.IsCurrentPlayingHighlight = false;
        }
        songsMgtFlow.SetCurrentSong(song);
        songsMgtFlow.CurrentlyPlayingSong=song;
        TemporarilyPickedSong = song;     
        
       await songsMgtFlow.PlaySongInAudioService();
    }

    public async Task PlayPauseSong()
    {
        if (TemporarilyPickedSong is not null)
        {

        if ( IsPlaying)
        {

           await songsMgtFlow.PauseResumeSongAsync(CurrentPositionInSeconds, true);
           
        }
        else
        {
            if (CurrentPositionPercentage >= 0.98)
            {
                await PlaySong(TemporarilyPickedSong);
                return;
            }
            await songsMgtFlow.PauseResumeSongAsync(CurrentPositionInSeconds, false);
            }
        }
    }

    public async Task PauseSong()
    {
        await songsMgtFlow.PauseResumeSongAsync(CurrentPositionInSeconds);

    }
    public async Task ResumeSong()
    {
        await songsMgtFlow.PauseResumeSongAsync(CurrentPositionInSeconds);
    }


    #endregion

    static bool IsFoundInCollection(SongModelView song, IEnumerable<SongModelView> songs)
    {
        foreach (var s in songs)
        {
            if (s.LocalDeviceId == song.LocalDeviceId)
                return true;
        }
        return false;
    }
    public async Task SeekSongPosition(LyricPhraseModelView? lryPhrase = null)
    {
        if (lryPhrase is not null)
        {

            CurrentPositionInSeconds =( (lryPhrase.TimeStampMs * 1000) * TemporarilyPickedSong!.DurationInSeconds) / 100;
            await SeekSongPosition(CurrentPositionInSeconds);
            return;
        }
    }

    public async Task SeekSongPosition(double currPosPer = 0)
    {
       await songsMgtFlow.SeekTo(currPosPer);
    }

    public void ToggleRepeatMode()
    {
       RepeatMode = songsMgtFlow.ToggleRepeatMode();

    }

    public void IncredeVolume()
    {
        songsMgtFlow.IncreaseVolume();
        VolumeLevel = songsMgtFlow.VolumeLevel * 100;
    }
    public void DecredeVolume()
    {
        songsMgtFlow.DecreaseVolume();
        VolumeLevel = songsMgtFlow.VolumeLevel * 100;
    }

    public void SetVolume(double vol)
    {
        songsMgtFlow.ChangeVolume(vol);
        VolumeLevel = songsMgtFlow.VolumeLevel;
    }

    public bool ToggleStickToTop()
    {
        IsStickToTop = !IsStickToTop;
        BaseAppFlow.ToggleStickToTop(IsStickToTop);
        return IsStickToTop;
    }
}

