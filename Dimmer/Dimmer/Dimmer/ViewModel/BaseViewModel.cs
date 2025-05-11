using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.Input;
using Dimmer.DimmerLive.Models;
using Dimmer.Services;
using Dimmer.Utilities.FileProcessorUtils;
using System.Diagnostics;
using System.Threading.Tasks;

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

    public const string CurrentAppVersion = "Dimmer v1.9";

    [ObservableProperty]
    public partial bool IsMainViewVisible { get; set; } = true;
    public static bool IsSearching { get; set; } = false;

    [ObservableProperty]
    public partial ParseUser? UserOnline { get; set; }
    [ObservableProperty]
    public partial UserModelView UserLocal { get; set; }

    private readonly IMapper _mapper;
    private readonly IDimmerLiveStateService dimmerLiveStateService;
    private readonly IDimmerStateService _stateService;
    private readonly ISettingsService _settingsService;
    private readonly SubscriptionManager _subs;
    private readonly IFolderMgtService _folderMgtService;

    [ObservableProperty]
    public partial string? LatestScanningLog { get; set; }
    [ObservableProperty]
    public partial ObservableCollection<AppLogModel>? ScanningLogs { get; set; }
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

    public BaseViewModel(IMapper mapper, BaseAppFlow baseAppFlow,
        IDimmerLiveStateService dimmerLiveStateService,
        AlbumsMgtFlow albumsMgtFlow, PlayListMgtFlow playlistsMgtFlow, 
        SongsMgtFlow songsMgtFlow, IDimmerStateService stateService, 
        ISettingsService settingsService, 
        SubscriptionManager subs, 
        LyricsMgtFlow lyricsMgtFlow,
        IFolderMgtService folderMgtService
        )
    {
        _mapper = mapper;
        BaseAppFlow=baseAppFlow;
        this.dimmerLiveStateService=dimmerLiveStateService;
        AlbumsMgtFlow = albumsMgtFlow;
        PlaylistsMgtFlow = playlistsMgtFlow;
        SongsMgtFlow = songsMgtFlow;
        _stateService = stateService;
        _settingsService = settingsService;
        _subs = subs;
        _folderMgtService=folderMgtService;
        LyricsMgtFlow=lyricsMgtFlow;
        Initialize();
        UserLocal= new UserModelView();
    }

    [RelayCommand]
    public async Task SignUpUser()
    {
        try
        {
            UserOnline = await ParseClient.Instance.SignUpWithAsync(UserLocal.UserName, UserLocal.UserPassword);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert(AppTitle, ex.Message, "OK");
        }

    }

    [ObservableProperty]
    public partial bool IsConnected { get; set; }
    [RelayCommand]
    public async Task LoginUser()
    {
        UserOnline = await ParseClient.Instance.LogInWithAsync(UserLocal.UserName, UserLocal.UserPassword);


        if (UserOnline.SessionToken is not null)
        {
            IsConnected=true;

            await SecureStorage.SetAsync("username", UserOnline.Username);
            await SecureStorage.SetAsync("Password", UserOnline.Password);
            await SecureStorage.SetAsync("ObjectId", UserOnline.ObjectId);
            await SecureStorage.SetAsync("SessionToken", UserOnline.SessionToken);
            await SecureStorage.SetAsync("Email", UserOnline.Email);
            SettingsPageIndex = 2;

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Shell.Current.DisplayAlert("Success", "User logged in successfully", "OK");
            });
        }
    }

    [RelayCommand]
    public async Task LogoutUser()
    {
        await ParseClient.Instance.LogOutAsync(CancellationToken.None);
        UserOnline = null;
    }
    [RelayCommand]
    public async Task ForgottenPassword()
    {
        await ParseClient.Instance.RequestPasswordResetAsync(UserLocal.UserEmail);
        await Shell.Current.DisplayAlert("Success", "Password reset email sent", "OK");
    }

    public async Task SaveUserNoteToDB(UserNoteModelView userNote, SongModelView song)
    {

        // 1) Ensure the song has a note list
        song.UserNote ??= [];
        song.UserNote.Add(userNote);

        ParseSong pSong = new ParseSong();
        //pSong.AudioFile= 

        //await pSong.SaveAsync();
        return;

        var songDb = _mapper.Map<SongModel>(song);
        var userNotee = _mapper.Map<UserNoteModel>(userNote);
            
        BaseAppFlow.UpSertSongNote(songDb, userNotee);

        // 2) Find any existing entry in PlaylistSongs by LocalDeviceId
        var existing = PlaylistSongs
            .FirstOrDefault(x => x.LocalDeviceId == song.LocalDeviceId);

        if (existing != null)
        {
            // Replace the old object with the updated one
            var idx = PlaylistSongs.IndexOf(existing);
            PlaylistSongs[idx] = song;
        }
        else
        {
            // It wasn’t in the list yet, so add it
            PlaylistSongs.Add(song);
        }

        // 3) Persist to database
    }


    private void Initialize()
    {

        if (AppUtils.IsUserFirstTimeOpening)
        {
            //Application.Current.m
            return;
        }

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
        //IsShuffle = AppSettingsService.ShuffleStatePreference.GetShuffleState

        Task.Run(async () => await LoginFromSecureData());
    }

    public async Task LoginFromSecureData()
    {
        IsConnected=true;

        var uName =  await SecureStorage.GetAsync("username");
        var uPass = await SecureStorage.GetAsync("Password");
        var uObjectId = await SecureStorage.GetAsync("ObjectId");
        var uSessionToken = await SecureStorage.GetAsync("SessionToken");
        var uEmail = await SecureStorage.GetAsync("Email");

        UserOnline = await ParseClient.Instance.LogInWithAsync(uName, uPass);
        if (UserOnline.SessionToken is not null)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Shell.Current.DisplayAlert("Success", "User logged in successfully", "OK");
            });
        }

    }

    private void SubscribeToStateChanges()
    {
        _subs.Add(_stateService.CurrentPlayBackState.
            DistinctUntilChanged()
            .Subscribe(state =>
            {
                IsPlaying = state.State == DimmerPlaybackState.Playing;
                switch (state.State)
                {
                    case DimmerPlaybackState.Opening:
                        break;
                    case DimmerPlaybackState.Stopped:
                        break;
                    case DimmerPlaybackState.Playing:
                        break;
                    case DimmerPlaybackState.Resumed:
                        break;
                    case DimmerPlaybackState.PausedUI:
                        break;
                    case DimmerPlaybackState.PausedUser:
                        break;
                    case DimmerPlaybackState.Loading:
                        break;
                    case DimmerPlaybackState.Error:
                        break;
                    case DimmerPlaybackState.Failed:
                        break;
                    case DimmerPlaybackState.Previewing:
                        break;
                    case DimmerPlaybackState.LyricsLoad:
                        break;
                    case DimmerPlaybackState.ShowPlayBtn:
                        break;
                    case DimmerPlaybackState.ShowPauseBtn:
                        break;
                    case DimmerPlaybackState.RefreshStats:
                        break;
                    case DimmerPlaybackState.Initialized:
                        break;
                    case DimmerPlaybackState.Ended:
                        break;
                    case DimmerPlaybackState.CoverImageDownload:
                        break;
                    case DimmerPlaybackState.LoadingSongs:
                        break;
                    case DimmerPlaybackState.SyncingData:
                        break;
                    case DimmerPlaybackState.Buffering:
                        break;
                    case DimmerPlaybackState.DoneScanningData:
                        break;
                    case DimmerPlaybackState.PlayCompleted:
                        break;
                    case DimmerPlaybackState.PlayPreviousUI:
                        break;
                    case DimmerPlaybackState.PlayPreviousUser:
                        break;
                    case DimmerPlaybackState.PlayNextUI:
                        break;
                    case DimmerPlaybackState.PlayNextUser:
                        break;
                    case DimmerPlaybackState.Skipped:
                        break;
                    case DimmerPlaybackState.RepeatSame:
                        break;
                    case DimmerPlaybackState.RepeatAll:
                        break;
                    case DimmerPlaybackState.RepeatPlaylist:
                        break;
                    case DimmerPlaybackState.MoveToNextSongInQueue:
                        break;
                    case DimmerPlaybackState.ShuffleRequested:
                        break;
                    case DimmerPlaybackState.FolderAdded:
                        break;
                    case DimmerPlaybackState.FolderRemoved:
                        break;
                    case DimmerPlaybackState.FileChanged:
                        break;
                    case DimmerPlaybackState.FolderNameChanged:
                        break;
                    case DimmerPlaybackState.FolderScanCompleted:
                        break;
                    case DimmerPlaybackState.FolderScanStarted:
                        break;
                    case DimmerPlaybackState.FolderWatchStarted:
                        break;
                    default:
                        break;
                }
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

        DimmerStateService.IsShuffleOn = IsShuffle;

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
                _stateService.SetCurrentPlaylist(BaseAppFlow.MasterList);
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
        _stateService.SetCurrentState((DimmerPlaybackState.Playing, null));

    }

    public void PlayNext(bool IsByUser)
    {
        if (IsByUser)
        {
            _stateService.SetCurrentState((DimmerPlaybackState.PlayNextUI , null));
            
        }
    }

    public void PlayPrevious()
    {
        _stateService.SetCurrentState((DimmerPlaybackState.PlayNextUI, null));
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
        _stateService.SetCurrentState((DimmerPlaybackState.ShuffleRequested,null));

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

    [RelayCommand]
    public void ToggleSettingsPage()
    {
        IsMainViewVisible = !IsMainViewVisible;

    }

    #region Settings Methods

    List<string> FullFolderPaths = [];

    bool hasAlreadyActivated=false;
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

        AppUtils.IsUserFirstTimeOpening=false;
        
        FolderPaths.Add(folder);

        FullFolderPaths.Add(folder);

        if (FolderPaths.Count == 1 && !hasAlreadyActivated)
        {
            BaseAppFlow.Initialize();
            
            Initialize();
            hasAlreadyActivated=true;
        }
        _folderMgtService.AddFolderToPreference(folder);
        
        
    }


    public void SubscribeToScanningLogs()
    {
        _subs.Add(_stateService.LatestDeviceLog.DistinctUntilChanged()
            .Subscribe(log =>
            {
                if (log == null || string.IsNullOrEmpty(log.Log))
                    return;
                LatestScanningLog = log.Log;
                ScanningLogs ??= new ObservableCollection<AppLogModel>();
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (ScanningLogs.Count > 10)
                        ScanningLogs.RemoveAt(0);
                    ScanningLogs.Add(log);
                });
            }));
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
