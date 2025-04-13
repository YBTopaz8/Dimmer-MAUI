using Dimmer.Utilities;
using Dimmer.WinUI.Utils.Helpers;
using Dimmer.WinUI.Utils.StaticUtils;
using Dimmer.WinUI.Utils.StaticUtils.TaskBarSection;
using Dimmer.WinUI.ViewModel;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using Vanara.PInvoke;
using static Vanara.PInvoke.Shell32;
using Vanara.PInvoke;
using static Vanara.PInvoke.User32;
using static Vanara.PInvoke.Kernel32;
using System.Windows.Forms;

namespace Dimmer.WinUI;

public partial class DimmerWin : Window
{
    public BaseViewModelWin MyViewModel { get; set; }
	public DimmerWin(BaseViewModelWin vm)
	{
		InitializeComponent();
		Page = IPlatformApplication.Current!.Services.GetService<IAppUtil>()?.GetShell();
        MyViewModel= vm;
        
    }
    protected async override void OnDestroying()
    {
        if (!AppSettingsService.ShowCloseConfirmationPopUp.GetCloseConfirmation())
        {
            return;
        }

        bool result = await Microsoft.Maui.Controls.Shell.Current.DisplayAlert(
            "Confirm Action",
            "You sure want to close app?",
            "Yes",
            "Cancel");
        if (!result)
        {
            return;            
        }
        base.OnDestroying();
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
    private bool _isRestoring = false;
    private void StickTopImgBtn_Clicked(object sender, EventArgs e)
    {
        PlatUtils.ToggleWindowAlwaysOnTop(MyViewModel.ToggleStickToTop(), PlatUtils.AppWinPresenter);

    }

    private void UnStickTopImgBtn_Clicked(object sender, EventArgs e)
    {
        PlatUtils.ToggleWindowAlwaysOnTop(MyViewModel.ToggleStickToTop(), PlatUtils.AppWinPresenter);

    }

    private TrayIconHelper? _trayIconHelper;
    private void Minimize_Clicked(object sender, EventArgs e)
    {

        IntPtr hwnd = PlatUtils.DimmerHandle;
        Icon appIcon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
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

}