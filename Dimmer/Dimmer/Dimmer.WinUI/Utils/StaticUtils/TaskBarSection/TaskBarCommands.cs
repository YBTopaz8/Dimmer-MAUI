using System.Windows.Forms;
using static Vanara.PInvoke.Shell32;

namespace Dimmer.WinUI.Utils.StaticUtils.TaskBarSection;
public static class TaskbarCommands
{
    // Custom message ID for tray icon notifications
    public const User32.WindowMessage WM_APP_TRAYMSG = User32.WindowMessage.WM_APP + 1;

    // IDs for thumbnail toolbar buttons
    public const uint ID_PREVIOUS = 1001;
    public const uint ID_PLAY_PAUSE = 1002; // Single ID for toggle button
    public const uint ID_NEXT = 1003;

    // ID for the tray icon itself
    public const uint TRAY_ICON_ID = 1;
    
}
public static class WindowsIntegration
{
    // P/Invoke for CoCreateInstance.
    [DllImport("ole32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
#pragma warning disable SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
    private static extern int CoCreateInstance(ref Guid clsid, IntPtr pUnkOuter, uint dwClsContext, ref Guid riid, out IntPtr ppv);
#pragma warning restore SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time

    private const uint CLSCTX_INPROC_SERVER = 1;

    #region Tray Icon Integration

    // Constants
    private const int WM_APP = 0x8000;
    private const uint WM_TRAY_CALLBACK = WM_APP + 100; // custom tray icon message
    private const uint WM_LBUTTONUP = 0x0202;
    private const uint WM_RBUTTONUP = 0x0205;

    private const int ID_TRAY_OPEN_HOME = 5001;
    private const int ID_TRAY_OPEN_LYRICS = 5002;

    // Tray icon flags
    private const int NIF_MESSAGE = 0x00000001;
    private const int NIF_ICON = 0x00000002;
    private const int NIF_TIP = 0x00000004;
    private const uint NIM_ADD = 0x00000000;
    private const uint NIM_DELETE = 0x00000002;

    // Struct used by Shell_NotifyIcon.
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct Notifyicondata
    {
        public uint cbSize;
        public IntPtr hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
    }

    // Static fields to hold the tray icon data and callbacks.
    private static Notifyicondata _nid;
    private static IntPtr _origWndProc = IntPtr.Zero;
    private static WndProcDelegate? _newWndProc;
    private static Action? _trayLeftClickAction;
    private static Action? _trayRightClickHomeAction;
    private static Action? _trayRightClickLyricsAction;
    private static IntPtr _hookedHwnd = IntPtr.Zero;

    // Delegate for window procedure hook.
    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    // Sets up the tray icon. Pass your window handle, tooltip text, icon, and the callbacks.
    public static void SetupTrayIcon(
        IntPtr hwnd,
        string tooltip,
        Icon icon,
        Action onLeftClick,
        Action onRightClickOpenHome,
        Action onRightClickOpenLyrics)
    {
        _hookedHwnd = hwnd;
        _trayLeftClickAction = onLeftClick;
        _trayRightClickHomeAction = onRightClickOpenHome;
        _trayRightClickLyricsAction = onRightClickOpenLyrics;

        // Prepare tray icon data.
        _nid = new Notifyicondata
        {
            cbSize = (uint)Marshal.SizeOf(typeof(Notifyicondata)),
            hWnd = hwnd,
            uID = 1,
            uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP,
            uCallbackMessage = WM_TRAY_CALLBACK,
            hIcon = icon.Handle,
            szTip = tooltip
        };

        // Add the tray icon.
        bool result = Shell_NotifyIcon(NIM_ADD, ref _nid);
        Debug.WriteLine("Tray icon created: " + result);

        // Hook window procedure (if not already hooked).
        _newWndProc = new WndProcDelegate(TrayWndProc);
        _origWndProc = SetWindowLongPtr(hwnd, GWL_WNDPROC, _newWndProc);
    }

    // Call to remove the tray icon (usually on exit).
    public static void RemoveTrayIcon()
    {
        Shell_NotifyIcon(NIM_DELETE, ref _nid);
        // Unhook window proc if needed.
        if (_hookedHwnd != IntPtr.Zero && _origWndProc != IntPtr.Zero)
        {
            SetWindowLongPtr(_hookedHwnd, GWL_WNDPROC, _origWndProc);
        }
    }

    // Our custom window procedure to capture tray icon messages.
    private static IntPtr TrayWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_TRAY_CALLBACK)
        {
            uint mouseMsg = (uint)lParam & 0xFFFF;
            if (mouseMsg == WM_LBUTTONUP)
            {
                _trayLeftClickAction?.Invoke();
            }
            else if (mouseMsg == WM_RBUTTONUP)
            {
                ShowTrayContextMenu(hWnd);
            }
        }
        else if (msg == WM_COMMAND)
        {
            // Command from our popup menu.
            int cmdId = wParam.ToInt32() & 0xFFFF;
            if (cmdId == ID_TRAY_OPEN_HOME)
            {
                _trayRightClickHomeAction?.Invoke();
            }
            else if (cmdId == ID_TRAY_OPEN_LYRICS)
            {
                _trayRightClickLyricsAction?.Invoke();
            }
        }
        return CallWindowProc(_origWndProc, hWnd, msg, wParam, lParam);
    }
    private const uint WM_COMMAND = 0x0111;

