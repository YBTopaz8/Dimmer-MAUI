

using Dimmer.WinUI.ViewModel.DimmerLiveWin;
using Dimmer.WinUI.Views.WinuiPages.Achievements;
using Dimmer.WinUI.Views.WinuiPages.DimmerLive;
using Dimmer.WinUI.Views.WinuiPages.LastFMSection;
using Dimmer.WinUI.Views.WinuiPages.SingleSongPage;

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
        builder.Services.AddDimmerCoreServices();


        builder.Services.AddSingleton<IDimmerAudioService, AudioService>();
        builder.Services.AddSingleton<BaseViewModelWin>();
        builder.Services.AddSingleton<IMauiWindowManagerService, WindowManagerService>();
        builder.Services.AddSingleton<EditorViewModel>();
        builder.Services.AddSingleton<IWinUIWindowMgrService, WinUIWindowMgrService>();
        builder.Services.AddSingleton<IDimmerAudioEditorService, WindowsAudioEditorService>();
        
        // Register Windows-specific Song Story Share Service
        builder.Services.AddSingleton<ISongStoryShareService, NativeServices.WindowsSongStoryShareService>();

        builder.Services.AddSingleton<DimmerWin>();



        builder.Services.AddScoped<IAppUtil, AppUtil>();
        builder.Services.AddSingleton<IUiErrorPresenter, WinUiErrorPresenter>();

        builder.ConfigureLifecycleEvents(events =>
        {
            events.AddWindows(wndLifeCycleBuilder =>
            {
                wndLifeCycleBuilder.OnClosed((window, args) =>
                {

                    //var winMgr = IPlatformApplication.Current!.Services.GetService<IWinUIWindowMgrService>();
                    //winMgr?.CloseAllWindows(window);

                    // Handle window closed event
                    // You can perform cleanup or save state here if needed
                });
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
                                PlatUtils.IsAppInForeground = s.IsVisible;
                            }

                        };

                        PlatUtils.AppWinPresenter = winuiAppWindow.Presenter;
                        PlatUtils.OverLappedPres = winuiAppWindow.Presenter as OverlappedPresenter;


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


        builder.Services.AddSingleton<IAnimationService, WindowsAnimationService>();



        //IMapper? mapperWin = AutoMapperConfWinUI.ConfigureAutoMapper();

        builder.Services.AddSingleton<SocialViewModelWin>();
        builder.Services.AddSingleton<HomePage>();
        builder.Services.AddSingleton<AllSongsListPage>();
        builder.Services.AddSingleton<ArtistPage>();
        builder.Services.AddSingleton<DimmerMAUIWin>();
        builder.Services.AddSingleton<AlbumPage>();
        builder.Services.AddSingleton<SettingsPage>();
        builder.Services.AddSingleton<DimmerLivePage>();
        builder.Services.AddSingleton<SongDetailPage>();
        builder.Services.AddSingleton<EditSongPage>();
        builder.Services.AddSingleton<SettingsViewModel>();
        builder.Services.AddSingleton<ChatViewModelWin>();
        builder.Services.AddSingleton<TqlTutorialViewModel>();
        builder.Services.AddSingleton<StatsViewModelWin>();
        builder.Services.AddSingleton<GraphAndNodes>();

        builder.Services.AddSingleton<DimmerMultiWindowCoordinator>();
        
        builder.Services.AddSingleton<LoginPage>();
        builder.Services.AddSingleton<SocialPage>();
        builder.Services.AddSingleton<CloudDataPage>();
        builder.Services.AddSingleton<LoginViewModelWin>();
        builder.Services.AddSingleton<LastFmPage>();
        builder.Services.AddSingleton<GlobalAchievementsPage>();


        return builder.Build();
    }

  
}