using Dimmer.Utilities;
using Dimmer.WinUI.Utils.Helpers;
using Dimmer.WinUI.Utils.StaticUtils;
using Dimmer.WinUI.Utils.StaticUtils.TaskBarSection;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using Vanara.PInvoke;

namespace Dimmer.WinUI;

public partial class DimmerWin : Window
{
    public BaseViewModel MyViewModel { get; set; }
	public DimmerWin(BaseViewModel vm)
	{
		InitializeComponent();
		Page = IPlatformApplication.Current!.Services.GetService<IAppUtil>()?.GetShell();
        MyViewModel= vm;
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        Debug.WriteLine("OnActivated");
    }
    protected override void OnCreated()
    {
        base.OnCreated();
        this.MinimumHeight = 750;
        this.MinimumWidth = 900;
        this.Height = 850;
        this.Width = 1100;
#if DEBUG
        DimmerTitleBar.Subtitle = "v1.8-debug";
        DimmerTitleBar.BackgroundColor = Microsoft.Maui.Graphics.Colors.DarkSeaGreen;
#elif RELEASE
        DimmerTitleBar.Subtitle = "v1.8-Release";
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
        MyViewModel.ToggleStickToTop();
        //MyViewModel.ToggleStickToTopCommand.Execute(null);
        //StickTopImgBtn.IsVisible = MyViewModel.IsStickToTop;
        //UnStickTopImgBtn.IsVisible = !MyViewModel.IsStickToTop;
    }

    private void UnStickTopImgBtn_Clicked(object sender, EventArgs e)
    {
        MyViewModel.ToggleStickToTop();
        //MyViewModel.ToggleStickToTopCommand.Execute(null);
        //StickTopImgBtn.IsVisible = MyViewModel.IsStickToTop;
        //UnStickTopImgBtn.IsVisible = !MyViewModel.IsStickToTop;
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