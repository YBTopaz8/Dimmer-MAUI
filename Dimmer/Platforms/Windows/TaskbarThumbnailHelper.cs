using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer_MAUI.Platforms.Windows;
// COM class for the TaskbarList.
[ComImport]
[Guid("56FDF344-FD6D-11d0-958A-006097C9A090")]
[ClassInterface(ClassInterfaceType.None)]
public class CTaskbarList { }

// ITaskbarList interface.
[ComImport]
[Guid("56FDF344-FD6D-11d0-958A-006097C9A090")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface ITaskbarList
{
    void HrInit();
    void AddTab(IntPtr hwnd);
    void DeleteTab(IntPtr hwnd);
    void ActivateTab(IntPtr hwnd);
    void SetActiveAlt(IntPtr hwnd);
}

// ITaskbarList2 (inherits ITaskbarList).
[ComImport]
[Guid("602D4995-B13A-429b-A66E-1935E44F4317")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface ITaskbarList2 : ITaskbarList
{
    void MarkFullscreenWindow(IntPtr hwnd, [MarshalAs(UnmanagedType.Bool)] bool fFullscreen);
}

// ITaskbarList3 (inherits ITaskbarList2).
[ComImport]
[Guid("C43DC798-95D1-4BEA-9030-BB99E2983A1A")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface ITaskbarList3 : ITaskbarList2
{
    // Methods from ITaskbarList and ITaskbarList2 omitted for brevity.
    void SetProgressValue(IntPtr hwnd, ulong ullCompleted, ulong ullTotal);
    void SetProgressState(IntPtr hwnd, TBPFLAG tbpFlags);
    void RegisterTab(IntPtr hwndTab, IntPtr hwndMDI);
    void UnregisterTab(IntPtr hwndTab);
    void SetTabOrder(IntPtr hwndTab, IntPtr hwndInsertBefore);
    void SetTabActive(IntPtr hwndTab, IntPtr hwndMDI, TBATFLAG tbatFlags);
    void ThumbBarAddButtons(IntPtr hwnd, uint cButtons, [MarshalAs(UnmanagedType.LPArray)] THUMBBUTTON[] pButton);
    void ThumbBarUpdateButtons(IntPtr hwnd, uint cButtons, [MarshalAs(UnmanagedType.LPArray)] THUMBBUTTON[] pButton);
    // Other methods omitted.
}

public enum TBPFLAG
{
    TBPF_NOPROGRESS = 0,
    TBPF_INDETERMINATE = 0x1,
    TBPF_NORMAL = 0x2,
    TBPF_ERROR = 0x4,
    TBPF_PAUSED = 0x8
}

[Flags]
public enum TBATFLAG
{
    TBATF_USEMDITHUMBNAIL = 0x1,
    TBATF_USEMDILIVEPREVIEW = 0x2
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
public struct THUMBBUTTON
{
    public THUMBBUTTONMASK dwMask;
    public uint iId;
    public uint iBitmap;
    public IntPtr hIcon;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
    public string szTip;
    public THUMBBUTTONFLAGS dwFlags;
}

[Flags]
public enum THUMBBUTTONMASK : uint
{
    THB_BITMAP = 0x1,
    THB_ICON = 0x2,
    THB_TOOLTIP = 0x4,
    THB_FLAGS = 0x8
}

[Flags]
public enum THUMBBUTTONFLAGS : uint
{
    THBF_ENABLED = 0x0,
    THBF_DISABLED = 0x1,
    THBF_DISMISSONCLICK = 0x2,
    THBF_NOBACKGROUND = 0x4,
    THBF_HIDDEN = 0x8,
    THBF_NONINTERACTIVE = 0x10
}

public static class TaskbarThumbnailHelper
{
    // Instantiate the COM object and initialize.
    private static ITaskbarList3 _taskbarList = (ITaskbarList3)new CTaskbarList();

    static TaskbarThumbnailHelper()
    {
        _taskbarList.HrInit();
    }

    /// <summary>
    /// Adds two thumbnail toolbar buttons to the given window.
    /// Button IDs 100 and 101 are used.
    /// </summary>
    public static void AddThumbnailButtons(IntPtr hwnd)
    {
        THUMBBUTTON[] buttons = new THUMBBUTTON[2];

        // Button 1: "Play"
        buttons[0] = new THUMBBUTTON
        {
            dwMask = THUMBBUTTONMASK.THB_ICON | THUMBBUTTONMASK.THB_TOOLTIP | THUMBBUTTONMASK.THB_FLAGS,
            iId = 100, // custom command ID
            hIcon = LoadIcon(IntPtr.Zero, (IntPtr)32512), // Using a default icon (IDI_APPLICATION)
            szTip = "Play",
            dwFlags = THUMBBUTTONFLAGS.THBF_ENABLED
        };

        // Button 2: "Stop"
        buttons[1] = new THUMBBUTTON
        {
            dwMask = THUMBBUTTONMASK.THB_ICON | THUMBBUTTONMASK.THB_TOOLTIP | THUMBBUTTONMASK.THB_FLAGS,
            iId = 101, // custom command ID
            hIcon = LoadIcon(IntPtr.Zero, (IntPtr)32512),
            szTip = "Stop",
            dwFlags = THUMBBUTTONFLAGS.THBF_ENABLED
        };

        _taskbarList.ThumbBarAddButtons(hwnd, (uint)buttons.Length, buttons);
    }

    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);
}
