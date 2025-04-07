

using Dimmer.WinUI.Utils.StaticUtils;
using Microsoft.Maui.LifecycleEvents;
using Microsoft.UI;
using Microsoft.UI.Windowing;
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
        builder.Services.AddSingleton<MediaControlBtmBar>();
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<HomeViewModel>();
        builder.Services.AddTransient<SingleSongPage>();
        builder.Services.AddScoped<IAppUtil, AppUtil>();

        builder.ConfigureLifecycleEvents(events =>
        {
            events.AddWindows(wndLifeCycleBuilder =>
            {
                wndLifeCycleBuilder.OnWindowCreated(window =>
                {
                    //HomePageVM homeVM = IPlatformApplication.Current!.Services.GetService<HomePageVM>()!;

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


                        PlatUtils.AppWinPresenter = winuiAppWindow.Presenter;
                        PlatUtils.OverLappedPres= winuiAppWindow.Presenter as OverlappedPresenter;


                        //winuiAppWindow.Title = new CustomTitleBar(homeVM);


                        //OLP.SetBorderAndTitleBar(false, false);
                        winuiAppWindow.Closing += async (s, e) =>
                        {
                            if (!PlatUtils.ShowCloseConfirmationPopUp.ShowCloseConfirmation)
                            {
                                return;
                            }
                            e.Cancel = true;
                            var allWins = Application.Current!.Windows.ToList<Window>();
                            foreach (var win in allWins)
                            {
                                if (win.Title != "MyWin")
                                {
                                    //await homeVM.ExitingApp();

                                    bool result = await win!.Page!.DisplayAlert(
                                        "Confirm Action",
                                        "You sure want to close app?",
                                        "Yes",
                                        "Cancel");
                                    if (result)
                                    {

                                        Application.Current.CloseWindow(win);
                                        Application.Current.Quit();
                                        Environment.Exit(0); // Forcefully kill all threads

                                    }
                                }
                            }
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
