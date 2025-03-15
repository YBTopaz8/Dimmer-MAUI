using System.Runtime.InteropServices;

namespace Dimmer_MAUI.Platforms.Windows;
public partial class TaskbarThumbnailHelper
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    private const int GWL_WNDPROC = -4;
    private const int WM_COMMAND = 0x0111;
    private const int WM_NOTIFY = 0x004E;
    private const int WM_DESTROY = 0x0002;

    // Replace with actual values from Spy++ or your inspection tool.
    private const string ThumbnailClassName = "Shell_ThumbPreview";
    private const string ThumbnailWindowTitle = null;

    private bool _thumbnailHandlingInitialized = false;
    private IntPtr _thumbnailPreviewWindowHandle = IntPtr.Zero;
    private IntPtr _oldWndProc = IntPtr.Zero;
    // Keep the delegate alive to avoid garbage collection.
    private WndProcDelegate _newWndProcDelegate;

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    public void InitializeThumbnailHandling()
    {
        if (_thumbnailHandlingInitialized)
        {
            Debug.WriteLine("Thumbnail handling already initialized.");
            return;
        }

        _thumbnailPreviewWindowHandle = FindThumbnailPreviewWindow();

        if (_thumbnailPreviewWindowHandle != IntPtr.Zero)
        {
            _newWndProcDelegate = new WndProcDelegate(WndProc);
            IntPtr newProcPtr = Marshal.GetFunctionPointerForDelegate(_newWndProcDelegate);
            _oldWndProc = SetWindowLongPtr(_thumbnailPreviewWindowHandle, GWL_WNDPROC, newProcPtr);

            if (_oldWndProc == IntPtr.Zero)
            {
                Debug.WriteLine("Failed to hook window procedure. Error: " + Marshal.GetLastWin32Error());
            }
            else
            {
                _thumbnailHandlingInitialized = true;
                Debug.WriteLine("Thumbnail handling initialized.");
            }
        }
        else
        {
            Debug.WriteLine("Could not find thumbnail preview window.");
        }
    }

    private static IntPtr FindThumbnailPreviewWindow()
    {
        IntPtr hwnd = FindWindow(ThumbnailClassName, ThumbnailWindowTitle);
        if (hwnd == IntPtr.Zero)
        {
            hwnd = FindWindowEx(IntPtr.Zero, IntPtr.Zero, ThumbnailClassName, ThumbnailWindowTitle);
        }
        return hwnd;
    }

    private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_NOTIFY)
        {
            NMHDR nmhdr = Marshal.PtrToStructure<NMHDR>(lParam);
            if (nmhdr.code == TTN_COMMAND)
            {
                // Retrieve COMMANDINFO structure.
                COMMANDINFO commandInfo = Marshal.PtrToStructure<COMMANDINFO>(lParam);
                uint commandId = commandInfo.commandId;
                switch (commandId)
                {
                    case 100:
                        Debug.WriteLine("Play button clicked (Thumbnail).");
                        break;
                    case 101:
                        Debug.WriteLine("Pause button clicked (Thumbnail).");
                        break;
                    case 102:
                        Debug.WriteLine("Resume button clicked (Thumbnail).");
                        break;
                    case 103:
                        Debug.WriteLine("Previous button clicked (Thumbnail).");
                        break;
                    case 104:
                        Debug.WriteLine("Next button clicked (Thumbnail).");
                        break;
                    default:
                        Debug.WriteLine("Unknown command: " + commandId);
                        break;
                }
            }
        }
        else if (msg == WM_DESTROY)
        {
            UnhookThumbnailHandling();
        }
        // Call the original window procedure.
        return CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
    }

    public void UnhookThumbnailHandling()
    {
        if (_thumbnailPreviewWindowHandle != IntPtr.Zero && _oldWndProc != IntPtr.Zero)
        {
            try
            {
                SetWindowLongPtr(_thumbnailPreviewWindowHandle, GWL_WNDPROC, _oldWndProc);
                _thumbnailPreviewWindowHandle = IntPtr.Zero;
                _oldWndProc = IntPtr.Zero;
                _thumbnailHandlingInitialized = false;
                Debug.WriteLine("Unhooked thumbnail handling.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error unhooking thumbnail handling: " + ex.Message);
            }
        }
    }

    // Structures for WM_NOTIFY handling.
    [StructLayout(LayoutKind.Sequential)]
    public struct NMHDR
    {
        public IntPtr hwndFrom;
        public UIntPtr idFrom;
        public uint code;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct COMMANDINFO
    {
        public uint cbSize;
        public IntPtr hwnd;
        public uint commandId;
        public IntPtr extraInfo;
    }

    public const int TTN_COMMAND = 0x0100;
}