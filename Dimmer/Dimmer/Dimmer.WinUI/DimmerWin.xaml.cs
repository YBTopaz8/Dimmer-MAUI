using Dimmer.Interfaces.Services.Interfaces;

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
    protected async override void OnDestroying()
    {
        if (!AppSettingsService.ShowCloseConfirmationPopUp.GetCloseConfirmation())
        {
            SubscriptionManager subMgr = IPlatformApplication.Current!.Services.GetService<SubscriptionManager>()!;
            CloseAllWindows();
            var dimmerAudio = IPlatformApplication.Current!.Services.GetService<IDimmerAudioService>();
            if (dimmerAudio is not null)
            {
                await dimmerAudio.StopAsync();
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
        // make a copy since closing mutates the collection
        foreach (var window in Application.Current!.Windows.ToList())
        {
            // this will completely close the window
            Application.Current.CloseWindow(window);
        }
    }
    protected override void OnActivated()
    {
        base.OnActivated();
        Debug.WriteLine("OnActivated");
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
        StickTopImgBtn.IsVisible = MyViewModel.IsStickToTop;
        UnStickTopImgBtn.IsVisible = !MyViewModel.IsStickToTop;

    }
    private void StickTopImgBtn_Clicked(object sender, EventArgs e)
    {
        StickTopImgBtn.IsVisible = false;
        UnStickTopImgBtn.IsVisible = true;

    }

    private void UnStickTopImgBtn_Clicked(object sender, EventArgs e)
    {
        StickTopImgBtn.IsVisible = true;
        UnStickTopImgBtn.IsVisible = false;
    }

    private TrayIconHelper? _trayIconHelper;
    private void Minimize_Clicked(object sender, EventArgs e)
    {

        IntPtr hwnd = PlatUtils.DimmerHandle;
        Icon appIcon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location)!;
        _trayIconHelper = new TrayIconHelper();
        _trayIconHelper.CreateTrayIcon("Dimmer", appIcon.Handle,
            onLeftClick: () =>
            {
                Debug.WriteLine("Left click on tray icon - restoring window");
                NativeMethods.ShowWindow(hwnd, NativeMethods.SW_RESTORE);
                NativeMethods.SetForegroundWindow(hwnd);
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

}