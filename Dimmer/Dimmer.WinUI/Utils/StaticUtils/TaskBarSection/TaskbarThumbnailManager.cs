using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Vanara.Windows.Shell;

using static MS.WindowsAPICodePack.Internal.CoreNativeMethods;
using static Vanara.PInvoke.ComCtl32;
using static Vanara.PInvoke.Shell32;
using static Vanara.PInvoke.User32;

namespace Dimmer.WinUI.Utils.StaticUtils.TaskBarSection;
public class TaskbarThumbnailManager : IDisposable
{
    private const uint WM_COMMAND = 0x0111;

    // Button IDs (must be unique)
    public const ushort ID_PLAYPAUSE = 1001;
    public const ushort ID_PREV = 1002;
    public const ushort ID_NEXT = 1003;
    public const ushort ID_SHUFFLE = 1004;

    private HWND _hwnd;
    private TaskbarList? _taskbarList;
    private SafeHIMAGELIST? _hImageList;

    // Icon handles - store them to update buttons
    private SafeHICON? _hPlayIcon;
    private SafeHICON? _hPauseIcon;
    private SafeHICON? _hPrevIcon;
    private SafeHICON? _hNextIcon;
    private SafeHICON? _hShuffleOnIcon;
    private SafeHICON? _hShuffleOffIcon;

    private bool _isPlaying = false;
    private bool _isShuffleOn = false;

    // Callbacks for button clicks
    public Action? OnPlayPauseClicked { get; set; }
    public Action? OnPrevClicked { get; set; }
    public Action? OnNextClicked { get; set; }
    public Action? OnShuffleClicked { get; set; }

    // For WndProc Hooking
    private Wndproc? _newWndProc;
    private IntPtr _oldWndProc = IntPtr.Zero;
    private const int GWL_WNDPROC = -4;

    public TaskbarThumbnailManager()
    {
        // COM should be initialized for the thread.
        // WinUI 3 apps usually run in an STA thread, so CoInitializeEx might not be strictly necessary
        // if called from the main UI thread, but it's good practice if unsure.
        // HRESULT hr = Ole32.CoInitializeEx(IntPtr.Zero, Ole32.COINIT.COINIT_APARTMENTTHREADED);
        // if (hr.Failed) { /* Log or handle error */ }
    }

