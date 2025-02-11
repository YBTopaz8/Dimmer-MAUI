using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer_MAUI.Platforms.Windows;

// Stock icon identifiers (from SHSTOCKICONID)
public enum SHSTOCKICONID : uint
{
    SIID_MEDIA_PLAY = 31,
    SIID_MEDIA_PAUSE = 32,
    SIID_MEDIA_STOP = 33,   // Use Play for Resume, so no separate resume icon.
    SIID_MEDIA_PREV = 34,
    SIID_MEDIA_NEXT = 35
}

[Flags]
public enum SHGSI : uint
{
    SHGSI_ICON = 0x000000100,
    SHGSI_SMALLICON = 0x000000001
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct SHSTOCKICONINFO
{
    public uint cbSize;
    public IntPtr hIcon;
    public int iSysImageIndex;
    public int iIcon;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
    public string szPath;
    

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    public static extern int SHGetStockIconInfo(SHSTOCKICONID siid, uint uFlags, ref SHSTOCKICONINFO psii);

    public static class StockIconHelper
{
    public static IntPtr GetStockIconHandle(SHSTOCKICONID iconId, SHGSI flags)
    {
        SHSTOCKICONINFO info = new SHSTOCKICONINFO();
        info.cbSize = (uint)Marshal.SizeOf(typeof(SHSTOCKICONINFO));
        int hr = SHGetStockIconInfo(iconId, (uint)flags, ref info);
        return (hr == 0) ? info.hIcon : IntPtr.Zero;
    }
}

// COM interfaces for the taskbar thumbnail toolbar.
[ComImport]
[Guid("56FDF344-FD6D-11d0-958A-006097C9A090")]
[ClassInterface(ClassInterfaceType.None)]
public class CTaskbarList { }

[ComImport]
[Guid("C43DC798-95D1-4BEA-9030-BB99E2983A1A")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface ITaskbarList3
{
    void HrInit();
    void AddTab(IntPtr hwnd);
    void DeleteTab(IntPtr hwnd);
    void ActivateTab(IntPtr hwnd);
    void SetActiveAlt(IntPtr hwnd);
    void MarkFullscreenWindow(IntPtr hwnd, [MarshalAs(UnmanagedType.Bool)] bool fFullscreen);
    void SetProgressValue(IntPtr hwnd, ulong ullCompleted, ulong ullTotal);
    void SetProgressState(IntPtr hwnd, TBPFLAG tbpFlags);
    void RegisterTab(IntPtr hwndTab, IntPtr hwndMDI);
    void UnregisterTab(IntPtr hwndTab);
    void SetTabOrder(IntPtr hwndTab, IntPtr hwndInsertBefore);
    void SetTabActive(IntPtr hwndTab, IntPtr hwndMDI, TBATFLAG tbatFlags);
    void ThumbBarAddButtons(IntPtr hwnd, uint cButtons, [MarshalAs(UnmanagedType.LPArray)] THUMBBUTTON[] pButton);
    void ThumbBarUpdateButtons(IntPtr hwnd, uint cButtons, [MarshalAs(UnmanagedType.LPArray)] THUMBBUTTON[] pButton);
}

public enum TBPFLAG
{
    TBPF_NOPROGRESS = 0,
    TBPF_INDETERMINATE = 1,
    TBPF_NORMAL = 2,
    TBPF_ERROR = 4,
    TBPF_PAUSED = 8
}

[Flags]
public enum TBATFLAG
{
    TBATF_USEMDITHUMBNAIL = 1,
    TBATF_USEMDILIVEPREVIEW = 2
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
    THBF_ENABLED = 0,
    THBF_DISABLED = 1,
    THBF_DISMISSONCLICK = 2,
    THBF_NOBACKGROUND = 4,
    THBF_HIDDEN = 8,
    THBF_NONINTERACTIVE = 16
}

public static class TaskbarThumbnailHelper
{
    private static ITaskbarList3 _taskbarList = (ITaskbarList3)new CTaskbarList();

    static TaskbarThumbnailHelper()
    {
        _taskbarList.HrInit();
    }

    /// <summary>
    /// Adds five thumbnail toolbar buttons for media control:
    /// Play (ID 100), Pause (101), Resume (102), Previous (103), and Next (104).
    /// </summary>
    public static void AddThumbnailButtons(IntPtr hwnd)
    {
        THUMBBUTTON[] buttons = new THUMBBUTTON[5];

        // Button 0: Play
        buttons[0] = new THUMBBUTTON
        {
            dwMask = THUMBBUTTONMASK.THB_ICON | THUMBBUTTONMASK.THB_TOOLTIP | THUMBBUTTONMASK.THB_FLAGS,
            iId = 100,
            hIcon = StockIconHelper.GetStockIconHandle(SHSTOCKICONID.SIID_MEDIA_PLAY, SHGSI.SHGSI_SMALLICON),
            szTip = "Play",
            dwFlags = THUMBBUTTONFLAGS.THBF_ENABLED
        };

        // Button 1: Pause
        buttons[1] = new THUMBBUTTON
        {
            dwMask = THUMBBUTTONMASK.THB_ICON | THUMBBUTTONMASK.THB_TOOLTIP | THUMBBUTTONMASK.THB_FLAGS,
            iId = 101,
            hIcon = StockIconHelper.GetStockIconHandle(SHSTOCKICONID.SIID_MEDIA_PAUSE, SHGSI.SHGSI_SMALLICON),
            szTip = "Pause",
            dwFlags = THUMBBUTTONFLAGS.THBF_ENABLED
        };

        // Button 2: Resume (using the Play icon)
        buttons[2] = new THUMBBUTTON
        {
            dwMask = THUMBBUTTONMASK.THB_ICON | THUMBBUTTONMASK.THB_TOOLTIP | THUMBBUTTONMASK.THB_FLAGS,
            iId = 102,
            hIcon = StockIconHelper.GetStockIconHandle(SHSTOCKICONID.SIID_MEDIA_PLAY, SHGSI.SHGSI_SMALLICON),
            szTip = "Resume",
            dwFlags = THUMBBUTTONFLAGS.THBF_ENABLED
        };

        // Button 3: Previous
        buttons[3] = new THUMBBUTTON
        {
            dwMask = THUMBBUTTONMASK.THB_ICON | THUMBBUTTONMASK.THB_TOOLTIP | THUMBBUTTONMASK.THB_FLAGS,
            iId = 103,
            hIcon = StockIconHelper.GetStockIconHandle(SHSTOCKICONID.SIID_MEDIA_PREV, SHGSI.SHGSI_SMALLICON),
            szTip = "Previous",
            dwFlags = THUMBBUTTONFLAGS.THBF_ENABLED
        };

        // Button 4: Next
        buttons[4] = new THUMBBUTTON
        {
            dwMask = THUMBBUTTONMASK.THB_ICON | THUMBBUTTONMASK.THB_TOOLTIP | THUMBBUTTONMASK.THB_FLAGS,
            iId = 104,
            hIcon = StockIconHelper.GetStockIconHandle(SHSTOCKICONID.SIID_MEDIA_NEXT, SHGSI.SHGSI_SMALLICON),
            szTip = "Next",
            dwFlags = THUMBBUTTONFLAGS.THBF_ENABLED
        };

        _taskbarList.ThumbBarAddButtons(hwnd, (uint)buttons.Length, buttons);
    }
}
}