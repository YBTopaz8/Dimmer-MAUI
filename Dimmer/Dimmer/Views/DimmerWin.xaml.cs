using System.Diagnostics;
using System.Threading.Tasks;

using Dimmer.Interfaces.Services.Interfaces;

namespace Dimmer.WinUI;

public partial class DimmerWin : Window
{
    public BaseViewModel MyViewModel { get; set; }
    public IAppUtil AppUtil { get; }

    public DimmerWin(BaseViewModel vm, IAppUtil appUtil)
    {
        InitializeComponent();
        //vm.MainMAUIWindow=this;
        Page = appUtil.GetShell();
        MyViewModel= vm;
        AppUtil=appUtil;
        BindingContext=vm;
        
    }
    private void AppShell_Loaded(object? sender, EventArgs e)
    {
        
    }


    protected async override void OnDestroying()
    {
        //MyViewModel.windowManager.CloseWindow(this);
        MyViewModel.OnAppClosing();
        if (!AppSettingsService.ShowCloseConfirmationPopUp.GetCloseConfirmation())
        {
            SubscriptionManager subMgr = IPlatformApplication.Current!.Services.GetService<SubscriptionManager>()!;
            MyViewModel.OnCloseAllWindows();
            var dimmerAudio = IPlatformApplication.Current!.Services.GetService<IDimmerAudioService>();
            if (dimmerAudio is not null)
            {
                dimmerAudio.Stop();
                await dimmerAudio.DisposeAsync();
            }
            subMgr.Dispose();
            base.OnDestroying();

            return;
        }
        bool result = await Shell.Current.DisplayAlert(
            "Confirm Action",
            "You sure want to close app?",
            "Yes",
            "Cancel");
        if (!result)
        {
            return;
        }

        MyViewModel.OnCloseAllWindows();

    }
   

    public event EventHandler? WindowActivated;
    int count;
    protected override async void OnActivated()
    {
        count++;
        try
        {
            
            base.OnActivated();
            if (MyViewModel.IsLastFMNeedsToConfirm)
            {
                bool isLastFMAuthorized = await Shell.Current.DisplayAlert("LAST FM Confirm", "Is Authorization done?", "Yes", "No");
                if (isLastFMAuthorized)
                {
                    await MyViewModel.CompleteLastFMLoginAsync();
                }
                else
                {
                    MyViewModel.IsLastFMNeedsToConfirm= false;
                    await Shell.Current.DisplayAlert("Action Cancelled", "Last FM Authorization Cancelled", "OK");

                }
            }
            MyViewModel.MainWindow_Activated();
          
        }
        catch (Exception ex)
        {

            Debug.WriteLine($"{ex.Message}");
        }
    }
    protected override void OnBackgrounding(IPersistedState state)
    {
        base.OnBackgrounding(state);
    }

    protected override void OnDeactivated()
    {
        base.OnDeactivated();

#if WINDOWS
        var nativeElement = this.Page?.Handler?.PlatformView as Microsoft.UI.Xaml.UIElement;
        if (nativeElement != null)
        {
            nativeElement.PointerPressed -= OnGlobalPointerPressed;
        }
        //PlatUtils.ToggleFullScreenMode(false, PlatUtils.AppWinPresenter);
        //PlatUtils.ShowWindow(PlatUtils.DimmerHandle, 0); // Hide window (SW_HIDE)
#endif
    }
   
    protected override void OnCreated()
    {
#if WINDOWS
        MyViewModel.windowManager.TrackWindow(this);
        base.OnCreated();
#if DEBUG
        DimmerTitleBar.BackgroundColor = Microsoft.Maui.Graphics.Colors.DarkRed;
#elif RELEASE
#endif
        if (MyViewModel is null)
        {
            return;
        }
        MyViewModel.OnAppOpening();
        StickTopImgBtn.IsVisible = MyViewModel.IsStickToTop;
        UnStickTopImgBtn.IsVisible = !MyViewModel.IsStickToTop;

        this.Height=1024;
        this.Width=1600;

#endif
    }
    protected override void OnStopped()
    {
        base.OnStopped();
    }
    private void StickTopImgBtn_Clicked(object sender, EventArgs e)
    {
        StickTopImgBtn.IsVisible = false;
        UnStickTopImgBtn.IsVisible = true;

#if WINDOWS
        PlatUtils.ToggleWindowAlwaysOnTop(true, PlatUtils.AppWinPresenter);
#endif
    }

    private void UnStickTopImgBtn_Clicked(object sender, EventArgs e)
    {
        StickTopImgBtn.IsVisible = true;
        UnStickTopImgBtn.IsVisible = false;
#if WINDOWS
        PlatUtils.ToggleWindowAlwaysOnTop(false, PlatUtils.AppWinPresenter);
#endif
    }

#if WINDOWS
    private TrayIconHelper? _trayIconHelper;
#endif
    private void Minimize_Clicked(object sender, EventArgs e)
    {
#if WINDOWS
        //PlatUtils.ToggleFullScreenMode(true, PlatUtils.AppWinPresenter);
        return;
        IntPtr hwnd = PlatUtils.DimmerHandle;
        Icon appIcon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location)!;
        _trayIconHelper = new TrayIconHelper();
        _trayIconHelper.CreateTrayIcon("Dimmer", appIcon.Handle,
            onLeftClick: () =>
            {
                Debug.WriteLine("Left click on tray icon - restoring window");
                //NativeMethods.ShowWindow(hwnd, NativeMethods.SW_RESTORE);
                //NativeMethods.SetForegroundWindow(hwnd);
            },
            onRightClickOpenHome: () =>
            {
                Debug.WriteLine("Tray: Open Home clicked.");
            },
            onRightClickOpenLyrics: () =>
            {
                Debug.WriteLine("Tray: Open Lyrics clicked.");
            });

        PlatUtils.ShowWindow(hwnd, 0); // Hide window (SW_HIDE)

        this.OnDeactivated();
#endif
    }
    public void SetTitle(SongModelView song)
    {
        this.Title = $"{song.AlbumName} by {song.ArtistName}";
    }

    private void SettingsBtn_Clicked(object sender, EventArgs e)
    {

        //await Shell.Current.GoToAsync(nameof(SettingsPage));
    }

    private void SettingsBtnn_Clicked(object sender, EventArgs e)
    {

#if WINDOWS
        MyViewModel.OpenAllSongsPageWinUICommand.Execute(null);
#endif
    }

    private void SwitchTheme_Clicked(object sender, EventArgs e)
    {
        MyViewModel.ToggleAppTheme();
    }

    private void TouchBehavior_InteractionStatusChanged(object sender, CommunityToolkit.Maui.Core.TouchInteractionStatusChangedEventArgs e)
    {

    }

    private async void DimmerChat_Clicked(object sender, EventArgs e)
    {
        //await Shell.Current.GoToAsync(nameof(ChatView), true);
    }

    private async void ShareBtn_Clicked(object sender, EventArgs e)
    {
        //await Shell.Current.GoToAsync(nameof(SessionTransferView),true);
    }

    private async void SocialBtn_Clicked(object sender, EventArgs e)
    {
        //await Shell.Current.GoToAsync(nameof(SocialView),true);
    }
}