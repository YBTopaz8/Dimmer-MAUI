


using CommunityToolkit.Maui;

using Dimmer.Interfaces.Services.Interfaces;
using Dimmer.WinUI.Utils.CustomHandlers.CollectionView;
using Dimmer.WinUI.Utils.WinMgt;
using Dimmer.WinUI.Views.AlbumsPage;
using Dimmer.WinUI.Views.DimmerLiveUI;

using Parse.LiveQuery;

namespace Dimmer.WinUI;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseSharedMauiApp()
            .UseMauiCommunityToolkit(options =>
            {
                options.SetShouldSuppressExceptionsInAnimations(true);
                options.SetShouldSuppressExceptionsInBehaviors(true);
                options.SetShouldSuppressExceptionsInConverters(true);

            })
            .ConfigureMauiHandlers(handlers =>
            {
                handlers.AddHandler<CollectionView, CustomCollectionViewHandler>();
            });


        builder.Services.AddSingleton<IDimmerAudioService, AudioService>();
        builder.Services.AddScoped<BaseViewModelWin>();
        builder.Services.AddTransient<AlbumWindow>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<AllArtistsPage>();
        builder.Services.AddTransient<SingleAlbumPage>();
        builder.Services.AddTransient<AllAlbumsPage>();
        builder.Services.AddSingleton<ArtistsPage>();
        builder.Services.AddTransient<OnlinePageManagement>();
        builder.Services.AddSingleton<IWindowManagerService, WindowManagerService>();
        builder.Services.AddSingleton<IWinUIWindowMgrService, WinUIWindowMgrService>();

        builder.Services.AddSingleton<DimmerWin>();

        builder.Services.AddSingleton<LibSanityPage>();
        builder.Services.AddSingleton<HomePage>();

        builder.Services.AddSingleton<SingleSongPage>();
        builder.Services.AddSingleton<DimmerLivePage>();
        builder.Services.AddSingleton<ExperimentsPage>();
        builder.Services.AddSingleton<SessionTransferVMWin>();


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

                        PlatUtils.AppWinPresenter = winuiAppWindow.Presenter;
                        PlatUtils.OverLappedPres= winuiAppWindow.Presenter as OverlappedPresenter;


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






        //IMapper? mapperWin = AutoMapperConfWinUI.ConfigureAutoMapper();
        builder.Services.AddSingleton<ChatView>();
        builder.Services.AddSingleton<SessionTransferView>();
        builder.Services.AddSingleton<SocialView>();
        builder.Services.AddSingleton<SocialViewModelWin>();
        builder.Services.AddSingleton<ChatViewModelWin>();

        return builder.Build();
    }
}
