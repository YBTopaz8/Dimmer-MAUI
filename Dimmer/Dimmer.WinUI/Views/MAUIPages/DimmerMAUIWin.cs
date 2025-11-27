using System.Reflection;

using Dimmer.WinUI.Utils.Helpers;

namespace Dimmer.WinUI.Views.MAUIPages;

public partial class DimmerMAUIWin : Microsoft.Maui.Controls.Window
{

    public BaseViewModelWin MyViewModel { get; set; }
    public IAppUtil AppUtil { get; }

    public DimmerMAUIWin(BaseViewModelWin vm, IAppUtil appUtil)
    {
        
        vm.MainMAUIWindow = this;
        Page = appUtil.GetShell();
        MyViewModel = vm;
        AppUtil = appUtil;
        BindingContext = vm;
    }
    public Microsoft.Maui.Controls.Page CurrentPage => this.Page as Microsoft.Maui.Controls.Page ?? throw new InvalidOperationException("Current Page is not a valid Page.");
    

    protected async override void OnDestroying()
    {
        MyViewModel.windowManager.CloseWindow(this);
        MyViewModel.OnAppClosing();
        if (!AppSettingsService.ShowCloseConfirmationPopUp.GetCloseConfirmation())
        {
            SubscriptionManager subMgr = IPlatformApplication.Current!.Services.GetService<SubscriptionManager>()!;
            
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


    }
    private void CloseAllWindows()
    {
        if (Microsoft.Maui.Controls.Application.Current!.Windows.Count < 2)
        {
            return;
        }

        


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
                    MyViewModel.IsLastFMNeedsToConfirm = false;
                    await Shell.Current.DisplayAlert("Action Cancelled", "Last FM Authorization Cancelled", "OK");

                }
            }
            var nativeElement = this.Page?.Handler?.PlatformView as Microsoft.UI.Xaml.UIElement;
            if (nativeElement != null)
            {
                nativeElement.PointerPressed += OnGlobalPointerPressed;
            }
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
        MyViewModel.windowManager.TrackWindow(this);
        base.OnCreated();
        var nativeWindow = PlatUtils.GetNativeWindowFromMAUIWindow(this);
        PlatUtils.MoveAndResizeCenter(nativeWindow, new Windows.Graphics.SizeInt32(600, 800));
        MinimumHeight = 750;
        MaximumHeight = 800;
        MaximumWidth = 500;
#if DEBUG
        //DimmerTitleBar.BackgroundColor = Microsoft.Maui.Graphics.Colors.DarkRed;
#elif RELEASE
#endif

        MyViewModel.DimmerMultiWindowCoordinator.SetHomeWindow(PlatUtils.GetNativeWindowFromMAUIWindow(this));
        
        if (MyViewModel is null)
        {
            return;
        }


    }
    protected override void OnStopped()
    {
        base.OnStopped();
    }
    private void StickTopImgBtn_Clicked(object sender, EventArgs e)
    {
        //StickTopImgBtn.IsVisible = false;
        //UnStickTopImgBtn.IsVisible = true;
        PlatUtils.ToggleWindowAlwaysOnTop(true, PlatUtils.AppWinPresenter);
    }

    private void UnStickTopImgBtn_Clicked(object sender, EventArgs e)
    {
        //StickTopImgBtn.IsVisible = true;
        //UnStickTopImgBtn.IsVisible = false;
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

        //await Shell.Current.GoToAsync(nameof(SettingsPage));
    }

    private void SettingsBtnn_Clicked(object sender, EventArgs e)
    {
        MyViewModel.OpenAllSongsPageWinUICommand.Execute(null);

    }

    

    private void SwitchTheme_Clicked(object sender, EventArgs e)
    {
        MyViewModel.ToggleAppTheme();
    }


    private async void DimmerChat_Clicked(object sender, EventArgs e)
    {
        //await Shell.Current.GoToAsync(nameof(ChatView), true);
    }

    private async void ShareBtn_Clicked(object sender, EventArgs e)
    {
        //await Shell.Current.GoToAsync(nameof(SessionTransferView), true);
    }

}
