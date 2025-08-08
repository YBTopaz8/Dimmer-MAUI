using Dimmer.Interfaces.Services.Interfaces;
using Dimmer.WinUI.Utils.WinMgt;
using Dimmer.WinUI.Views.WinUIPages;

namespace Dimmer.WinUI;

public partial class DimmerWin : Window
{
    public BaseViewModelWin MyViewModel { get; set; }
    public DimmerWin(BaseViewModelWin vm)
    {
        InitializeComponent();
        Page = IPlatformApplication.Current!.Services.GetService<IAppUtil>()?.GetShell();
        MyViewModel= vm;
        BindingContext=vm;
        

    }

    private void AppShell_Loaded(object? sender, EventArgs e)
    {
        
    }


    protected async override void OnDestroying()
    {
        
        MyViewModel.OnAppClosing();
        if (!AppSettingsService.ShowCloseConfirmationPopUp.GetCloseConfirmation())
        {
            SubscriptionManager subMgr = IPlatformApplication.Current!.Services.GetService<SubscriptionManager>()!;
            CloseAllWindows();
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

        CloseAllWindows();

    }
    private void CloseAllWindows()
    {
        if (Application.Current!.Windows.Count <2)
        {
            return;
        }

        // make a copy since closing mutates the collection
        foreach (var window in Application.Current!.Windows.ToList())
        {
            //REWORK THIS
            // this will completely close the window
            Application.Current.CloseWindow(window);
        }
    }
    protected override void OnActivated()
    {
        base.OnActivated();
        var nativeElement = this.Page?.Handler?.PlatformView as Microsoft.UI.Xaml.UIElement;
        if (nativeElement != null)
        {
            
            nativeElement.PointerPressed += OnGlobalPointerPressed;
        }
    }
    protected override void OnBackgrounding(IPersistedState state)
    {
        base.OnBackgrounding(state);
    }

    protected override void OnDeactivated()
    {
        base.OnDeactivated();
        var nativeElement = this.Page?.Handler?.PlatformView as Microsoft.UI.Xaml.UIElement;
        if (nativeElement != null)
        {
            nativeElement.PointerPressed -= OnGlobalPointerPressed;
        }
        //PlatUtils.ToggleFullScreenMode(false, PlatUtils.AppWinPresenter);
        //PlatUtils.ShowWindow(PlatUtils.DimmerHandle, 0); // Hide window (SW_HIDE)
    }
    private async void OnGlobalPointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        var properties = e.GetCurrentPoint(sender as Microsoft.UI.Xaml.UIElement).Properties;


        if (properties.IsXButton1Pressed)
        {
            // Handle Back Navigation
            await Shell.Current.GoToAsync("..");
        }
        else if (properties.IsXButton2Pressed)
        {
            
        }
    }
    protected override void OnCreated()
    {
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
    }
    protected override void OnStopped()
    {
        base.OnStopped();
    }
    private void StickTopImgBtn_Clicked(object sender, EventArgs e)
    {
        StickTopImgBtn.IsVisible = false;
        UnStickTopImgBtn.IsVisible = true;
        PlatUtils.ToggleWindowAlwaysOnTop(true, PlatUtils.AppWinPresenter);
    }

    private void UnStickTopImgBtn_Clicked(object sender, EventArgs e)
    {
        StickTopImgBtn.IsVisible = true;
        UnStickTopImgBtn.IsVisible = false;
        PlatUtils.ToggleWindowAlwaysOnTop(false, PlatUtils.AppWinPresenter);
    }

    private TrayIconHelper? _trayIconHelper;
    private void Minimize_Clicked(object sender, EventArgs e)
    {
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

    }
    public void SetTitle(SongModelView song)
    {
        this.Title = $"{song.AlbumName} by {song.ArtistName}";
    }

    private void SettingsBtn_Clicked(object sender, EventArgs e)
    {
     
        var winMgr = IPlatformApplication.Current!.Services.GetService<IWindowManagerService>()!;

        winMgr.GetOrCreateUniqueWindow(() => new SettingWin(MyViewModel));
        //await Shell.Current.GoToAsync(nameof(SettingsPage));
    }

    private void SettingsBtnn_Clicked(object sender, EventArgs e)
    {
        var winMgr = IPlatformApplication.Current!.Services.GetService<IWinUIWindowMgrService>()!;

        var win = winMgr.GetOrCreateUniqueWindow(() => new AllSongsWindow(MyViewModel));
        Debug.WriteLine(win.Visible);
        Debug.WriteLine(win.AppWindow.IsShownInSwitchers);//VERY IMPORTANT FOR WINUI 3 TO SHOW IN TASKBAR
    }
}