    public void Initialize(HWND windowHandle)
    {
        _hwnd = windowHandle;
        if (_hwnd.IsNull)
        {
            Debug.WriteLine("TaskbarThumbnailManager: Window handle is null.");
            return;
        }

        try
        {
            _taskbarList = new TaskbarList(); // This wraps ITaskbarList3/4

            LoadIcons();
            CreateImageList();
            AddThumbButtons();
            HookWndProc();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"TaskbarThumbnailManager Initialization failed: {ex.Message}");
            // Clean up partial initialization if necessary
            Dispose();
        }
    }

    private void LoadIcons()
    {
        // --- IMPORTANT ---
        // Replace these paths with your actual icon file paths or resource loading logic.
        // Icons should ideally be 16x16 for standard DPI.
        // The system will scale them, but providing the correct base size is best.
        string basePath = Path.Combine(AppContext.BaseDirectory, "Assets", "TaskbarIcons");

        _hPlayIcon = LoadIconFromFile(Path.Combine(basePath, "play.ico"));
        _hPauseIcon = LoadIconFromFile(Path.Combine(basePath, "pause.ico"));
        _hPrevIcon = LoadIconFromFile(Path.Combine(basePath, "previous.ico"));
        _hNextIcon = LoadIconFromFile(Path.Combine(basePath, "next.ico"));
        _hShuffleOnIcon = LoadIconFromFile(Path.Combine(basePath, "shuffle_on.ico"));
        _hShuffleOffIcon = LoadIconFromFile(Path.Combine(basePath, "shuffle_off.ico"));

        // Fallback if icons are not found (optional, or throw exception)
        if (_hPlayIcon == null || _hPauseIcon == null || _hPrevIcon == null || _hNextIcon == null || _hShuffleOnIcon == null || _hShuffleOffIcon == null)
        {
            Debug.WriteLine("TaskbarThumbnailManager: One or more icons could not be loaded. Ensure icon files exist at the specified paths.");
            // You might want to throw an exception here or use default system icons if suitable (more complex)
        }
    }

    private SafeHICON LoadIconFromFile(string iconPath)
    {
        if (!File.Exists(iconPath))
        {
            Debug.WriteLine($"Icon not found: {iconPath}");
            return SafeHICON.Null;
        }
        // Load small icon (16x16 usually)
        // LR_LOADFROMFILE is crucial for loading from a file path directly.
        // For .ico files that might contain multiple sizes, LoadImage can pick the best fit.
        // We specify 0,0 to let it pick the icon's natural size or the first image.
        // For specific sizes, use GetSystemMetrics(SystemMetric.SM_CXSMICON) etc.
        var hIcon = LoadImage(IntPtr.Zero, iconPath, LoadImageType.IMAGE_ICON,
                              GetSystemMetrics(SystemMetric.SM_CXSMICON), // desired width for small icon
                              GetSystemMetrics(SystemMetric.SM_CYSMICON), // desired height for small icon
                              LoadImageOptions.LR_LOADFROMFILE | LoadImageOptions.LR_SHARED);
        if (hIcon.IsNull)
        {
            Debug.WriteLine($"Failed to load icon: {iconPath}, Error: {Kernel32.GetLastError()}");
            return SafeHICON.Null;
        }
        return new SafeHICON(hIcon.DangerousGetHandle(), true);
    }


    private void CreateImageList()
    {
        if (_hPlayIcon == null || _hPlayIcon.IsInvalid || _hPauseIcon == null || _hPauseIcon.IsInvalid ||
            _hPrevIcon == null || _hPrevIcon.IsInvalid || _hNextIcon == null || _hNextIcon.IsInvalid ||
            _hShuffleOnIcon == null || _hShuffleOnIcon.IsInvalid || _hShuffleOffIcon == null || _hShuffleOffIcon.IsInvalid)
        {
            Debug.WriteLine("TaskbarThumbnailManager: Cannot create ImageList due to missing icons.");
            return;
        }

        // Create an image list.
        // Using GetSystemMetrics for icon sizes to be DPI aware.
        int cx = GetSystemMetrics(SystemMetric.SM_CXSMICON); // Small icon width
        int cy = GetSystemMetrics(SystemMetric.SM_CYSMICON); // Small icon height

        _hImageList = ImageList_Create(cx, cy, ImageListFlags.ILC_COLOR32 | ImageListFlags.ILC_MASK, 6, 1);
        if (_hImageList == null || _hImageList.IsInvalid)
        {
            Debug.WriteLine($"TaskbarThumbnailManager: ImageList_Create failed. Error: {Kernel32.GetLastError()}");
            return;
        }

        // Vanara's SafeHIMAGELIST doesn't directly expose AddIcon, so we use the P/Invoke
        ImageList_AddIcon(_hImageList, _hPlayIcon);       // Index 0
        ImageList_AddIcon(_hImageList, _hPauseIcon);      // Index 1
        ImageList_AddIcon(_hImageList, _hPrevIcon);       // Index 2
        ImageList_AddIcon(_hImageList, _hNextIcon);       // Index 3
        ImageList_AddIcon(_hImageList, _hShuffleOffIcon); // Index 4
        ImageList_AddIcon(_hImageList, _hShuffleOnIcon);  // Index 5

        _taskbarList?.ThumbBarSetImageList(_hwnd, _hImageList);
    }

    private void AddThumbButtons()
    {
        if (_taskbarList == null || _hImageList == null || _hImageList.IsInvalid)
        {
            Debug.WriteLine("TaskbarThumbnailManager: TaskbarList or ImageList not initialized. Cannot add buttons.");
            return;
        }

        var buttons = new List<THUMBBUTTON>
            {
                new THUMBBUTTON // Previous
                {
                    iId = ID_PREV,
                    dwMask = THUMBBUTTONMASK.THB_ICON | THUMBBUTTONMASK.THB_TOOLTIP | THUMBBUTTONMASK.THB_FLAGS,
                    iBitmap = 2, // Index in ImageList for _hPrevIcon
                    szTip = "Previous",
                    dwFlags = THUMBBUTTONFLAGS.THBF_ENABLED
                },
                new THUMBBUTTON // Play/Pause (initial state: Play)
                {
                    iId = ID_PLAYPAUSE,
                    dwMask = THUMBBUTTONMASK.THB_ICON | THUMBBUTTONMASK.THB_TOOLTIP | THUMBBUTTONMASK.THB_FLAGS,
                    iBitmap = _isPlaying ? 1 : 0, // Index for _hPauseIcon or _hPlayIcon
                    szTip = _isPlaying ? "Pause" : "Play",
                    dwFlags = THUMBBUTTONFLAGS.THBF_ENABLED
                },
                new THUMBBUTTON // Next
                {
                    iId = ID_NEXT,
                    dwMask = THUMBBUTTONMASK.THB_ICON | THUMBBUTTONMASK.THB_TOOLTIP | THUMBBUTTONMASK.THB_FLAGS,
                    iBitmap = 3, // Index in ImageList for _hNextIcon
                    szTip = "Next",
                    dwFlags = THUMBBUTTONFLAGS.THBF_ENABLED
                },
                new THUMBBUTTON // Shuffle (initial state: Off)
                {
                    iId = ID_SHUFFLE,
                    dwMask = THUMBBUTTONMASK.THB_ICON | THUMBBUTTONMASK.THB_TOOLTIP | THUMBBUTTONMASK.THB_FLAGS,
                    iBitmap = _isShuffleOn ? 5 : 4, // Index for _hShuffleOnIcon or _hShuffleOffIcon
                    szTip = _isShuffleOn ? "Shuffle On" : "Shuffle Off",
                    dwFlags = THUMBBUTTONFLAGS.THBF_ENABLED | THUMBBUTTONFLAGS.THBF_NOBACKGROUND // Example of no background
                }
            };

        HRESULT hr = _taskbarList.ThumbBarAddButtons(_hwnd, buttons.ToArray());
        if (hr.Failed)
        {
            Debug.WriteLine($"TaskbarThumbnailManager: ThumbBarAddButtons failed. HRESULT: {hr}");
        }
    }

    public void UpdatePlayPauseButton(bool isPlaying)
    {
        if (_taskbarList == null || _hImageList == null || _hImageList.IsInvalid)
            return;
        _isPlaying = isPlaying;

        THUMBBUTTON playPauseButton = new THUMBBUTTON
        {
            iId = ID_PLAYPAUSE,
            dwMask = THUMBBUTTONMASK.THB_ICON | THUMBBUTTONMASK.THB_TOOLTIP,
            iBitmap = _isPlaying ? 1 : 0, // Pause icon or Play icon
            szTip = _isPlaying ? "Pause" : "Play"
        };
        _taskbarList.ThumbBarUpdateButtons(_hwnd, new[] { playPauseButton });
    }

    public void UpdateShuffleButton(bool isShuffleOn)
    {
        if (_taskbarList == null || _hImageList == null || _hImageList.IsInvalid)
            return;
        _isShuffleOn = isShuffleOn;

        THUMBBUTTON shuffleButton = new THUMBBUTTON
        {
            iId = ID_SHUFFLE,
            dwMask = THUMBBUTTONMASK.THB_ICON | THUMBBUTTONMASK.THB_TOOLTIP,
            iBitmap = _isShuffleOn ? 5 : 4, // Shuffle On icon or Shuffle Off icon
            szTip = _isShuffleOn ? "Shuffle On" : "Shuffle Off"
        };
        _taskbarList.ThumbBarUpdateButtons(_hwnd, new[] { shuffleButton });
    }

    public void SetProgressState(TBPFLAG state)
    {
        _taskbarList?.SetProgressState(_hwnd, state);
    }

    public void SetProgressValue(ulong completed, ulong total)
    {
        _taskbarList?.SetProgressValue(_hwnd, completed, total);
    }


    private void HookWndProc()
    {
        if (_hwnd.IsNull || _oldWndProc != IntPtr.Zero)
            return; // Already hooked or no HWND

        _newWndProc = new WndProc(MyWndProc); // Keep a reference to prevent GC
        _oldWndProc = SetWindowLongPtr(_hwnd, GWL_WNDPROC, Marshal.GetFunctionPointerForDelegate(_newWndProc));
        if (_oldWndProc == IntPtr.Zero)
        {
            Debug.WriteLine($"TaskbarThumbnailManager: SetWindowLongPtr failed. Error: {Kernel32.GetLastError()}");
        }
    }

    private void UnhookWndProc()
    {
        if (_hwnd.IsNull || _oldWndProc == IntPtr.Zero)
            return;

        SetWindowLongPtr(_hwnd, GWL_WNDPROC, _oldWndProc);
        _oldWndProc = IntPtr.Zero;
        _newWndProc = null; // Allow GC
    }

    private IntPtr MyWndProc(HWND hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_COMMAND)
        {
            // For WM_COMMAND, wParam high word is notification code (0 for menu/accelerator),
            // low word is control/menu/accelerator ID.
            if (Macros.HIWORD(wParam.ToInt32()) == 0) // Command from toolbar button
            {
                ushort commandId = Macros.LOWORD(wParam.ToInt32());
                bool processed = ProcessButtonCommand(commandId);
                if (processed)
                    return IntPtr.Zero; // Handled
            }
        }

        // Call original WndProc for other messages
        return _oldWndProc != IntPtr.Zero ? CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam) : DefWindowProc(hWnd, msg, wParam, lParam);
    }

    private bool ProcessButtonCommand(ushort commandId)
    {
        switch (commandId)
        {
            case ID_PLAYPAUSE:
                Debug.WriteLine("Play/Pause button clicked");
                OnPlayPauseClicked?.Invoke();
                // The actual icon/tooltip update should be done by the client
                // by calling UpdatePlayPauseButton based on the new state.
                return true;
            case ID_PREV:
                Debug.WriteLine("Previous button clicked");
                OnPrevClicked?.Invoke();
                return true;
            case ID_NEXT:
                Debug.WriteLine("Next button clicked");
                OnNextClicked?.Invoke();
                return true;
            case ID_SHUFFLE:
                Debug.WriteLine("Shuffle button clicked");
                OnShuffleClicked?.Invoke();
                // The actual icon/tooltip update by client via UpdateShuffleButton.
                return true;
        }
        return false;
    }


    public void Dispose()
    {
        UnhookWndProc();

        // Release COM object
        if (_taskbarList != null)
        {
            // Vanara's TaskbarList is IDisposable and should release ITaskbarList internally
            _taskbarList.Dispose();
            _taskbarList = null;
        }

        // Destroy image list
        if (_hImageList != null && !_hImageList.IsInvalid)
        {
            _hImageList.Dispose(); // SafeHIMAGELIST will call ImageList_Destroy
            _hImageList = null;
        }

        // Destroy icons (SafeHICON will call DestroyIcon)
        _hPlayIcon?.Dispose();
        _hPauseIcon?.Dispose();
        _hPrevIcon?.Dispose();
        _hNextIcon?.Dispose();
        _hShuffleOnIcon?.Dispose();
        _hShuffleOffIcon?.Dispose();

        // Ole32.CoUninitialize(); // If CoInitializeEx was called
        GC.SuppressFinalize(this);
    }

    // Helper to set GWL_WNDPROC. User32.SetWindowLongPtr is overloaded.
    // We need the one that takes a delegate (which gets marshaled to a function pointer).
    private static IntPtr SetWindowLongPtr(HWND hWnd, int nIndex, WndProc newProc)
    {
        IntPtr pNewProc = Marshal.GetFunctionPointerForDelegate(newProc);
        return SetWindowLongPtr(hWnd, nIndex, pNewProc);
    }
}