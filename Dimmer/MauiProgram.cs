namespace Dimmer_MAUI;
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseDevExpress(useLocalization: false)
            .UseDevExpressCollectionView()
            .UseDevExpressControls()
            .UseDevExpressDataGrid()
            .UseDevExpressEditors()
            .UseDevExpressGauges()

            .UseMauiCommunityToolkit(options =>
            {
                options.SetShouldSuppressExceptionsInAnimations(true);
                options.SetShouldSuppressExceptionsInBehaviors(true);
                options.SetShouldSuppressExceptionsInConverters(true);

            })

            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("FA6Brands-Regular-400.otf", "FABrands");


            })

            .ConfigureSyncfusionToolkit();


#if WINDOWS || Debug
        builder.Logging.AddDebug();
#endif

#if WINDOWS
        builder.Services.AddSingleton(INativeAudioService => NativeAudioService.Current);

        builder.ConfigureLifecycleEvents(events =>
        {
            events.AddWindows(wndLifeCycleBuilder =>
            {
                wndLifeCycleBuilder.OnWindowCreated(async window =>
                {
                    HomePageVM homeVM = IPlatformApplication.Current!.Services.GetService<HomePageVM>()!;

                    IntPtr nativeWindowHandle = WindowNative.GetWindowHandle(window);
                    if (nativeWindowHandle != IntPtr.Zero)
                    {

                        WindowId win32WindowsId = Win32Interop.GetWindowIdFromWindow(nativeWindowHandle);
                        AppWindow winuiAppWindow = AppWindow.GetFromWindowId(win32WindowsId);

                        if (winuiAppWindow.Title != "MP")
                        {
                            homeVM.AppWinPresenter = winuiAppWindow.Presenter;
                            var OLP = winuiAppWindow.Presenter as OverlappedPresenter;


                            //winuiAppWindow.Title = new CustomTitleBar(homeVM);


                            //OLP.SetBorderAndTitleBar(false, false);
                            winuiAppWindow.Closing += async (s, e) =>
                            {

                                e.Cancel = true;
                                var allWins = Application.Current!.Windows.ToList<Window>();
                                foreach (var win in allWins)
                                {
                                    if (win.Title != "MyWin")
                                    {
                                        await homeVM.ExitingApp();

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
                            if (winuiAppWindow.Presenter is OverlappedPresenter p)
                            {

                                p.IsResizable = false;
                                p.SetBorderAndTitleBar(false, false); // Remove title bar and border
                                p.IsAlwaysOnTop = true;
                            }
                            window.Activate();
                        }
                    }
                });
            });
        });
#endif

#if ANDROID
        builder.Services.AddSingleton<MediaPlayerService>(); // Register as singleton
        builder.Services.AddSingleton(INativeAudioService => NativeAudioService.Current);
#endif

#if ANDROID || WINDOWS
        builder.Services.AddSingleton<INativeAudioService, NativeAudioService>();
        builder.Services.AddSingleton<DimmerWindow>();

#endif
        builder.Services.AddSingleton(FolderPicker.Default);
        //builder.Services.AddSingleton(FilePicker.Default);
        builder.Services.AddSingleton(FileSaver.Default);

        builder.Services.AddSingleton<EachPageNPFAB_Mobile>();
        builder.Services.AddTransient<NowPlayingBtmSheet>(); //btmsheet
        //builder.Services.AddSingleton<SongMenuBtmSheet>();

        /* Registering the DataAccess Services */
        builder.Services.AddSingleton<IDataBaseService, DataBaseService>();
        builder.Services.AddSingleton<ISongsManagementService, SongsManagementService>();
        builder.Services.AddSingleton<IStatsManagementService, StatsManagementService>();
        builder.Services.AddSingleton<IPlaylistManagementService, PlayListManagementService>();


        /* Registering the Utilities services */
        builder.Services.AddSingleton<IPlaybackUtilsService, PlaybackUtilsService>();
        builder.Services.AddSingleton<ILyricsService, LyricsService>();

        /* Registering the ViewModels */
        builder.Services.AddSingleton(provider =>
        new Lazy<HomePageVM>(() => provider.GetRequiredService<HomePageVM>()));

        builder.Services.AddSingleton<HomePageVM>();


        /* Registering the Desktop Views */
        builder.Services.AddSingleton<MainPageD>();
        builder.Services.AddSingleton<SingleSongShellPageD>();
        builder.Services.AddSingleton<PlaylistsPageD>();
        builder.Services.AddSingleton<ArtistsPageD>();
        builder.Services.AddSingleton<FullStatsPageD>();
        builder.Services.AddSingleton<SingleSongStatsPageD>();
        builder.Services.AddSingleton<SettingsPageD>();
        builder.Services.AddSingleton<LandingPageD>();
        builder.Services.AddSingleton<AlbumsPageD>();
        
        builder.Services.AddSingleton<MediaPlaybackControlsView>();


        /* Registering the Mobile Views */
        builder.Services.AddSingleton<HomePageM>();
        builder.Services.AddSingleton<SingleSongShell>();
        builder.Services.AddSingleton<PlaylistsPageM>();
        builder.Services.AddSingleton<SinglePlaylistPageM>();
        builder.Services.AddSingleton<TopStatsPageM>();
        builder.Services.AddSingleton<SingleSongStatsPageM>();
        builder.Services.AddSingleton<ArtistsPageM>();
        builder.Services.AddSingleton<AlbumsM>();
        builder.Services.AddSingleton<SpecificAlbumPage>();
        builder.Services.AddSingleton<AlbumPageM>();
        builder.Services.AddTransient<ShareSongPage>();
        builder.Services.AddSingleton<SettingsPageM>();
        builder.Services.AddSingleton<FirstStepPage>();
        //builder.Services.AddSingleton<NowPlayingPage>();


        return builder.Build();
    }

}
