using CommunityToolkit.Mvvm.Input;
using Dimmer.Interfaces.Services;

//using Dimmer.DimmerLive.Models;
using Dimmer.Utilities.FileProcessorUtils;
using System.Diagnostics;
using System.Threading.Tasks;

//using Parse;
//using Parse.Infrastructure;

namespace Dimmer.ViewModel;

public partial class BaseViewModel : ObservableObject
{
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
        LatestAppLog = new() { Log = "Dimmer App Started" };
        UserLocal = new UserModelView();
    }
    #region Settings Section


    [ObservableProperty]
    public partial SongModelView SelectedSong { get; set; }
    [ObservableProperty]
    public partial bool IsLoadingSongs { get; set; }
    [ObservableProperty]
    public partial int SettingsPageIndex { get; set; } = 0;
    [ObservableProperty]
    public partial ObservableCollection<string>? FolderPaths { get; set; } = new();


    #endregion

    public const string CurrentAppVersion = "Dimmer v1.9";

    [ObservableProperty]
    public partial bool IsMainViewVisible { get; set; } = true;
    public static bool IsSearching { get; set; } = false;

    
    public ParseUser? UserOnline { get; set; }
    [ObservableProperty]
    public partial UserModelView UserLocal { get; set; }

    private readonly IMapper _mapper;
    public readonly IDimmerLiveStateService dimmerLiveStateService;
    private readonly IDimmerStateService _stateService;
    private readonly ISettingsService _settingsService;
    private readonly SubscriptionManager _subs;
    private readonly IFolderMgtService _folderMgtService;

    [ObservableProperty]
    public partial string? LatestScanningLog { get; set; }

    [ObservableProperty]
    public partial AppLogModel LatestAppLog { get; set; }
    [ObservableProperty]
    public partial ObservableCollection<AppLogModel> ScanningLogs { get; set; } = new();
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
    public partial ObservableCollection<SongModelView> PlaylistSongs { get; set; } = new();
    [ObservableProperty]
    public partial ObservableCollection<SongModelView> NowPlayingQueue { get; set; } = new();

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

   

    [RelayCommand]
    public async Task SignUpUser()
    {
        await dimmerLiveStateService.SignUpUserAsync(UserLocal);
        SettingsPageIndex=1;
    }

    [ObservableProperty]
    public partial bool IsConnected { get; set; }
    [RelayCommand]
    public async Task LoginUser()
    {
        var usr = _mapper.Map<UserModel>(UserLocal);
        if (await dimmerLiveStateService.LoginUserAsync(usr))
        {
            IsConnected=true;
            SettingsPageIndex=0;
        }
    }
    [ObservableProperty]
    public partial AlbumModelView? SelectedAlbum { get; internal set; }
    [RelayCommand]
    public void FullyBackUpData()
    {
        //dimmerLiveStateService.
    }

    [RelayCommand]
    public void GetMyDevices()
    {

    }


    // i need a method or set of method for user to user communication like instant messaging
    // and file sharing where user a and b can chat, and can share even song data etc..
    // and more methods etc 

    [ObservableProperty]
    public partial ObservableCollection<AlbumModelView>? SelectedAlbumsCol { get; set; }
    [ObservableProperty]
    public partial ObservableCollection<SongModelView>? SelectedAlbumsSongs { get; set; }

    public void SetSelectedAlbumsSongs(ObservableCollection<SongModelView>? songs)
    {

        SelectedAlbumsSongs = songs;
        
    }
    public void OpenAlbumWindow(SongModelView song)
    {
        //show the albums songs
        var songg = BaseAppFlow.MasterList.First(x => x.Id == SecondSelectedSong.Id);
        
        var songgs = BaseAppFlow._mapper.Map<ObservableCollection<SongModelView>>(songg.Album?.Songs);

        //var songB = albumSongs[2];
        //var artistB = songB.ArtistIds[0];
        //var songC = artistB.Songs;
        SetSelectedAlbumsSongs(songgs);
        
    }
    [RelayCommand]
    public async Task LogoutUser()
    {
        //await dimmerLiveStateService.LogoutUser();
        //UserOnline = null;
    }
    [RelayCommand]
    public async Task ForgottenPassword()
    {
        //await dimmerLiveStateService.ForgottenPassword();
    }

    public async Task SaveUserNoteToDB(UserNoteModelView userNote, SongModelView song)
    {

        // 1) Ensure the song has a note list
        song.UserNote ??= [];
        song.UserNote.Add(userNote);

        //DimmerSharedSong pSong = new DimmerSharedSong();
        //pSong.AudioFile= 

        //await pSong.SaveAsync();
        return;

        var songDb = _mapper.Map<SongModel>(song);
        var userNotee = _mapper.Map<UserNoteModel>(userNote);
            
        BaseAppFlow.UpSertSongNote(songDb, userNotee);

        // 2) Find any existing entry in PlaylistSongs by Id
        var existing = PlaylistSongs
            .FirstOrDefault(x => x.Id == song.Id);

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


    public void Initialize()
    {

        

        ResetMasterListOfSongs();
        SubscribeToCurrentSong();
        SubscribeToDeviceVolume();
        SubscribeToMasterList();
        SubscribeToSecondSelectdSong();
        SubscribeToIsPlaying();
        SubscribeToPosition();
        SubscribeToStateChanges();


        SubscribeToAlbumListChanges();
        CurrentPositionPercentage = 0;
        //IsShuffle = AppSettingsService.ShuffleStatePreference.GetShuffleState
       FolderPaths=  _folderMgtService.StartWatchingFolders()?.Select(x=>x.FolderPath).ToObservableCollection();
    }



    public async Task LoginFromSecureData()
    {

        if (UserLocal is null || string.IsNullOrEmpty(UserLocal.Username))
        {
            await dimmerLiveStateService.AttemptAutoLoginAsync();
            UserLocal= dimmerLiveStateService.UserLocalView;
            BaseAppFlow.CurrentUserView = UserLocal;
            IsConnected=true;
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
                        TemporarilyPickedSong = _stateService.CurrentSongValue;
                        break;
                    case DimmerPlaybackState.FolderScanCompleted:
                        LatestAppLog.Log ="Folder Scan Completed";
                        var newlyAddedSongs = (IReadOnlyList<SongModel>)state.ExtraParameter as IReadOnlyList<SongModel>;
                        var songss = _mapper.Map<ObservableCollection<SongModelView>>(newlyAddedSongs);
                        foreach (var song in songss)
                        {
                            PlaylistSongs.Add(song);
                        }
                        LatestAppLog.Log = $"{count++}Playlist Updated with {songss.Count} new additions";
                        Debug.WriteLine($"{count++}Playlist Updated with {songss.Count} new additions");
                        break;
               
                        break;
                    default:
                        break;
                }
            }));
    }

    int count = 0;

    
    private void SubscribeToMasterList()
    {
        _subs.Add(_stateService.AllCurrentSongs.
            DistinctUntilChanged()
            .Subscribe(list =>
            {

                NowPlayingQueue ??= _mapper.Map<ObservableCollection<SongModelView>>(list);
            }));
    }
    //[ObservableProperty]
    //public partial ObservableCollection<ChatConversation> Conversations { get; set; } = new();
    //[ObservableProperty] 
    //public partial ObservableCollection<ChatMessage> ActiveMessageCollection { get; set; } = new();
    //[ObservableProperty] 
    //public partial ChatMessage ActiveMessages { get; set; } 
    //[ObservableProperty] 
    //public partial ChatConversation ActiveConversation { get; set; } 
    //[ObservableProperty] 
    //public partial string Message { get; set; } = string.Empty;
    
    [RelayCommand]
    public async Task SwitchRecipient(string id)
    {
        //ActiveConversation =  await dimmerLiveStateService.GetOrCreateConversationWithUserAsync(id);
       
    }
    [RelayCommand]
    public async Task SendMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
            return;
       //ChatMessage? msg=  await dimmerLiveStateService.SendTextMessageAsync(ActiveConversation, message);
       //ActiveMessageCollection.Add(msg);
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

                SecondSelectedSong?.IsCurrentPlayingHighlight = false;

                SecondSelectedSong = _mapper.Map<SongModelView>(song);
                SecondSelectedSong.IsCurrentPlayingHighlight = true;

                TemporarilyPickedSong ??= SecondSelectedSong;
            }));
    }
    private void SubscribeToCurrentSong()
    {
        _subs.Add(_stateService.CurrentSong
            
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

    
    public async Task ShareSong()
    {

        //await dimmerLiveStateService.ShareSongOnline(SecondSelectedSong, CurrentPositionInSeconds);
    }

    public async Task PlaySong(
     SongModelView song,
     CurrentPage source,
     IEnumerable<SongModelView>? listOfSongs = null)
    {
        // 1) Un‑highlight the old song
        TemporarilyPickedSong?.IsCurrentPlayingHighlight = false;

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
                    Id=ObjectId.GenerateNewId(),
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
                Id=ObjectId.GenerateNewId(),
                PlaylistName = "Custom Playlist",
                Description = "Custom Playlist by Dimmer",
            };
            var domainList = listOfSongs
           .Select(vm => _mapper.Map<SongModel>(vm))
           .ToList()
           .AsReadOnly();

            _stateService.SetCurrentPlaylist( domainList,  CustomPlaylist);
        }

       await  SongsMgtFlow.SetPlayState();
    }

    public async Task PlayNext(bool IsByUser)
    {
        if (IsByUser)
        {
            _stateService.SetCurrentState(new(DimmerPlaybackState.PlayNextUI , null));
            await SongsMgtFlow.SetPlayState();
        }
    }

    public async Task PlayPrevious()
    {
        _stateService.SetCurrentState(new(DimmerPlaybackState.PlayNextUI, null));
        await SongsMgtFlow.SetPlayState();
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
        _stateService.SetCurrentState(new(DimmerPlaybackState.ShuffleRequested,null));

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

    [RelayCommand]
    public async Task PickNewProfileImage()
    {
        CancellationTokenSource cts = new();
        CancellationToken token = cts.Token;
        var res = await FilePicker.Default.PickAsync(new PickOptions()
        {
            PickerTitle = "Pick a new profile image",
            FileTypes = FilePickerFileType.Images,
        });
        if (res is null)
            return;
        var file = res.FullPath;
        UserLocal.UserProfileImage = file;

        //dimmerLiveStateService.SaveUserLocally(UserLocal);
    }

    #region Settings Methods

    List<string> FullFolderPaths = [];

    bool hasAlreadyActivated=false;
    [RelayCommand]
    public void DeleteFolderPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return;


        FolderPaths.Remove(path);

        _folderMgtService.RemoveFolderFromPreference(path);
    }
    
    public async Task SelectSongFromFolder(string? pathToOverride=null)
    {

        CancellationTokenSource cts = new();
        CancellationToken token = cts.Token;

        FolderPickerResult res = await FolderPicker.Default.PickAsync(token);

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
        
        FolderPaths?.Add(folder);

        FullFolderPaths.Add(folder);

        if (FolderPaths?.Count == 1 && !hasAlreadyActivated)
        {
            BaseAppFlow.Initialize();
            
            Initialize();
            hasAlreadyActivated=true;
        }

        if (pathToOverride is not null)
        {
            FolderPaths?.Remove(pathToOverride);
            _folderMgtService.RemoveFolderFromPreference(pathToOverride);
        }


        _folderMgtService.AddFolderToPreference(folder);
        
        
    }

    //[ObservableProperty]
    //public partial ObservableCollection<UserDeviceSession>? UserDevices { get; set; } = new();

    public void SubscribeToScanningLogs()
    {
        _subs.Add(_stateService.LatestDeviceLog.DistinctUntilChanged()
            .Subscribe(log =>
            {


                if (log == null)
                    return;

                
                if (string.IsNullOrEmpty(log.Log))
                    return;
                LatestScanningLog = log.Log;
                LatestAppLog = log;
                if (log is not null)
                { 
                    //log.SharedSong.AudioFile.Url
                  // log. log.SharedSong.AudioFile.Url is the link to song, i need to download song and save in local device
                    return;
                }
                ScanningLogs ??= new ObservableCollection<AppLogModel>();
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (ScanningLogs.Count > 10)
                        ScanningLogs.RemoveAt(0);
                    ScanningLogs.Add(log);
                });
            }));
    }


    //[ObservableProperty]
    //public partial DimmerSharedSong? SharedSong { get; set; }
    public void FetchSharedSongById(string songId)
    {
        //SharedSong = await dimmerLiveStateService.FetchSharedSongByCodeAsync(songId);

    }

    [RelayCommand]
    public async Task UpdateUserProfileImage()
    {
        var imagePath = await FilePicker.PickAsync(new PickOptions()
        {
            FileTypes = FilePickerFileType.Images,
            PickerTitle = "Choose a New Image for your Profile"
        });
        if (imagePath is not null)
        {
            UserLocal.UserProfileImage = imagePath.FullPath;
        }
        BaseAppFlow.UpSertUser(_mapper.Map<UserModel>(UserLocal));
    }

    [RelayCommand]
    public static void ToggleShowCloseConfPopUp(bool IsShow)
    {
        BaseAppFlow.DimmerAppState.IsShowCloseConfirmation=IsShow;
    }

    [RelayCommand]
    public void DoneFirstSetup()
    {
        AppUtils.IsUserFirstTimeOpening = false;
        
        Application.Current.CloseWindow(Application.Current.Windows[1]);
        
    }
    [RelayCommand]
    public static void ToggleIsStickToTop(bool IsStick)
    {
        BaseAppFlow.DimmerAppState.IsShowCloseConfirmation=IsStick;
    }

  

    public bool ToggleStickToTop()
    {
        IsStickToTop = !IsStickToTop;
        _settingsService.IsStickToTop = IsStickToTop;
        return IsStickToTop;
    }


    #endregion

    public void SetSelectedSong(SongModelView song)
    {
        if (song == null)
            return;
        SelectedSong =song;
        //ScrollToCurrentlyPlayingSong();
    }
    private void SubscribeToAlbumListChanges()
    {


        _subs.Add(
            AlbumsMgtFlow.SpecificAlbums
                .Subscribe(albums =>
                {
                    if (albums == null)
                        return;
                    if (albums.Count > 0)
                    {
                        SelectedAlbumsSongs = _mapper.Map<ObservableCollection<SongModelView>>(albums[0].Songs);
                        SelectedAlbumsCol = _mapper.Map<ObservableCollection<AlbumModelView>>(albums);

                        SelectedAlbum = SelectedAlbumsCol[0];

                    }
                })
        );
    }

    public void GetAlbumForSpecificSong(SongModelView song)
    {
        AlbumsMgtFlow.GetAlbumsBySongId(song.Id);
    }

    public void PlaySong(SongModelView song)
    {
        SelectedSong.IsCurrentPlayingHighlight = false;
        SelectedSong = song;
        PlaySong(song, CurrentPage.SpecificAlbumPage, SelectedAlbumsSongs);

    }

    public void LoadAlbum()
    {

    }


    public void Dispose()
    {
       _subs.Dispose();
    }
}
