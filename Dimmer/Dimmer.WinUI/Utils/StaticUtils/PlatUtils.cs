
using System.Drawing.Imaging;

using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

using Windows.Graphics;

using Application = Microsoft.Maui.Controls.Application;
using Compositor = Microsoft.UI.Composition.Compositor;
using FlyoutBase = Microsoft.UI.Xaml.Controls.Primitives.FlyoutBase;
using ImageFormat = System.Drawing.Imaging.ImageFormat;
using ImageSource = Microsoft.Maui.Controls.ImageSource;

namespace Dimmer.WinUI.Utils.StaticUtils;
public static class PlatUtils
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetWindowRect(IntPtr hwnd, out Rect lpRect);

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect { public int Left, Top, Right, Bottom; }

    /// <summary>
    /// Captures the given MAUI Window to a PNG-backed ImageSource.
    /// </summary>
    public static ImageSource CaptureWindow(this Microsoft.Maui.Controls.Window mauiWindow)
    {
        if (mauiWindow == null)
            throw new ArgumentNullException(nameof(mauiWindow));
        // 1) get native handle
        var native = mauiWindow.Handler.PlatformView as Microsoft.Maui.MauiWinUIWindow
                     ?? throw new InvalidOperationException("Not running on WinUI");
        IntPtr hwnd = WindowNative.GetWindowHandle(native);

        // 2) grab bounds
        GetWindowRect(hwnd, out var rect);
        int w = rect.Right - rect.Left;
        int h = rect.Bottom - rect.Top;

        // 3) render into Bitmap
        using var bmp = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        IntPtr hdc = g.GetHdc();
        PrintWindow(hwnd, hdc, 0);
        g.ReleaseHdc(hdc);

        // 4) encode to PNG in-memory
        var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);
        ms.Position = 0;

        // 5) wrap in MAUI ImageSource
        return ImageSource.FromStream(() => ms);
    }
    public static IntPtr DimmerHandle { get; set; }
    public static bool IsAppInForeground { get; set; }
    public static AppWindowPresenter? AppWinPresenter { get; set; }
    public static OverlappedPresenter? OverLappedPres { get; set; }

    public static class ShowCloseConfirmationPopUp
    {
        const bool showCloseConfirmation = false;
        public static bool ShowCloseConfirmation
        {
            get => Preferences.Default.Get(nameof(ShowCloseConfirmation), showCloseConfirmation);
            set => Preferences.Default.Set(nameof(ShowCloseConfirmation), value);
        }
        public static void ToggleCloseConfirmation(bool showClose)
        {
            ShowCloseConfirmation = showClose;
        }
        public static bool GetCloseConfirmation()
        {
            return ShowCloseConfirmation;
        }
    }
    // Method to set the window on top
    public static void ToggleWindowAlwaysOnTop(bool topMost, AppWindowPresenter? appPresenter)
    {
        try
        {

            OverLappedPres = appPresenter as OverlappedPresenter;
            if (topMost)
            {
                OverLappedPres!.IsAlwaysOnTop = true;
            }
            else
            {
                OverLappedPres!.IsAlwaysOnTop = false;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"{ex.Message}");
        }
    }




    public static void MiniMimizeWindow(Microsoft.Maui.Controls.Window win)
    {

        var nativeWindow = win.Handler.PlatformView;
        IntPtr windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(nativeWindow);
        ShowWindow(windowHandle, SW_HIDE);
        //System.Windows.SystemCommands.MinimizeWindow(win);
    }
    public static void ToggleFullScreenMode(bool IsToFullScreen, AppWindowPresenter? appPresenter)
    {
        try
        {
            OverLappedPres = appPresenter as OverlappedPresenter;
            if (IsToFullScreen)
            {
                OverLappedPres!.IsAlwaysOnTop = true;
                OverLappedPres.SetBorderAndTitleBar(false, false);
                OverLappedPres!.Maximize();
                
            }
            else
            {
                OverLappedPres!.IsAlwaysOnTop = false;
                OverLappedPres.SetBorderAndTitleBar(true, true);
                OverLappedPres!.Restore();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"{ex.Message}");
        }
    }

    public static void OpenAndSetWindowToEdgePosition(Microsoft.Maui.Controls.Window concernedWindow, RectInt32 positionToSet)
    {
        var nativeWindow = GetNativeWindowFromMAUIWindow(concernedWindow);
        if (nativeWindow is null) return;
        nativeWindow.AppWindow.IsShownInSwitchers = false;
        var sysBackDrop = new MicaBackdrop();
        nativeWindow.SystemBackdrop = sysBackDrop;
        var pres = nativeWindow.AppWindow.Presenter;
        //window.SetTitleBar()
        var p = pres as OverlappedPresenter;
        if (p != null)
        {
            p.SetBorderAndTitleBar(false, false);
            p.IsResizable = false;
            p.IsAlwaysOnTop = true;

        }


        nativeWindow.Activate();
        nativeWindow.AppWindow.MoveAndResize(positionToSet);

        nativeWindow.AppWindow.MoveInZOrderAtTop();

    }

    public static void ResizeWindow(this Microsoft.Maui.Controls.Window concernedWindow, SizeInt32 sizeToSet)
    {
        var nativeWindow = GetNativeWindowFromMAUIWindow(concernedWindow);
        var newRect = new RectInt32
        {

            Width = sizeToSet.Width,
            Height = sizeToSet.Height
        };
        nativeWindow.AppWindow.Resize(sizeToSet);
        nativeWindow.AppWindow.MoveInZOrderAtTop();
    }

    public static void ResizeNativeWindow(Microsoft.UI.Xaml.Window concernedWindow, SizeInt32 sizeToSet)
    {
        
        var newRect = new RectInt32
        {

            Width = sizeToSet.Width,
            Height = sizeToSet.Height
        };
        concernedWindow.AppWindow.Resize(sizeToSet);
        concernedWindow.AppWindow.MoveInZOrderAtTop();
    }

    public static void MoveAndResizeCenter(Microsoft.UI.Xaml.Window nativeWindow, SizeInt32 sizeToSet)
    {

        var width = DisplayArea.Primary.WorkArea.Width;
        var height = DisplayArea.Primary.WorkArea.Height;
        var newRect = new RectInt32
        {
            Height = sizeToSet.Height,
            Width = sizeToSet.Width,
            X = (width - sizeToSet.Width) / 2,
            Y = (height - sizeToSet.Height) / 2
        };
        nativeWindow.AppWindow.MoveAndResize(newRect);
        nativeWindow.AppWindow.MoveInZOrderAtTop();
    }
    public static void MoveAndResizeWindow(this Microsoft.Maui.Controls.Window concernedWindow, RectInt32 positionToSet)
    {
        var nativeWindow = GetNativeWindowFromMAUIWindow(concernedWindow);

        //var disInfo= DisplayInformation.GetForCurrentView();

        var width = DisplayArea.Primary.WorkArea.Width;
        var height = DisplayArea.Primary.WorkArea.Height;

        //var width = DisplayArea.Primary.WorkArea.Width;
        //var height = DisplayArea.Primary.WorkArea.Height;
        //var x = DisplayArea.Primary.WorkArea.X;
        //var y = DisplayArea.Primary.WorkArea.Y;
        //AppWindow.MoveAndResize(new Windows.Graphics.RectInt32
        //{
        //    Height = height,
        //    Width = 340,
        //    X = x,
        //    Y = y
        //});

        // move to left x - (width - 400)
        // move to right x + (width - 400)

        //move to top y - (height - 400)
        //move to top y + (height - 400)

        nativeWindow.AppWindow.MoveAndResize(positionToSet);

        nativeWindow.AppWindow.MoveInZOrderAtTop();
    }


    public static bool DeleteSongFile(SongModelView song)
    {
        try
        {
            if (File.Exists(song.FilePath))
            {
                FileSystem.DeleteFile(song.FilePath, UIOption.AllDialogs, RecycleOption.SendToRecycleBin);

            }
            return true;
        }
        catch (UnauthorizedAccessException e)
        {
            Debug.WriteLine("Unauthorized to delete file: " + e.Message);
            return false;
        }
        catch (IOException e)
        {
            Debug.WriteLine("An IO exception occurred: " + e.Message);
            return false;
        }
        catch (Exception e)
        {
            Debug.WriteLine("An error occurred: " + e.Message);
            return false;
        }
    }

    public static bool MultiDeleteSongFiles(ObservableCollection<SongModelView> songs)
    {
        try
        {
            foreach (var song in songs.Where(song => File.Exists(song.FilePath)))
            {
                FileSystem.DeleteFile(song.FilePath, UIOption.AllDialogs, RecycleOption.SendToRecycleBin);
            }

            return true;
        }
        catch (UnauthorizedAccessException e)
        {
            Debug.WriteLine("Unauthorized to delete file: " + e.Message);
            return false;
        }
        catch (IOException e)
        {
            Debug.WriteLine("An IO exception occurred: " + e.Message);
            return false;
        }
        catch (Exception e)
        {
            Debug.WriteLine("An error occurred: " + e.Message);
            return false;
        }
    }
    // Helper to retrieve a valid window handle from your main window
    public static IntPtr GetWindowHandle()
    {
        var window = IPlatformApplication.Current!.Services.GetService<DimmerWin>()!;


        DimmerHandle = WindowNative.GetWindowHandle(window);
        return DimmerHandle;
    }

    public static async Task EnsureWindowReadyAsync(Microsoft.Maui.Controls.Window? MauiWindow = null)
    {
        if (MauiWindow == null)
        {       // Ensure there’s at least one window created by MAUI
            MauiWindow = Application.Current?.Windows.FirstOrDefault();
        }
        int attempts = 0;
        while ((MauiWindow?.Handler == null) && attempts++ < 20)
        {
            await Task.Delay(100);
            MauiWindow = Application.Current?.Windows.FirstOrDefault();
        }

        if (MauiWindow?.Handler == null)
            throw new InvalidOperationException("Window handler was not ready after waiting.");
    }

    public static Compositor GetCompositor(Microsoft.Maui.Controls.Window? MauiWindow = null)
    {
        var nativeWindow = GetNativeWindowFromMAUIWindow(MauiWindow);

        return nativeWindow.Compositor;
    }

    public static Compositor MainWindowCompositor => GetCompositor();
    public static Microsoft.UI.Xaml.Window GetNativeWindowFromMAUIWindow(Microsoft.Maui.Controls.Window? MauiWindow = null)
    {
        if (MauiWindow == null)
        {       // Ensure there’s at least one window created by MAUI
            MauiWindow = Application.Current?.Windows.FirstOrDefault();
        }
        if (MauiWindow == null)
            throw new InvalidOperationException("No MAUI window available yet.");



        var nativeWindow = MauiWindow.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
        if (nativeWindow == null)
            throw new InvalidOperationException("Unable to retrieve native window.");

        return nativeWindow;
    }

    public static Microsoft.UI.Xaml.Controls.Frame? GetNativeFrame(this IElement element)
    {
        if (element.Handler?.PlatformView is Microsoft.UI.Xaml.FrameworkElement fe)
        {
            Microsoft.UI.Xaml.DependencyObject? parent = fe;
            while (parent != null)
            {
                if (parent is Microsoft.UI.Xaml.Controls.Frame frame)
                    return frame;

                parent = VisualTreeHelper.GetParent(parent);
            }
        }
        return null;
    }
    public static nint GetHWIdnInt(Microsoft.UI.Xaml.Window win)
    {
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(win);
        return hwnd;
    }
    public static string GetHWId(Microsoft.UI.Xaml.Window win)

    {
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(win);
        return hwnd.ToInt64().ToString();
    }

    public static AppWindow GetAppWindow(Microsoft.UI.Xaml.Window win)
    {
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(win);
        var id = Win32Interop.GetWindowIdFromWindow(hwnd);
        return AppWindow.GetFromWindowId(id);
    }
    public static IntPtr GetAnyWindowHandle(Microsoft.Maui.Controls.Window window)
    {

        if (window == null)
            throw new ArgumentNullException(nameof(window));
        // Get the underlying native window (WinUI).
        var nativeWindow = window.Handler?.PlatformView as Microsoft.UI.Xaml.Window ?? throw new InvalidOperationException("Unable to retrieve the native window.");

        var intPtrHandle = WindowNative.GetWindowHandle(nativeWindow);

        return intPtrHandle;
    }

    [DllImport("user32.dll")]
