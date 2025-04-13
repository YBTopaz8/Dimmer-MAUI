using Point = System.Drawing.Point;

namespace Dimmer.WinUI.Utils.Helpers
{
    public class TrayIconHelper
    {
        private const uint NIM_ADD = 0x00000000;
        private const uint NIM_DELETE = 0x00000002;
        private const uint NIF_MESSAGE = 0x00000001;
        private const uint NIF_ICON = 0x00000002;
        private const uint NIF_TIP = 0x00000004;
        public const uint WM_TRAYICON = 0x8000 + 1; // WM_APP + 1 (32769)
        private const uint WM_LBUTTONUP = 0x0202;
        private const uint WM_RBUTTONUP = 0x0205;
        private const uint WM_COMMAND = 0x0111;
        private const uint MF_BYPOSITION = 0x00000400;
        private const uint TPM_LEFTALIGN = 0x0000;
        private const uint TPM_RIGHTBUTTON = 0x0002;

        private Notifyicondata _data;
        private bool _iconAdded;

        // Callbacks
        private Action? _onLeftClick;
        private Action? _onRightClickOpenHome;
        private Action? _onRightClickOpenLyrics;

        // WndProc hook fields for the tray window
        private WndProcDelegate? _trayProcDelegate;
        private IntPtr _oldTrayWndProc = IntPtr.Zero;

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
#pragma warning disable SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
        private static extern bool Shell_NotifyIcon(uint dwMessage, ref Notifyicondata lpdata);
#pragma warning restore SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
#pragma warning disable SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, WndProcDelegate newProc);
#pragma warning restore SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
        // For restoring by setting an IntPtr
        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
#pragma warning disable SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
        private static extern IntPtr SetWindowLongPtrIntPtr(IntPtr hWnd, int nIndex, IntPtr newProc);
#pragma warning restore SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time

        [DllImport("user32.dll", SetLastError = true)]
#pragma warning disable SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
        private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
#pragma warning restore SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time

        // Used for context menu creation.
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

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct Notifyicondata
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

        // Constructor: Initialize Notifyicondata.
        public TrayIconHelper()
        {
            _data = new Notifyicondata
            {
                cbSize = (uint)Marshal.SizeOf(typeof(Notifyicondata))
            };
        }

        /// <summary>
        /// Creates a tray icon and installs a window procedure hook to receive its messages.
        /// </summary>
        public void CreateTrayIcon(string tooltip, IntPtr iconHandle,
            Action onLeftClick, Action onRightClickOpenHome, Action onRightClickOpenLyrics)
        {
            // Save callbacks.
            _onLeftClick = onLeftClick;
            _onRightClickOpenHome = onRightClickOpenHome;
            _onRightClickOpenLyrics = onRightClickOpenLyrics;

            // Get the window handle from your platform utilities.
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

            // Hook the window procedure so that we get tray messages.
            _trayProcDelegate = new WndProcDelegate(TrayWndProc);
            _oldTrayWndProc = SetWindowLongPtr(hwnd, -4, _trayProcDelegate); // GWL_WNDPROC = -4
        }

        /// <summary>
        /// Removes the tray icon and unhooks the window procedure.
        /// </summary>
        public void RemoveTrayIcon()
        {
            if (_iconAdded)
            {
                Shell_NotifyIcon(NIM_DELETE, ref _data);
                _iconAdded = false;
            }
            // Restore original WndProc if needed.
            if (_oldTrayWndProc != IntPtr.Zero)
            {
                SetWindowLongPtrIntPtr(PlatUtils.DimmerHandle, -4, _oldTrayWndProc);
            }
        }

        /// <summary>
        /// Custom window procedure to process tray icon messages.
        /// </summary>
        private IntPtr TrayWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WM_TRAYICON)
            {
                // lParam carries the mouse event.
                uint mouseMsg = (uint)lParam & 0xFFFF;
                if (mouseMsg == WM_LBUTTONUP)
                {
                    // Left click: restore window.
                    _onLeftClick?.Invoke();
                }
                else if (mouseMsg == WM_RBUTTONUP)
                {
                    // Right click: show context menu.
                    ShowContextMenu(hWnd);
                }
                return IntPtr.Zero; // handled
            }
            // Pass on unhandled messages.
            return CallWindowProc(_oldTrayWndProc, hWnd, msg, wParam, lParam);
        }

        /// <summary>
        /// Displays a context menu with options "Open Home" and "Open Lyrics" when right-clicked.
        /// </summary>
        private void ShowContextMenu(IntPtr hWnd)
        {
            IntPtr hMenu = CreatePopupMenu();
            if (hMenu != IntPtr.Zero)
            {
                // Insert "Open Home" as first item.
                InsertMenu(hMenu, 0, MF_BYPOSITION, (UIntPtr)5001, "Open Home");
                // Insert "Open Lyrics" as second item.
                InsertMenu(hMenu, 1, MF_BYPOSITION, (UIntPtr)5002, "Open Lyrics");

                if (GetCursorPos(out Point pt))
                {
                    SetForegroundWindow(hWnd);
                    // Display the context menu.
                    TrackPopupMenu(hMenu, TPM_LEFTALIGN | TPM_RIGHTBUTTON, pt.X, pt.Y, 0, hWnd, IntPtr.Zero);
                }
                DestroyMenu(hMenu);
            }
        }

    }
}
