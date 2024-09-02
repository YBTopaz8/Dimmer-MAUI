﻿#if ANDROID
using Dimmer_MAUI.DataAccess.IServices;
using Dimmer_MAUI.DataAccess.Services;
using Dimmer_MAUI.Platforms.Android.MAudioLib;
using Dimmer_MAUI.Utilities.IServices;
using Dimmer_MAUI.Utilities.Services;
using Microsoft.Maui.LifecycleEvents;
using PanCardView;

#elif WINDOWS
using Dimmer_MAUI.DataAccess.IServices;
using Dimmer_MAUI.DataAccess.Services;
using Dimmer_MAUI.Platforms.Windows;
using Dimmer_MAUI.Utilities.IServices;
using Dimmer_MAUI.Utilities.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using WinRT.Interop;
#endif


namespace Dimmer_MAUI;
public static class MauiProgram
{    
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseUraniumUI()
            .UseUraniumUIBlurs()
            .UseUraniumUIMaterial()
            .UseBottomSheet()
            .UseSegmentedControl()
            .ConfigureContextMenuContainer()
            .UseCardsView()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddMaterialIconFonts();
            });
        
#if WINDOWS || Debug
		builder.Logging.AddDebug();
#endif

#if WINDOWS        
        builder.ConfigureLifecycleEvents(events =>
        {
            events.AddWindows(wndLifeCycleBuilder =>
            {
                wndLifeCycleBuilder.OnWindowCreated(window =>
                {
                    IntPtr nativeWindowHandle = WindowNative.GetWindowHandle(window);
                    WindowId win32WindowsId = Win32Interop.GetWindowIdFromWindow(nativeWindowHandle);
                    AppWindow winuiAppWindow = AppWindow.GetFromWindowId(win32WindowsId);

                    // Check if this is the mini player window by checking its title or other identifying property
                    if (window.Title == "MP")
                    {
                        if (winuiAppWindow.Presenter is OverlappedPresenter p)
                        {                            
                            p.IsResizable = false;
                            p.IsAlwaysOnTop = true;
                            p.SetBorderAndTitleBar(false, false); // Remove title bar and border
                        }
                    }
                    else
                    {
                        
                        // Customizations for the main window, if needed
                    }
                });
            });
        });
#endif



#if ANDROID || WINDOWS
        builder.Services.AddSingleton<INativeAudioService, NativeAudioService>();

        builder.Services.AddSingleton(INativeAudioService => NativeAudioService.Current);
#endif

        builder.Services.AddSingleton(FolderPicker.Default);
        builder.Services.AddSingleton(FilePicker.Default);
        builder.Services.AddSingleton(FileSaver.Default);

        builder.Services.AddSingleton<NowPlayingSongPageBtmSheet>();
        builder.Services.AddTransient<SongMenuBtmSheet>();

        /* Registering the DataAccess Services */
        builder.Services.AddSingleton<IDataBaseService, DataBaseService>();
        builder.Services.AddSingleton<ISongsManagementService, SongsManagementService>();
        builder.Services.AddSingleton<IStatsManagementService, StatsManagementService>();
        builder.Services.AddSingleton<IPlaylistManagementService, PlayListManagementService>();
        builder.Services.AddSingleton<IArtistsManagementService, ArtistsManagementService>();

        /* Registering the Utilities services */
        builder.Services.AddSingleton<IPlaybackUtilsService, PlaybackUtilsService>();
        builder.Services.AddSingleton<ILyricsService, LyricsService>();

        /* Registering the ViewModels */
        builder.Services.AddSingleton<HomePageVM>();
        
        
        /* Registering the Desktop Views */
        builder.Services.AddSingleton<HomeD>();
        builder.Services.AddSingleton<NowPlayingD>();
        builder.Services.AddSingleton<PlaylistsPageD>();
        builder.Services.AddSingleton<ArtistsPageD>();

        /* Registering the Mobile Views */
        builder.Services.AddSingleton<HomePageM>();
        builder.Services.AddSingleton<SingleSongShell>();
        builder.Services.AddSingleton<PlaylistsPageM>();
        builder.Services.AddSingleton<SinglePlaylistPageM>();

        return builder.Build();
    }
}
