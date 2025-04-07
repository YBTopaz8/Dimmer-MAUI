using Dimmer.WinUI.Utils.StaticUtils;
using System.Runtime.InteropServices;

namespace Dimmer.WinUI.Utils.Helpers;

public class TrayIconHelper
{
    private const uint NIM_ADD = 0x00000000;
    private const uint NIM_DELETE = 0x00000002;
    private const uint NIF_MESSAGE = 0x00000001;
    private const uint NIF_ICON = 0x00000002;
    private const uint NIF_TIP = 0x00000004;
    public const uint WM_TRAYICON = 0x8000 + 1; // WM_APP + 1 (32769)

    private NOTIFYICONDATA _data;
    private bool _iconAdded = false;

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern bool Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATA lpdata);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, WndProcDelegate newProc);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct NOTIFYICONDATA
    {
        public uint cbSize;
        public IntPtr hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
        public uint dwState;
        public uint dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szInfo;
        public uint uTimeoutOrVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string szInfoTitle;
        public uint dwInfoFlags;
        public Guid guidItem;
        public IntPtr hBalloonIcon;
    }

    public TrayIconHelper()
    {
        _data = new NOTIFYICONDATA
        {
            cbSize = (uint)Marshal.SizeOf(typeof(NOTIFYICONDATA))
        };
    }

    public void CreateTrayIcon(string tooltip, IntPtr iconHandle)
    {
        IntPtr hwnd = PlatUtils.DimmerHandle;

        _data.hWnd = hwnd;
        _data.uID = 1;
        _data.uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP;
        _data.uCallbackMessage = WM_TRAYICON;
        _data.hIcon = iconHandle;
        _data.szTip = tooltip;

        if (Shell_NotifyIcon(NIM_ADD, ref _data))
        {
            _iconAdded = true;
        }
    }

    public void RemoveTrayIcon()
    {
        if (_iconAdded)
        {
            Shell_NotifyIcon(NIM_DELETE, ref _data);
            _iconAdded = false;
        }
    }
}
