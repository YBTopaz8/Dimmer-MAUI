using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;
using Windows.Graphics.Display;
using Microsoft.Maui.Storage;
using System.Diagnostics;
using Dimmer.WinUI.Utils.StaticUtils;

namespace Dimmer.WinUI.Utils.StaticUtils;

public static class WindowDockManager
{
    private const string PrefKeyPrefix = "WinDock_";
    private const int AnimationSteps = 10;
    private static readonly TimeSpan StepDelay = TimeSpan.FromMilliseconds(12);

    public static void RestoreWindowPositions(IWinUIWindowMgrService mgr)
    {
        foreach (var win in mgr.GetOpenNativeWindows())
        {
            try
            {
                string id = PrefKeyPrefix + PlatUtils.GetHWId(win);
                if (!Preferences.Default.ContainsKey(id)) continue;

                var rectStr = Preferences.Default.Get(id, string.Empty);
                if (string.IsNullOrWhiteSpace(rectStr)) continue;

                var parts = rectStr.Split(',');
                if (parts.Length != 4) continue;

                if (!int.TryParse(parts[0], out int x) ||
                    !int.TryParse(parts[1], out int y) ||
                    !int.TryParse(parts[2], out int w) ||
                    !int.TryParse(parts[3], out int h))
                    continue;

                var appWin = PlatUtils.GetAppWindow(win);
                appWin.MoveAndResize(new RectInt32(x, y, w, h));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DockMgr] Restore fail for {win.Title}: {ex.Message}");
            }
        }
    }

    public static void SaveWindowPosition(Microsoft.UI.Xaml.Window win)
    {
        try
        {
            var appWin = PlatUtils.GetAppWindow(win);
            var pos = appWin.Position;
            var sz = appWin.Size;
            Preferences.Default.Set(
                PrefKeyPrefix + PlatUtils.GetHWId(win),
                $"{pos.X},{pos.Y},{sz.Width},{sz.Height}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DockMgr] Save fail for {win.Title}: {ex.Message}");
        }
    }

    public static async void SnapHomeWindow(Microsoft.UI.Xaml.Window home, IEnumerable<Microsoft.UI.Xaml.Window> others)
    {
        try
        {
            var hApp = PlatUtils.GetAppWindow(home);
            var area = DisplayArea.GetFromWindowId(hApp.Id, DisplayAreaFallback.Primary);
            var work = area.WorkArea;

            var candidates = others.Where(o => o != home).Select(PlatUtils.GetAppWindow).ToList();
            if (candidates.Count == 0) return;

            var nearest = candidates
                .OrderBy(c => Distance(hApp.Position, c.Position))
                .First();

            var newPos = CalculateSnapPosition(hApp, nearest, work);
            await SmoothMoveAsync(hApp, newPos);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DockMgr] Snap error: {ex.Message}");
        }
    }

    private static PointInt32 CalculateSnapPosition(AppWindow home, AppWindow target, RectInt32 work)
    {
        int hW = home.Size.Width, hH = home.Size.Height;
        int tX = target.Position.X, tY = target.Position.Y;
        int tW = target.Size.Width, tH = target.Size.Height;

        if (tX + tW + hW <= work.X + work.Width) return new(tX + tW, tY);     // right
        if (tX - hW >= work.X) return new(tX - hW, tY);                       // left
        if (tY + tH + hH <= work.Y + work.Height) return new(tX, tY + tH);    // below
        if (tY - hH >= work.Y) return new(tX, tY - hH);                       // above

        return new(work.X + (work.Width - hW) / 2, work.Y + (work.Height - hH) / 2);
    }

    private static async Task SmoothMoveAsync(AppWindow win, PointInt32 target)
    {
        try
        {
            var start = win.Position;
            int step = 0;
            while (step < AnimationSteps)
            {
                step++;
                int x = start.X + (target.X - start.X) * step / AnimationSteps;
                int y = start.Y + (target.Y - start.Y) * step / AnimationSteps;
                win.Move(new PointInt32(x, y));
                await Task.Delay(StepDelay);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DockMgr] Smooth move failed: {ex.Message}");
            win.Move(target);
        }
    }

    private static double Distance(PointInt32 a, PointInt32 b)
    {
        int dx = a.X - b.X, dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }
}