    // Displays the context menu for the tray icon.
    private static void ShowTrayContextMenu(IntPtr hwnd)
    {
        IntPtr hMenu = CreatePopupMenu();
        if (hMenu != IntPtr.Zero)
        {
            // Insert "Open Home"
            InsertMenu(hMenu, 0, MF_BYPOSITION, (UIntPtr)ID_TRAY_OPEN_HOME, "Open Home");
            // Insert "Open Lyrics"
            InsertMenu(hMenu, 1, MF_BYPOSITION, (UIntPtr)ID_TRAY_OPEN_LYRICS, "Open Lyrics");

            // Get current cursor position.
            if (GetCursorPos(out Point pt))
            {
                // Make the menu the foreground window.
                SetForegroundWindow(hwnd);
                TrackPopupMenu(hMenu, TPM_LEFTALIGN | TPM_RIGHTBUTTON, pt.X, pt.Y, 0, hwnd, IntPtr.Zero);
            }
            DestroyMenu(hMenu);
        }
    }



    // Flags for progress state.
    public enum TBPFLAG
    {
        TBPF_NOPROGRESS = 0,
        TBPF_INDETERMINATE = 0x1,
        TBPF_NORMAL = 0x2,
        TBPF_ERROR = 0x4,
        TBPF_PAUSED = 0x8
    }

    // Enums and struct for thumbnail buttons.
    //[Flags]
    //private enum THUMBBUTTONMASK : uint
    //{
    //    THB_BITMAP = 0x1,
    //    THB_ICON = 0x2,
    //    THB_TOOLTIP = 0x4,
    //    THB_FLAGS = 0x8
    //}

    //[Flags]
    //private enum ETHUMBBUTTON : uint
    //{
    //    None = 0x0,
    //    THBF_DISABLED = 0x1,
    //    THBF_DISMISSONCLICK = 0x2,
    //    THBF_NOBACKGROUND = 0x4,
    //    THBF_HIDDEN = 0x8,
    //    THBF_NONINTERACTIVE = 0x10
    //}

    //[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    //private struct Thumbutton
    //{
    //    public THUMBBUTTONMASK dwMask;
    //    public uint iId;
    //    public uint iBitmap;
    //    public IntPtr hIcon;
    //    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
    //    public string szTip;
    //    public ETHUMBBUTTON dwFlags;
    //}

    // Command IDs for thumbnail buttons.
    private const uint ID_TB_PREV = 6001;
    private const uint ID_TB_PLAYPAUSE = 6002;
    private const uint ID_TB_NEXT = 6003;

    // Holds the ITaskbarList3 COM object.
    private static ITaskbarList3? _taskbarList3;

