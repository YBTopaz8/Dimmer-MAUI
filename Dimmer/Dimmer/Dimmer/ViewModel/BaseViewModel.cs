using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.Input;
using Dimmer.Services;
using Dimmer.Utilities.FileProcessorUtils;

namespace Dimmer.ViewModel;

public partial class BaseViewModel : ObservableObject
{

    #region Settings Section
    

    [ObservableProperty]
    public partial bool IsLoadingSongs { get; set; }
    [ObservableProperty]
    public partial int SettingsPageIndex { get; set; } = 0;
    [ObservableProperty]
    public partial ObservableCollection<string> FolderPaths { get; set; } = new();


    #endregion

    public const string CurrentAppVersion = "Dimmer v1.8b";

    public bool IsSearching { get; set; } = false;

    private readonly IMapper _mapper;
    private readonly IPlayerStateService _stateService;
    private readonly ISettingsService _settingsService;
    private readonly SubscriptionManager _subs;

    public BaseAppFlow BaseAppFlow { get; }    
    public List<SongModelView>? FilteredSongs { get; set; }
    public AlbumsMgtFlow AlbumsMgtFlow { get; }
    public PlayListMgtFlow PlaylistsMgtFlow { get; }
    public SongsMgtFlow SongsMgtFlow { get; }
    public LyricsMgtFlow LyricsMgtFlow { get; }
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
    public partial ObservableCollection<SongModelView>? PlaylistSongs {get;set;}

    [ObservableProperty]
    public partial ObservableCollection<LyricPhraseModelView>? SynchronizedLyrics {get;set;}

    [ObservableProperty]
    public partial LyricPhraseModelView CurrentLyricPhrase {get;set;}

    [ObservableProperty]
    public partial SongModelView TemporarilyPickedSong {get;set;}
    [ObservableProperty]
    public partial SongModelView SecondSelectedSong {get;set;}

    [ObservableProperty]
    public partial double CurrentPositionInSeconds {get;set;}

    [ObservableProperty]
    public partial double VolumeLevel {get;set;}

    

    [ObservableProperty]
    public partial CurrentPage CurrentlySelectedPage {get;set;}

    public BaseViewModel(
        IMapper mapper,
       BaseAppFlow baseAppFlow,
        AlbumsMgtFlow albumsMgtFlow,
        PlayListMgtFlow playlistsMgtFlow,
        SongsMgtFlow songsMgtFlow,
        IPlayerStateService stateService,
        ISettingsService settingsService,
        SubscriptionManager subs,
        LyricsMgtFlow lyricsMgtFlow)
    {
        _mapper = mapper;
        BaseAppFlow=baseAppFlow;
        AlbumsMgtFlow = albumsMgtFlow;
        PlaylistsMgtFlow = playlistsMgtFlow;
        SongsMgtFlow = songsMgtFlow;
        _stateService = stateService;
        _settingsService = settingsService;
        _subs = subs;
        LyricsMgtFlow=lyricsMgtFlow;
        Initialize();
        //SubscribeToLyricIndexChanges();
        //SubscribeToSyncLyricsChanges();
    }

    private void Initialize()
    {
        ResetMasterListOfSongs();
        SubscribeToCurrentSong();
        SubscribeToDeviceVolume();
        SubscribeToMasterList();
        SubscribeToSecondSelectdSong();
        SubscribeToIsPlaying();
        SubscribeToPosition();
        SubscribeToStateChanges();
        
        CurrentPositionPercentage = 0;
        IsStickToTop = _settingsService.IsStickToTop;
        RepeatMode = _settingsService.RepeatMode;
        //IsShuffle = AppSettingsService.ShuffleStatePreference.GetShuffleState();
    }

    private void SubscribeToStateChanges()
    {
        _subs.Add(_stateService.CurrentPlayBackState.
            DistinctUntilChanged()
            .Subscribe(list =>
            {
                IsPlaying = list == DimmerPlaybackState.Playing;
               
               
            }));
    }

    private void SubscribeToMasterList()
    {
        _subs.Add(_stateService.AllCurrentSongs.
            DistinctUntilChanged()
            .Subscribe(list =>
            {
                if (list.Count == PlaylistSongs.Count)
                    return;
                PlaylistSongs = _mapper.Map<ObservableCollection<SongModelView>>(list);
            }));
    }