#pragma warning disable S4200 // Native methods should be wrapped
#pragma warning disable SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
#pragma warning disable CA1401 // P/Invokes should not be visible
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
#pragma warning restore CA1401 // P/Invokes should not be visible
#pragma warning restore SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
#pragma warning restore S4200 // Native methods should be wrapped

    public const int SW_HIDE = 0;
    public const int SW_RESTORE = 9;
    public static class ApplicationProps
    {
        public static DisplayArea? DisplayArea { get; set; }



        public async static Task LaunchNotificationWindowAndFadeItAwayAfterSixSeconds(BaseViewModelWin vm)
        {

            //SongNotifierWindow newNotif = new SongNotifierWindow(vm);

            //newNotif.Height = 300;
            //newNotif.Width = AppUtils.UserScreenWidth;

            //Application.Current?.OpenWindow(newNotif);


            //await Task.Delay(6000);


            //Application.Current?.CloseWindow(newNotif);


        }

    }

    public static void OpenAllSongsWindow(BaseViewModelWin vm)
    {
        var winMgr = IPlatformApplication.Current!.Services.GetService<IWinUIWindowMgrService>()!;

        //AllSongsWindow? win = winMgr.GetOrCreateUniqueWindow(vm, windowFactory: () => new AllSongsWindow(vm));

        // move and resize to the center of the screen

        //var pres = win?.AppWindow.Presenter;

        ////window.SetTitleBar()
        //if (pres is OverlappedPresenter p)
        //{
        //    //p.PreferredMaximumHeight = 1200;
        //    //p.PreferredMaximumWidth = 720;
        //    //p.PreferredMinimumWidth = 600;
        //    //p.PreferredMinimumHeight = 800;
        //    p.IsResizable = true;
        //    p.SetBorderAndTitleBar(true, true); // Remove title bar and border
        //    p.IsAlwaysOnTop = false;

        //}

    }

    public static async Task ShowNewSongNotification(string songTitle, string artistName, string albumArtPath)
    {
        try
        {

            var notificationBuilder = new AppNotificationBuilder()
                .AddArgument("action", "viewSong")
                //.AddArgument("songId", "12345")
                .SetAppLogoOverride(new Uri(albumArtPath), AppNotificationImageCrop.Circle, songTitle)
                .AddText("Now Playing...", new AppNotificationTextProperties().SetMaxLines(2))
                .AddText(songTitle, new AppNotificationTextProperties().SetMaxLines(2))

                .AddText(artistName, new AppNotificationTextProperties().SetMaxLines(2));

            //.AddButton(new AppNotificationButton("View Song Details").AddArgument("action", "play"))
            //.AddButton(new AppNotificationButton("Queue").AddArgument("action", "queue")


            await AppNotificationManager.Default.RemoveAllAsync();
            var notif = notificationBuilder.BuildNotification();

            AppNotificationManager.Default.Show(notif);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }

    public static void ShowContextMenu(this Element element)
    {
        // Get the MenuFlyout attached to the MAUI element
        var menuFlyout = Microsoft.Maui.Controls.FlyoutBase.GetContextFlyout(element);
        if (menuFlyout == null)
        {
            // No context menu to show
            return;
        }

        // Use platform-specific code to trigger the flyout
        var platformView = element.Handler?.PlatformView as Microsoft.UI.Xaml.FrameworkElement;
        if (platformView != null)
        {
            var flyoutMenu = FlyoutBase.GetAttachedFlyout(platformView);
            if (flyoutMenu is null) return;

            // The native way to show a context flyout on WinUI
            FlyoutBase.ShowAttachedFlyout(platformView);
        }
    }


    public static T? FindVisualChild<T>(DependencyObject? parent, string? childName) where T : FrameworkElement
    {
        if (parent == null)
        {
            return null;
        }

        int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < childrenCount; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);

            // Check if the current child is the target control.
            if (child is T frameworkElement && frameworkElement.Name == childName)
            {
                return frameworkElement;
            }

            // If not, recursively search in the children of the current child.
            T? childOfChild = FindVisualChild<T>(child, childName);
            if (childOfChild != null)
            {
                return childOfChild;
            }
        }
        return null;
    }
    public static T? FindChildOfType<T>(DependencyObject parent, string name) where T : FrameworkElement
    {
        if (parent == null)
            return null;

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);

            if (child is T fe && fe.Name == name)
                return fe;

            var result = FindChildOfType<T>(child, name);
            if (result != null)
                return result;
        }

        return null;
    }
    /// <summary>
    /// Finds a child control of a specific type within the visual tree of a parent element.
    /// </summary>
    /// <typeparam name="T">The type of the child control to find.</typeparam>
    /// <param name="parent">The parent element to search within.</param>
    /// <returns>The first child control of the specified type, or null if not found.</returns>
    public static T? FindChildOfType<T>(DependencyObject parent) where T : DependencyObject
    {
        if (parent == null)
            return null;

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);

            if (child is T typedChild)
            {
                return typedChild;
            }

            var result = FindChildOfType<T>(child);
            if (result != null)
            {
                return result;
            }
        }
        return null;
    }

    public static void AnimateHoverUIElement(Microsoft.UI.Xaml.UIElement element, bool isHover, Microsoft.UI.Composition.Compositor _compositor)
    {

        var visual = ElementCompositionPreview.GetElementVisual(element);

        // scale up / down
        var scaleAnim = _compositor.CreateVector3KeyFrameAnimation();
        scaleAnim.Duration = TimeSpan.FromMilliseconds(250);
        scaleAnim.InsertKeyFrame(1f, isHover
            ? new System.Numerics.Vector3(1.1f)
            : new System.Numerics.Vector3(1f));

        // keep scale centered
        visual.CenterPoint = new System.Numerics.Vector3(
            (float)element.RenderSize.Width / 2,
            (float)element.RenderSize.Height / 2,
            0);

        visual.StartAnimation(nameof(visual.Scale), scaleAnim);

        // OPTIONAL: subtle fade instead of full vanish
        var opacityAnim = _compositor.CreateScalarKeyFrameAnimation();
        opacityAnim.Duration = TimeSpan.FromMilliseconds(250);
        opacityAnim.InsertKeyFrame(1f, isHover ? 1f : 0.85f);   // not 0
        visual.StartAnimation(nameof(visual.Opacity), opacityAnim);


    }
    public static async Task ApplyExitEffectAsync(FrameworkElement frameElt, Compositor _compositor, ExitTransitionEffect exitAnim = ExitTransitionEffect.FadeSlideDown)
    {
        var visual = ElementCompositionPreview.GetElementVisual(frameElt);
        var duration = TimeSpan.FromMilliseconds(400);
        // 1. Create a "Batch" to track when animations finish
        var batch = _compositor.CreateScopedBatch(CompositionBatchTypes.Animation);

        // Ensure the center point is set correctly for Scale/Rotation animations
        visual.CenterPoint = new Vector3((float)frameElt.ActualWidth / 2f,
                                         (float)frameElt.ActualHeight / 2f, 0);

        // Standard linear easing for simple fades, Cubic for movement
        var cubicBezier = _compositor.CreateCubicBezierEasingFunction(new Vector2(0.5f, 0.0f), new Vector2(0.5f, 1.0f));

        switch (exitAnim)
        {
            // 1. THE REQUESTED EFFECT: Fade Out + Slide Down
            case ExitTransitionEffect.FadeSlideDown:
            default:
                // Opacity: 1 -> 0
                var fadeDown = _compositor.CreateScalarKeyFrameAnimation();
                fadeDown.InsertKeyFrame(1f, 0f);
                fadeDown.Duration = duration;

                // Offset: (0,0) -> (0, 50)
                var slideDown = _compositor.CreateVector3KeyFrameAnimation();
                slideDown.InsertKeyFrame(1f, new Vector3(0, 50f, 0), cubicBezier);
                slideDown.Duration = duration;

                visual.StartAnimation("Opacity", fadeDown);
                visual.StartAnimation("Offset", slideDown);
                break;

            // 2. Zoom Out (Scale down to nothing)
            case ExitTransitionEffect.ZoomOut:
                var zoom = _compositor.CreateVector3KeyFrameAnimation();
                zoom.InsertKeyFrame(1f, new Vector3(0f)); // Scale to 0
                zoom.Duration = duration;

                var fadeZoom = _compositor.CreateScalarKeyFrameAnimation();
                fadeZoom.InsertKeyFrame(1f, 0f);
                fadeZoom.Duration = duration;

                visual.StartAnimation("Scale", zoom);
                visual.StartAnimation("Opacity", fadeZoom);
                break;

            // 3. Slide Right
            case ExitTransitionEffect.SlideRight:
                var slideR = _compositor.CreateVector3KeyFrameAnimation();
                // Move right by width + buffer
                slideR.InsertKeyFrame(1f, new Vector3((float)frameElt.ActualWidth + 50f, 0, 0), cubicBezier);
                slideR.Duration = duration;

                visual.StartAnimation("Offset", slideR);
                break;

            // 4. Slide Left
            case ExitTransitionEffect.SlideLeft:
                var slideL = _compositor.CreateVector3KeyFrameAnimation();
                slideL.InsertKeyFrame(1f, new Vector3(-((float)frameElt.ActualWidth + 50f), 0, 0), cubicBezier);
                slideL.Duration = duration;

                visual.StartAnimation("Offset", slideL);
                break;

            // 5. Fly Up (Fade out while moving up)
            case ExitTransitionEffect.FlyUp:
                var flyUp = _compositor.CreateVector3KeyFrameAnimation();
                flyUp.InsertKeyFrame(1f, new Vector3(0, -50f, 0), cubicBezier);
                flyUp.Duration = duration;

                var fadeUp = _compositor.CreateScalarKeyFrameAnimation();
                fadeUp.InsertKeyFrame(1f, 0f);
                fadeUp.Duration = duration;

                visual.StartAnimation("Offset", flyUp);
                visual.StartAnimation("Opacity", fadeUp);
                break;

            // 6. Spin and Shrink (Whirlpool effect)
            case ExitTransitionEffect.SpinAndShrink:
                var spin = _compositor.CreateScalarKeyFrameAnimation();
                spin.InsertKeyFrame(1f, 360f, cubicBezier); // Rotate full circle
                spin.Duration = duration;

                var shrink = _compositor.CreateVector3KeyFrameAnimation();
                shrink.InsertKeyFrame(1f, Vector3.Zero);
                shrink.Duration = duration;

                visual.StartAnimation("RotationAngleInDegrees", spin);
                visual.StartAnimation("Scale", shrink);
                break;

            // 7. Flip Horizontal (Rotates along Y axis until invisible)
            case ExitTransitionEffect.FlipHorizontal:
                var flip = _compositor.CreateScalarKeyFrameAnimation();
                // Rotate 90 degrees (edge-on to viewer, effectively invisible)
                flip.InsertKeyFrame(1f, 90f, cubicBezier);
                flip.Duration = duration;

                // Set the Axis of rotation to Y
                visual.RotationAxis = new Vector3(0, 1, 0);
                visual.StartAnimation("RotationAngleInDegrees", flip);

                // Helpful to fade it slightly at the end to prevent "clipping" artifacts
                var fadeFlip = _compositor.CreateScalarKeyFrameAnimation();
                fadeFlip.InsertKeyFrame(0.5f, 1f);
                fadeFlip.InsertKeyFrame(1f, 0f);
                fadeFlip.Duration = duration;
                visual.StartAnimation("Opacity", fadeFlip);
                break;

            // 8. Fold Vertical (Squash Y scale)
            case ExitTransitionEffect.FoldVertical:
                var fold = _compositor.CreateVector3KeyFrameAnimation();
                // Keep X scale at 1, squash Y to 0
                fold.InsertKeyFrame(1f, new Vector3(1f, 0f, 1f), cubicBezier);
                fold.Duration = duration;

                visual.StartAnimation("Scale", fold);
                break;

            // 9. Explode (Scale up + Fade out)
            case ExitTransitionEffect.Explode:
                var explodeScale = _compositor.CreateVector3KeyFrameAnimation();
                explodeScale.InsertKeyFrame(1f, new Vector3(1.5f), cubicBezier); // Grow 1.5x
                explodeScale.Duration = duration;

                var explodeFade = _compositor.CreateScalarKeyFrameAnimation();
                explodeFade.InsertKeyFrame(0f, 1f);
                explodeFade.InsertKeyFrame(1f, 0f); // Disappear
                explodeFade.Duration = duration;

                visual.StartAnimation("Scale", explodeScale);
                visual.StartAnimation("Opacity", explodeFade);
                break;
        }

        // 2. End the batch definition
        batch.End();

        // 3. Wait for the batch to complete asynchronously
        var tcs = new TaskCompletionSource<bool>();
        batch.Completed += (s, e) => tcs.SetResult(true);
        await tcs.Task;

        // 4. CLEANUP: Now that animation is done, hide the XAML element
        frameElt.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;

        // 5. RESET: Reset visuals so if you set Visibility.Visible later, it looks normal
        ResetElementVisual(frameElt);
    }
    private static void ResetElementVisual(FrameworkElement frameElt)
    {
        var visual = ElementCompositionPreview.GetElementVisual(frameElt);

        // Reset all transform properties to defaults
        visual.Opacity = 1f;
        visual.Offset = Vector3.Zero;
        visual.Scale = Vector3.One;
        visual.RotationAngleInDegrees = 0f;
        visual.RotationAxis = new Vector3(0, 0, 1); // Default Z-axis
    }
    internal static void ApplyEntranceEffect(Microsoft.UI.Composition.Visual visual, FrameworkElement element, SongTransitionAnimation _userPrefAnim, Microsoft.UI.Composition.Compositor _compositor)
    {
        visual.CenterPoint = new Vector3(
            (float)element.ActualWidth / 2f,
            (float)element.ActualHeight / 2f,
            0f);

        switch (_userPrefAnim)
        {
            case SongTransitionAnimation.Fade:
                visual.Opacity = 0f;
                var fade = _compositor.CreateScalarKeyFrameAnimation();
                fade.InsertKeyFrame(1f, 1f);
                fade.Duration = TimeSpan.FromMilliseconds(350);
                visual.StartAnimation("Opacity", fade);
                break;

            case SongTransitionAnimation.Scale:
                visual.Scale = new Vector3(0.85f);
                var scale = _compositor.CreateVector3KeyFrameAnimation();
                scale.InsertKeyFrame(1f, Vector3.One);
                scale.Duration = TimeSpan.FromMilliseconds(350);
                visual.StartAnimation("Scale", scale);
                break;

            case SongTransitionAnimation.Slide:
                visual.Offset = new Vector3(60f, 0f, 0f);
                var slide = _compositor.CreateVector3KeyFrameAnimation();
                slide.InsertKeyFrame(1f, Vector3.Zero);
                slide.Duration = TimeSpan.FromMilliseconds(350);
                visual.StartAnimation("Offset", slide);
                break;

            case SongTransitionAnimation.Spring:
            default:

                var original = visual.Offset;


                visual.Offset = new Vector3(original.X, original.Y + 40f, 0f);

                var spring = _compositor.CreateSpringVector3Animation();
                spring.FinalValue = original;
                spring.DampingRatio = 0.55f;
                spring.Period = TimeSpan.FromMilliseconds(350);

                visual.StartAnimation("Offset", spring);
                break;
        }
    }

    internal static bool IsElementInView(Microsoft.Maui.Controls.ScrollView mainScrollView, Microsoft.Maui.Controls.Grid coverImageSection)
    {
        var checkIfInView = mainScrollView.Bounds;
        var elementBounds = coverImageSection.Bounds;
        return (elementBounds.Top >= checkIfInView.Top) && (elementBounds.Bottom <= checkIfInView.Bottom);

    }
}