    // Sets up the thumbnail toolbar buttons for a given window.
    // The three button callbacks will be invoked via WM_COMMAND messages.
    // (Make sure your window procedure can catch WM_COMMAND; in our tray hook we already check for it.)
    public static void SetupTaskbarButtons(IntPtr hwnd, Action onPrev, Action onPlayPause, Action onNext)
    {
        try
        { 
        Guid clsid_TaskbarList = new Guid("56FDF344-FD6D-11d0-958A-006097C9A090");
            Guid iid_ITaskbarList3 = new Guid("ea1afb91-9e28-4b86-90e9-9e9f8a5eefaf");

        IntPtr pTaskbar;
        int hr = CoCreateInstance(ref clsid_TaskbarList, IntPtr.Zero, CLSCTX_INPROC_SERVER, ref iid_ITaskbarList3, out pTaskbar);
        if (hr != 0)
        {
            Debug.WriteLine($"CoCreateInstance failed with HR = {hr:X}");
            return;
        }

        // Get a typed object for ITaskbarList3.
        _taskbarList3 = (ITaskbarList3)Marshal.GetTypedObjectForIUnknown(pTaskbar, typeof(ITaskbarList3));
        _taskbarList3.HrInit();

        // Create three thumbnail toolbar buttons.
        THUMBBUTTON[] buttons = new THUMBBUTTON[3];

        // Get valid icon handles; replace with your own icons if available.
        IntPtr prevIcon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location)?.Handle ?? IntPtr.Zero;
        IntPtr playIcon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location)?.Handle ?? IntPtr.Zero;
        IntPtr nextIcon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location)?.Handle ?? IntPtr.Zero;

        buttons[0] = new THUMBBUTTON
        {
            dwMask = THUMBBUTTONMASK.THB_ICON | THUMBBUTTONMASK.THB_TOOLTIP | THUMBBUTTONMASK.THB_FLAGS,
            iId = ID_TB_PREV,
            hIcon = prevIcon,
            szTip = "Previous",
            dwFlags = THUMBBUTTONFLAGS.THBF_ENABLED
        };

        buttons[1] = new THUMBBUTTON
        {
            dwMask = THUMBBUTTONMASK.THB_ICON | THUMBBUTTONMASK.THB_TOOLTIP | THUMBBUTTONMASK.THB_FLAGS,
            iId = ID_TB_PLAYPAUSE,
            hIcon = playIcon,
            szTip = "Play/Pause",
            dwFlags = THUMBBUTTONFLAGS.THBF_ENABLED
        };

        buttons[2] = new THUMBBUTTON
        {
            dwMask = THUMBBUTTONMASK.THB_ICON | THUMBBUTTONMASK.THB_TOOLTIP | THUMBBUTTONMASK.THB_FLAGS,
            iId = ID_TB_NEXT,
            hIcon = nextIcon,
            szTip = "Next",
            dwFlags = THUMBBUTTONFLAGS.THBF_ENABLED
        };

        _taskbarList3.ThumbBarAddButtons(hwnd, (uint)buttons.Length, buttons);

        // Handle WM_COMMAND messages for thumbnail buttons via message filter.
        System.Windows.Forms.Application.AddMessageFilter(new TaskbarCommandMessageFilter(onPrev, onPlayPause, onNext));
    }

        catch (Exception ex)
        {
            // Handle the case where the interface is not available.
            Debug.WriteLine("Taskbar integration not supported: " + ex.Message);
            _taskbarList3 = null; // Or disable thumbnail button functionality.
        }
    }

    // Sets the taskbar progress bar state.
    public static void SetTaskbarProgress(IntPtr hwnd, ulong completed, ulong total, Vanara.PInvoke.Shell32.TBPFLAG state = Shell32.TBPFLAG.TBPF_NORMAL)
    {
        _taskbarList3?.SetProgressState(hwnd, state);
        _taskbarList3?.SetProgressValue(hwnd, completed, total);
    }

    // Helper COM class to create the TaskbarList object.
    [ComImport]
    [Guid("56FDF344-FD6D-11d0-958A-006097C9A090")]
    private class CTaskbarList { }

    #endregion

    #region P/Invoke Declarations

    private const int GWL_WNDPROC = -4;
    private const uint TPM_LEFTALIGN = 0x0000;
    private const uint TPM_RIGHTBUTTON = 0x0002;
    private const uint MF_BYPOSITION = 0x00000400;

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
#pragma warning disable SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
    private static extern bool Shell_NotifyIcon(uint dwMessage, ref Notifyicondata lpdata);
#pragma warning restore SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
#pragma warning disable SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, WndProcDelegate newProc);
#pragma warning restore SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
#pragma warning disable SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr newProc);
#pragma warning restore SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time

    [DllImport("user32.dll")]
#pragma warning disable SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
#pragma warning restore SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time

    [DllImport("user32.dll", SetLastError = true)]
#pragma warning disable SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
    private static extern IntPtr CreatePopupMenu();
#pragma warning restore SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time

    [DllImport("user32.dll", SetLastError = true)]