    private void SubscribeToLyricIndexChanges()
    {
        _subs.Add(_stateService.CurrentLyric
            .DistinctUntilChanged()
            .Subscribe(l =>
            {
                if (l == null)
                    return;
                CurrentLyricPhrase = _mapper.Map<LyricPhraseModelView>(l);
            }));
    }
    
    private void SubscribeToSyncLyricsChanges()
    {
        _subs.Add(_stateService.SyncLyrics
            .DistinctUntilChanged()
            .Subscribe(l =>
            {
                if (l == null || l.Count<1)
                    return;
                SynchronizedLyrics = _mapper.Map<ObservableCollection<LyricPhraseModelView>>(l);
            }));
    }

    private void ResetMasterListOfSongs()
    {
     
        PlaylistSongs ??= new ObservableCollection<SongModelView>();
        PlaylistSongs.Clear();
        if(BaseAppFlow.MasterList is not null)
        {
            if(BaseAppFlow.MasterList.Count == PlaylistSongs.Count)
                return;
            PlaylistSongs = _mapper.Map<ObservableCollection<SongModelView>>(BaseAppFlow.MasterList);
        }
    
    }

    public void SetCurrentlyPickedSong(SongModelView song)
    {
        if (song == null)
            return;
        if (SecondSelectedSong != null && TemporarilyPickedSong is not null)
        {
            if(SecondSelectedSong == song)
            {
                SecondSelectedSong  = TemporarilyPickedSong;
            }
        }
        else if(SecondSelectedSong == TemporarilyPickedSong)
        {

            SecondSelectedSong = song;
        }

        _stateService.SetSecondSelectdSong(_mapper.Map<SongModel>(song));

    }

    private void SubscribeToSecondSelectdSong()
    {
        _subs.Add(_stateService.SecondSelectedSong
            .DistinctUntilChanged()
            .Subscribe(song =>
            {
                if(string.IsNullOrEmpty(song.FilePath))
                {
                    SecondSelectedSong=new();
                    return;
                }

                if (SecondSelectedSong != null)
                    SecondSelectedSong.IsCurrentPlayingHighlight = false;

                SecondSelectedSong = _mapper.Map<SongModelView>(song);
                if (SecondSelectedSong != null)
                {
                    SecondSelectedSong.IsCurrentPlayingHighlight = true;
                }
            }));
    }
    private void SubscribeToCurrentSong()
    {
        _subs.Add(_stateService.CurrentSong
            .DistinctUntilChanged()
            .Subscribe(song =>
            {
                if (string.IsNullOrEmpty(song.FilePath))
                {
                    TemporarilyPickedSong=new();
                    return;
                }
                var coverPath = FileCoverImageProcessor.SaveOrGetCoverImageToFilePath(song.FilePath, isDoubleCheckingBeforeFetch: true);
                song.CoverImagePath = coverPath;
                if (TemporarilyPickedSong != null)
                {
                    TemporarilyPickedSong.IsCurrentPlayingHighlight = false;

                    TemporarilyPickedSong = song;

                    SecondSelectedSong = song;

                    TemporarilyPickedSong.IsCurrentPlayingHighlight = true;
                    AppTitle = $"{TemporarilyPickedSong.Title} - {TemporarilyPickedSong.ArtistName} [{TemporarilyPickedSong.AlbumName}] | {CurrentAppVersion}";
                }
                else
                {
                    AppTitle = CurrentAppVersion;
                }

            }));
    }


