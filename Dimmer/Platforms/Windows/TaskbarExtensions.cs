using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer_MAUI.Platforms.Windows;
public static class TaskbarExtensions
{
    [DllImport("shell32.dll")]
    private static extern void SHGetPropertyStoreForWindow(IntPtr hwnd, ref Guid iid, [Out(), MarshalAs(UnmanagedType.Interface)] out IPropertyStore propertyStore);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [ComImport]
    [Guid("886d8eeb-8cf2-4446-8d02-cdba1dbdcf99")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPropertyStore
    {
        void GetCount(out uint count);
        void GetAt(uint iProp, ref PropertyKey pkey, [Out(), MarshalAs(UnmanagedType.Struct)] out PropVariant pv);
        void SetValue(ref PropertyKey key, [In(), MarshalAs(UnmanagedType.Struct)] ref PropVariant pv);
        void Commit();
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    private struct PropertyKey
    {
        public Guid fmtid;
        public uint pid;

        public PropertyKey(Guid fmtid, uint pid)
        {
            this.fmtid = fmtid;
            this.pid = pid;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct PropVariant
    {
        [FieldOffset(0)]
        public ushort vt;
        [FieldOffset(8)]
        public IntPtr pwszVal;
        [FieldOffset(8)]
        public uint uintVal;
    }

    [ComImport]
    [Guid("56FDF344-FD6D-11d0-958A-006097C9A090")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ITaskbarList
    {
        void HrInit();
        void AddTab(IntPtr hwnd);
        void DeleteTab(IntPtr hwnd);
        void ActivateTab(IntPtr hwnd);
        void SetActiveAlt(IntPtr hwnd);
    }

    [ComImport]
    [Guid("EA1AFB91-9E28-4B86-90E9-9E9F8A5EEA6E")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ITaskbarList3 : ITaskbarList
    {
        void HrInit();
        void AddTab(IntPtr hwnd);
        void DeleteTab(IntPtr hwnd);
        void ActivateTab(IntPtr hwnd);
        void SetActiveAlt(IntPtr hwnd);
        void MarkFullscreenWindow(IntPtr hwnd, [MarshalAs(UnmanagedType.Bool)] bool fFullscreen);
        void SetProgressValue(IntPtr hwnd, ulong ullCompleted, ulong ullTotal);
        void SetProgressState(IntPtr hwnd, TBPFLAG tbpFlags);
    }

    [ComImport]
    [Guid("C43DC798-95D1-4BEA-9030-BB99E2983A1A")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ITaskbarList4 : ITaskbarList3
    {
        void ThumbBarAddButtons(IntPtr hwnd, uint cButtons, [MarshalAs(UnmanagedType.LPArray)] ThumbButton[] pButtons);
    }

    private enum TBPFLAG
    {
        TBPF_NOPROGRESS = 0,
        TBPF_INDETERMINATE = 0x1,
        TBPF_NORMAL = 0x2,
        TBPF_ERROR = 0x4,
        TBPF_PAUSED = 0x8
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct ThumbButton
    {
        public THBFLAGS dwMask;
        public uint iId;
        public uint iBitmap;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szTip;
        public THBFLAGS dwFlags;
    }

    [Flags]
    public enum THBFLAGS : uint
    {
        THBF_ENABLED = 0x00000004,
        THBF_DISMISSONCLICK = 0x00000008
    }

    [ComImport]
    [Guid("56FDF344-FD6D-11d0-958A-006097C9A090")]
    [ClassInterface(ClassInterfaceType.None)]
    private class TaskbarList { }

    public static void SetTaskbarProgress(IntPtr hwnd, double progress)
    {
        ITaskbarList3 taskbarList = (ITaskbarList3)new TaskbarList();
        taskbarList.HrInit();
        if (progress < 0 || progress > 1)
            throw new ArgumentOutOfRangeException(nameof(progress));
        taskbarList.SetProgressValue(hwnd, (ulong)(progress * 100), 100);
        taskbarList.SetProgressState(hwnd, TBPFLAG.TBPF_NORMAL);
    }

    public static void SetThumbnailButtons(IntPtr hwnd)
    {
        var playButton = new ThumbButton
        {
            iId = 0,
            szTip = "Play",
            dwMask = THBFLAGS.THBF_ENABLED | THBFLAGS.THBF_DISMISSONCLICK,
            hIcon = LoadIcon(IntPtr.Zero, new IntPtr(32512)) // Standard application icon
        };

        var pauseButton = new ThumbButton
        {
            iId = 1,
            szTip = "Pause",
            dwMask = THBFLAGS.THBF_ENABLED | THBFLAGS.THBF_DISMISSONCLICK,
            hIcon = LoadIcon(IntPtr.Zero, new IntPtr(32512)) // Standard application icon
        };

        var stopButton = new ThumbButton
        {
            iId = 2,
            szTip = "Stop",
            dwMask = THBFLAGS.THBF_ENABLED | THBFLAGS.THBF_DISMISSONCLICK,
            hIcon = LoadIcon(IntPtr.Zero, new IntPtr(32512)) // Standard application icon
        };

        var buttons = new[] { playButton, pauseButton, stopButton };
        ITaskbarList4 taskbarList = (ITaskbarList4)new TaskbarList();
        taskbarList.HrInit();
        taskbarList.ThumbBarAddButtons(hwnd, (uint)buttons.Length, buttons);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);
}