#pragma warning disable SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
    private static extern bool DestroyMenu(IntPtr hMenu);
#pragma warning restore SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
#pragma warning disable SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
    private static extern bool InsertMenu(IntPtr hMenu, uint uPosition, uint uFlags, UIntPtr uIDNewItem, string lpNewItem);
#pragma warning restore SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time

    [DllImport("user32.dll")]
#pragma warning disable SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
    private static extern bool GetCursorPos(out Point lpPoint);
#pragma warning restore SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time

    [DllImport("user32.dll")]
#pragma warning disable SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
    private static extern bool SetForegroundWindow(IntPtr hWnd);
#pragma warning restore SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
#pragma warning disable SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
    private static extern int TrackPopupMenu(IntPtr hMenu, uint uFlags, int x, int y, int nReserved, IntPtr hWnd, IntPtr prcRect);
#pragma warning restore SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }

    #endregion

    #region Taskbar Button Message Filter (for WM_COMMAND)

    // A message filter to catch WM_COMMAND messages for our thumbnail buttons.
    sealed class TaskbarCommandMessageFilter : IMessageFilter
    {
        private readonly Action _onPrev;
        private readonly Action _onPlayPause;
        private readonly Action _onNext;

        public TaskbarCommandMessageFilter(Action onPrev, Action onPlayPause, Action onNext)
        {
            _onPrev = onPrev;
            _onPlayPause = onPlayPause;
            _onNext = onNext;
        }

        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg == 0x0111) // WM_COMMAND
            {
                int cmdId = m.WParam.ToInt32() & 0xFFFF;
                if (cmdId == ID_TB_PREV)
                {
                    _onPrev?.Invoke();
                    return true;
                }
                else if (cmdId == ID_TB_PLAYPAUSE)
                {
                    _onPlayPause?.Invoke();
                    return true;
                }
                else if (cmdId == ID_TB_NEXT)
                {
                    _onNext?.Invoke();
                    return true;
                }
            }
            return false;
        }
    }

    #endregion
}
public static class NativeMethods
{
    public const int SW_RESTORE = 9;

    [DllImport("user32.dll")]
#pragma warning disable S4200 // Native methods should be wrapped
#pragma warning disable SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
#pragma warning disable CA1401 // P/Invokes should not be visible
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
#pragma warning restore CA1401 // P/Invokes should not be visible
#pragma warning restore SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
#pragma warning restore S4200 // Native methods should be wrapped

    [DllImport("user32.dll")]
#pragma warning disable S4200 // Native methods should be wrapped
#pragma warning disable SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
#pragma warning disable CA1401 // P/Invokes should not be visible
    public static extern bool SetForegroundWindow(IntPtr hWnd);
#pragma warning restore CA1401 // P/Invokes should not be visible
#pragma warning restore SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
#pragma warning restore S4200 // Native methods should be wrapped
}
    // Replace the ComImportAttribute with GeneratedComInterfaceAttribute for the ITaskbarList3 interface.
    // This change is required to generate COM marshalling code at compile time as per the diagnostic SYSLIB1096.
    [ComImport]
[Guid("ea1afb91-9e28-4b86-90e9-9e9f8a5eefaf")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface ITaskbarList3
    {
        // ITaskbarList
        void HrInit();
        void AddTab(IntPtr hwnd);
        void DeleteTab(IntPtr hwnd);
        void ActivateTab(IntPtr hwnd);
        void SetActiveAlt(IntPtr hwnd);

        // ITaskbarList3
        void MarkFullscreenWindow(IntPtr hwnd, [MarshalAs(UnmanagedType.Bool)] bool fFullscreen);
        void SetProgressValue(IntPtr hwnd, ulong ullCompleted, ulong ullTotal);
        void SetProgressState(IntPtr hwnd, TBPFLAG tbpFlags);
        void RegisterTab(IntPtr hwndTab, IntPtr hwndMDI);
        void UnregisterTab(IntPtr hwndTab);
        void SetTabOrder(IntPtr hwndTab, IntPtr hwndInsertBefore);
        void SetTabActive(IntPtr hwndTab, IntPtr hwndMDI, uint dwReserved);
        void ThumbBarAddButtons(IntPtr hwnd, uint cButtons, [MarshalAs(UnmanagedType.LPArray)] THUMBBUTTON[] pButton);
    }