    private void SubscribeToDeviceVolume()
    {
        _subs.Add(
            SongsMgtFlow.Volume
                .DistinctUntilChanged()  
                .StartWith(1)
                .Subscribe(s =>
                {
                    VolumeLevel = s;
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
                    if (TemporarilyPickedSong is null)
                    {
                        return;
                    }
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



    public void PlaySong(
     SongModelView song,
     CurrentPage source,
     IEnumerable<SongModelView>? listOfSongs = null)
    {
        // 1) Un‑highlight the old song
        if (TemporarilyPickedSong != null)
            TemporarilyPickedSong.IsCurrentPlayingHighlight = false;

        // 2) Highlight and pick the new song
        TemporarilyPickedSong = song;
        song.IsCurrentPlayingHighlight = true;

        PlayerStateService.IsShuffleOn = IsShuffle;

        _stateService.SetCurrentSong(_mapper.Map<SongModel>(song));
        if (source == CurrentPage.HomePage)
        {
            if (IsSearching)
            {
                PlaylistModel CustomPlaylist = new()
                {
                    PlaylistName = "Search Playlist "+DateTime.Now.ToLocalTime(),
                    Description = "Custom Playlist by Dimmer",
                };
                var  domainList = FilteredSongs
           .Select(vm => _mapper.Map<SongModel>(vm))
           .ToList()
           .AsReadOnly();
                _stateService.SetCurrentPlaylist(domainList, CustomPlaylist);
            }
            else
            {
                _stateService.SetCurrentPlaylist(null);
            }
        }
        else
        {

            PlaylistModel CustomPlaylist = new()
            {
                PlaylistName = "Custom Playlist",
                Description = "Custom Playlist by Dimmer",
            };
            var domainList = listOfSongs
           .Select(vm => _mapper.Map<SongModel>(vm))
           .ToList()
           .AsReadOnly();

            _stateService.SetCurrentPlaylist( domainList,  CustomPlaylist);
        }
        
        //this triggers the pl flow and song mgt flow
        _stateService.SetCurrentState(DimmerPlaybackState.Playing);

    }

    public void PlayNext(bool IsByUser)
    {
        if (IsByUser)
        {
            _stateService.SetCurrentState(DimmerPlaybackState.PlayNextUI);
            _stateService.SetCurrentState(DimmerPlaybackState.Playing);
        }
    }

    public void PlayPrevious()
    {
        _stateService.SetCurrentState(DimmerPlaybackState.PlayNextUI);
        _stateService.SetCurrentState(DimmerPlaybackState.Playing);
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
        _stateService.SetCurrentState(DimmerPlaybackState.ShuffleRequested);

        SongsMgtFlow.ToggleShuffle(IsShuffle);
        
    }

    public void ToggleRepeatMode()
    {
        RepeatMode = SongsMgtFlow.ToggleRepeatMode();
        
    }

    public void IncreaseVolume()
    {
        SongsMgtFlow.IncreaseVolume();
    }

    public void DecreaseVolume()
    {
        SongsMgtFlow.DecreaseVolume();
    }

    public void SetVolume(double vol)
    {
        SongsMgtFlow.ChangeVolume(vol);
    }

    public void SeekTo(double position, bool isByUser)
    {
        if (!isByUser)
        {
            return;
        }
        _ = SongsMgtFlow.SeekTo(position);
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

    partial void OnCurrentPositionInSecondsChanging(double oldValue, double newValue)
    {
        if (newValue != oldValue)
        {
            SeekTo(newValue, false);
        }
    }
    partial void OnVolumeLevelChanging(double oldValue, double newValue)
    {
        
    }

    #region Settings Methods

    List<string> FullFolderPaths = [];
    [RelayCommand]
    public async Task SelectSongFromFolder()
    {

        CancellationTokenSource cts = new();
        CancellationToken token = cts.Token;

        FolderPickerResult res = await CommunityToolkit.Maui.Storage.FolderPicker.Default.PickAsync(token);

        if (res.Folder is null)
        {
            return;
        }
        string? folder = res.Folder?.Path;
        if (string.IsNullOrEmpty(folder))
        {
            return;
        }

        FolderPaths.Add(folder);

        FullFolderPaths.Add(folder);

        AppSettingsService.MusicFoldersPreference.AddMusicFolder(FullFolderPaths);
        
    }


    [RelayCommand]
    public async Task LoadSongsFromFolders()
    {
        try
        {
            DeviceDisplay.Current.KeepScreenOn = true;
            IsLoadingSongs = true;
            if (FolderPaths is null || FolderPaths.Count < 0)
            {
                await Shell.Current.DisplayAlert("Error !", "No Paths to load", "OK");
                IsLoadingSongs = false;
                return;
            }
            
            var loadSongsResult = await Task.Run(()=> BaseAppFlow.LoadSongs([.. FolderPaths]));
            if (loadSongsResult is not null)
            {   
                Debug.WriteLine("Songs Loaded Successfully");
            }
            else
            {
                Debug.WriteLine("No Songs Found");
            }
            IsLoadingSongs = false;
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error During Scanning", ex.Message, "Ok");
        }
        finally
        {
            DeviceDisplay.Current.KeepScreenOn = false;
        }
    }

    public bool ToggleStickToTop()
    {
        IsStickToTop = !IsStickToTop;
        _settingsService.IsStickToTop = IsStickToTop;
        return IsStickToTop;
    }


    #endregion
    public void Dispose()
    {
        _subs.Dispose();
    }
}
