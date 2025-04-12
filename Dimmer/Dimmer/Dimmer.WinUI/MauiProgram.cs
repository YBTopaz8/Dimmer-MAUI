

using Dimmer.Utilities;
using Dimmer.WinUI.DimmerAudio;
using Dimmer.WinUI.Utils.StaticUtils;
using Microsoft.Maui.LifecycleEvents;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using System.Diagnostics;
using WinRT.Interop;

namespace Dimmer.WinUI;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseSharedMauiApp();
        builder.Services.AddSingleton<IDimmerAudioService, AudioService>();
        builder.Services.AddSingleton<DimmerWin>();
        
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<HomeViewModel>();

        builder.Services.AddTransient<SingleSongPageViewModel>();
        builder.Services.AddTransient<SingleSongPage>();


        builder.Services.AddScoped<IAppUtil, AppUtil>();

        builder.ConfigureLifecycleEvents(events =>
        {
            events.AddWindows(wndLifeCycleBuilder =>
            {
                wndLifeCycleBuilder.OnWindowCreated(window =>
                {
                    IntPtr nativeWindowHandle = WindowNative.GetWindowHandle(window);

                    if (nativeWindowHandle != IntPtr.Zero)
                    {
                        PlatUtils.DimmerHandle = nativeWindowHandle;
                        WindowId win32WindowsId = Win32Interop.GetWindowIdFromWindow(nativeWindowHandle);
                        AppWindow winuiAppWindow = AppWindow.GetFromWindowId(win32WindowsId);
                        winuiAppWindow.Changed += (s, e) =>
                        {
                            if (e.DidVisibilityChange)
                            {
                                PlatUtils.IsAppInForeground =s.IsVisible;
                            }
                        };
                        ApplicationProps.DisplayArea = DisplayArea.GetFromWindowId(win32WindowsId, DisplayAreaFallback.Primary);
                        
                        PlatUtils.AppWinPresenter = winuiAppWindow.Presenter;
                        PlatUtils.OverLappedPres= winuiAppWindow.Presenter as OverlappedPresenter;
                        
                        Debug.WriteLine(ApplicationProps.DisplayArea.IsPrimary);
                        Debug.WriteLine(ApplicationProps.DisplayArea.WorkArea.Width);
                        Debug.WriteLine(ApplicationProps.DisplayArea.WorkArea.Height);
                        Debug.WriteLine(ApplicationProps.DisplayArea.WorkArea.Y);
                        Debug.WriteLine(ApplicationProps.DisplayArea.WorkArea.X);
                        AppUtils.UserScreenWidth=ApplicationProps.DisplayArea.OuterBounds.Width;
                        AppUtils.UserScreenHeight=(ApplicationProps.DisplayArea.OuterBounds.Height);
                        Debug.WriteLine(ApplicationProps.DisplayArea.DisplayId.Value);
                        
                        winuiAppWindow.Closing += async (s, e) =>
                        {
                            
                        };
                    }
                    // Check if this is the mini player window by checking its title or other identifying property
                    if (window.Title == "MP")
                    {

                        //window.SetTitleBar()
                        if (PlatUtils.OverLappedPres is OverlappedPresenter p)
                        {

                            p.IsResizable = true;
                            p.SetBorderAndTitleBar(false, false); // Remove title bar and border
                            p.IsAlwaysOnTop = true;
                        }
                        window.Activate();
                    }
                });
            });
        });
        return builder.Build();
    }
}
