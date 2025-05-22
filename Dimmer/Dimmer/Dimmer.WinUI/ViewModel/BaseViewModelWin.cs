using CommunityToolkit.Mvvm.Input;
//using Dimmer.DimmerLive;
//using Dimmer.DimmerLive.Interfaces;
//using Dimmer.DimmerLive.Models;
using Dimmer.WinUI.Utils.Models;
using SkiaSharp;
using Vanara.Windows.Shell;
using ZXing;
using ZXing.Net.Maui.Controls;
using ZXing.SkiaSharp;
using static Vanara.PInvoke.Shell32;

namespace Dimmer.WinUI.ViewModel;

public partial class BaseViewModelWin : BaseViewModel, IDisposable
{

    [ObservableProperty]
    public partial string? barCodeInvitationValue { get; set; } = ParseClient.Instance.CurrentUserController.CurrentUser?.ObjectId;

    [ObservableProperty]
    public partial int CurrentQueue { get; set; }
    private readonly SubscriptionManager _subs;
    private readonly IFilePicker filePicker;

    [ObservableProperty]
    public partial ObservableCollection<SongModelView>? DisplayedSongs { get; set; }

    [ObservableProperty]
    public partial CollectionView? SongLyricsCV { get; set; }

    private readonly IDimmerStateService _stateService;

    private readonly IMapper _mapper;

    

    private TrayIconHelper? _trayIconHelper;

    public BaseViewModelWin(IMapper mapper,
        BaseAppFlow baseAppFlow,
        IDimmerLiveStateService dimmerLiveStateService,
        AlbumsMgtFlow albumsMgtFlow,
        PlayListMgtFlow playlistsMgtFlow,
        SongsMgtFlow songsMgtFlow,
        IDimmerStateService stateService,
        ISettingsService settingsService,
        SubscriptionManager subs,
        LyricsMgtFlow lyricsMgtFlow,
        IFolderMgtService folderMgtService,
        IFilePicker filePicker
    ) : base(mapper, baseAppFlow, dimmerLiveStateService, albumsMgtFlow, playlistsMgtFlow, songsMgtFlow, stateService, settingsService, subs, lyricsMgtFlow, folderMgtService)
    {
        _mapper = mapper;
        _stateService = stateService;
        _subs = subs;
        this.filePicker=filePicker;


    }

    
    public bool InitializeWindows()
    {
        IsSettingWindoOpened=true;

        var win = Application.Current.Windows[0];
        

        PlatUtils.OpenSettingsWindow();
        
        if (AppUtils.IsUserFirstTimeOpening)
        {
            //IsMainViewVisible = false;
            return false;
        }
        ResetDisplayedMasterList();
        SubscribeToLyricIndexChanges();

        SubscribeToPosition();
        return true;
    }

    private void SubscribeToPosition()
    {

        
        _subs.Add(SongsMgtFlow.Position
            .Synchronize(SynchronizationContext.Current!)
        .Subscribe(pos =>
        {
            if (pos == 0)
            {
                TaskbarList.SetProgressState(PlatUtils.DimmerHandle, (TaskbarButtonProgressState)TBPFLAG.TBPF_NORMAL);

                return;
            }
                CurrentPositionInSeconds = pos;
                var duration = SongsMgtFlow.CurrentlyPlayingSong?.DurationInSeconds ?? 1;
                CurrentPositionPercentage = pos / duration;
                MainThread.BeginInvokeOnMainThread(
               () =>
               {
                   TaskbarList.SetProgressValue(PlatUtils.DimmerHandle, (ulong)CurrentPositionPercentage*100, 100);
               });

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
                MainThread.BeginInvokeOnMainThread(
                    () =>
                    {
                        SongLyricsCV?.ScrollTo(CurrentLyricPhrase, null, ScrollToPosition.Center, true);
                    });


            }));
    }
    public void ResetDisplayedMasterList()
    {

        // Initialize displayed songs to the full master list
        if (BaseAppFlow.MasterList!= null)
        {
            var e = _mapper.Map<ObservableCollection<SongModelView>>(BaseAppFlow.MasterList);
            DisplayedSongs = [.. e];
        }

    }


    [RelayCommand]
    public void OpenSpecificChatConversation(string userId)
    {

        
        //if (string.IsNullOrEmpty(userId))
        //    return;
        //var ss = await  dimmerLiveStateService.GetOrCreateConversationWithUserAsync(userId);
        //if (ss == null)
        //    return;

        //Debug.WriteLine(ss.Name);
    }

    static void IfUserOnlineIsNull()
    {
        //UserOnline ??=  dimmerLiveStateService.UserOnline;
    }

    [RelayCommand]
    public async Task ShareProfile(BarcodeGeneratorView bCodeView)
    {

        IfUserOnlineIsNull();




        var screenshot = await bCodeView.CaptureAsync();
        var fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"qr_{DateTime.UtcNow.Ticks}.png");

        using Stream fileStream = File.OpenWrite(fileName);
        await screenshot.CopyToAsync(fileStream, ScreenshotFormat.Png);
        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = "Share QR Code",
            File = new ShareFile(fileName)
        });

      
    }

    [ObservableProperty]
    public partial ObservableCollection<WindowInfo> WindowsOpened { get; set; } = new ObservableCollection<WindowInfo>();
    [ObservableProperty]
    public partial bool IsSettingWindoOpened { get; set; }


    public Task LoadOnlineData()
    {
        IfUserOnlineIsNull();
        //var qrData = new QrCodeData
        //{
        //    EventType = QrEventTypes.AddUser,
        //    EventId = ParseClient.Instance.CurrentUserController.CurrentUser?.ObjectId,
        //    Timestamp = DateTime.UtcNow.ToString("o"), // ISO 8601 format
        //    SenderId = ParseClient.Instance.CurrentUserController.CurrentUser?.ObjectId,
        //    SenderName = UserOnline.Username, // Or a display name property
        //    Payload = new Dictionary<string, object>
        //    {
        //        { "username", UserOnline.Username }

        //    }
        //};

        //string jsonPayload = JsonSerializer.Serialize(qrData);
        //barCodeInvitationValue = jsonPayload;

        //barCodeInvitationValue = ParseClient.Instance.CurrentUserController.CurrentUser?.ObjectId;
        //ShareProfile();
        return Task.CompletedTask;
    }

    [RelayCommand]
    public async Task AddNewUser()
    {
        //var file = await filePicker.PickAsync(new PickOptions
        //{
        //    PickerTitle = "Select a QR Invite Image",
        //    FileTypes = FilePickerFileType.Images,
        //});

        var file = await FilePicker.PickAsync(new PickOptions
        {
            PickerTitle = "Select a QR image",
            FileTypes = FilePickerFileType.Images
        });

        if (file == null)
            return;

        using var stream = await file.OpenReadAsync();
        using var skStream = new SKManagedStream(stream);
        using var bitmap = SKBitmap.Decode(skStream);

        if (bitmap == null)
            return;

        // Use the generic reader with SKBitmap
        var reader = new BarcodeReader<SKBitmap>(bmp => new SKBitmapLuminanceSource(bmp));
        var result = reader.Decode(bitmap);

        var s = result?.Text;
        if (s == null)
            return;
    }

    public void ShareSongOnline()
    {

        DimmerSongWindow newWin = new DimmerSongWindow(this);

        var dq = DispatcherQueue.GetForCurrentThread();
        dq.TryEnqueue(() =>
        {
            Application.Current!.OpenWindow(newWin);
            
        });
        //await base.ShareSong();
        

    }

    public void Dispose()
    {
        // if you registered any additional subscriptions here, dispose them
    }
}
