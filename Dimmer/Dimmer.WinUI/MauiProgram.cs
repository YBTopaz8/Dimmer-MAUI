



using Windows.Graphics;

using Colors = Microsoft.Maui.Graphics.Colors;

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
                //Microsoft.Maui.Handlers.LabelHandler.Mapper.AppendToMapping(
                //    nameof(FlyoutBase.ContextFlyoutProperty), (handler, view) =>
                //{
                //    if (handler.PlatformView is Microsoft.UI.Xaml.FrameworkElement nativeView && view is Element element)
                //    {
                //        var contextFlyout = FlyoutBase.GetContextFlyout(element);
                //        if (contextFlyout?.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.Primitives.FlyoutBase nativeFlyout)
                //        {
                //            Microsoft.UI.Xaml.Controls.Primitives.FlyoutBase.SetAttachedFlyout(nativeView, nativeFlyout);
                //        }
                //    }
                //});


                Microsoft.Maui.Handlers.ButtonHandler.Mapper.AppendToMapping(
                         key: "AddGlobalTouchBehavior",
                         method: (handler, view) =>
                         {
                             return;
                                 
                             // The 'view' is the cross-platform Button control.
                             if (view is Button button)
                             {

                                 // --- PREVENT DUPLICATES ---
                                 // This is an important check to ensure we don't add the behavior
                                 // multiple times if the handler's logic re-runs for the same control.
                                 if (button.Behaviors.OfType<TouchBehavior>().Any() || button.Behaviors.OfType<IconTintColorBehavior>().Any())
                                 {
                                     return;
                                 }
                                 var iconTintBehavior = new CommunityToolkit.Maui.Behaviors.IconTintColorBehavior
                                 {
                                     // Set the initial/unhovered color. Let's use the button's TextColor for flexibility.
                                     TintColor = Colors.DarkSlateBlue
                                     
                                 };


                                 var touchBehavior = new TouchBehavior
                                 {
                                     HoveredAnimationDuration = 350,
                                     HoveredAnimationEasing = Easing.Linear,
                                     HoveredOpacity=0.8,
                                     PressedAnimationDuration = 300,
                                     // Add any other customizations here

                                 };

                                 touchBehavior.HoverStateChanged += (sender, e) =>
                                 {
                                     // Here we define the desired visual state changes.
                                     switch (e.State)
                                     {
                                         case CommunityToolkit.Maui.Core.HoverState.Hovered:
                                             var bev =button.Behaviors.FirstOrDefault(x=>x.GetType()== typeof(IconTintColorBehavior));
                                                if (bev is null) return;
                                                var iconTintBehavior = (CommunityToolkit.Maui.Behaviors.IconTintColorBehavior)bev;
                                             // The 'sender' of this event is the TouchBehavior itself.
                                             iconTintBehavior.TintColor= Colors.DarkSlateBlue;

                                             button.BorderWidth = 1;
                                             button.BorderColor = Colors.DarkSlateBlue;
                                             break;

                                         case CommunityToolkit.Maui.Core.HoverState.Default:
                                         default:




                                             // e.g., button.BackgroundColor = Colors.DarkSlateBlue;
                                             break;
                                     }
                                 };


                                 button.Behaviors.Add(touchBehavior);
                                 button.Behaviors.Add(iconTintBehavior);
                             }
                         });


                 });


        builder.Services.AddSingleton<IDimmerAudioService, AudioService>();
        builder.Services.AddSingleton<BaseViewModelWin>();
        builder.Services.AddTransient<AlbumWindow>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<AllArtistsPage>();
        builder.Services.AddTransient<SingleAlbumPage>();
        builder.Services.AddTransient<AllAlbumsPage>();
        builder.Services.AddSingleton<ArtistsPage>();
        builder.Services.AddTransient<OnlinePageManagement>();
        builder.Services.AddSingleton<IMauiWindowManagerService, WindowManagerService>();
        builder.Services.AddSingleton<IWinUIWindowMgrService, WinUIWindowMgrService>();

        builder.Services.AddSingleton<DimmerWin>();

        builder.Services.AddSingleton<LibSanityPage>();
        builder.Services.AddSingleton<HomePage>();

        builder.Services.AddSingleton<SingleSongPage>();
        builder.Services.AddSingleton<DimmerLivePage>();
        builder.Services.AddSingleton<ExperimentsPage>();
        builder.Services.AddSingleton<SessionTransferVMWin>();

        builder.Services.AddSingleton<DuplicatesMgtWindow>();


        builder.Services.AddScoped<IAppUtil, AppUtil>();

        builder.ConfigureLifecycleEvents(events =>
        {
            events.AddWindows(wndLifeCycleBuilder =>
            {
                wndLifeCycleBuilder.OnClosed((window, args) =>
                {
                    
                 
                    var winMgr = IPlatformApplication.Current.Services.GetService<IWinUIWindowMgrService>();
                    winMgr?.CloseAllWindows();

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


        builder.Services.AddSingleton<IAnimationService,WindowsAnimationService>();



        //IMapper? mapperWin = AutoMapperConfWinUI.ConfigureAutoMapper();
        builder.Services.AddSingleton<ChatView>();
        builder.Services.AddSingleton<SessionTransferView>();
        builder.Services.AddSingleton<SocialView>();
        builder.Services.AddSingleton<SocialViewModelWin>();
        builder.Services.AddSingleton<ChatViewModelWin>();
        builder.Services.AddSingleton<SongDetailPage>();
        builder.Services.AddSingleton<AllPlaylists>();
        builder.Services.AddSingleton<TqlTutorialViewModel>();
        builder.Services.AddSingleton<TqlTutorialPage>();
        builder.Services.AddSingleton<SingleAlbumPage>();
        builder.Services.AddSingleton<WelcomePage>();
        builder.Services.AddSingleton<DimmerMultiWindowCoordinator>();
        builder.Services.AddSingleton<ControlPanelWindow>();

        builder.Services.AddSingleton<SyncLyricsPopUpView>();
        return builder.Build();
    }

    private static void TouchBehavior_HoverStateChanged(object? sender, CommunityToolkit.Maui.Core.HoverStateChangedEventArgs e)
    {
        // The 'sender' of this event is the TouchBehavior itself.
        // We need to get the Button it is attached to.
        var touchBehavior = (CommunityToolkit.Maui.Behaviors.TouchBehavior)sender;
        //var button = (Button)touchBehavior.p; // Get the Button

        // Find the IconTintColorBehavior that is already attached to the button.
        // The easiest way is to use the x:Name we gave it in XAML, if the handler is in the same class.
        // Or, more robustly, find it in the Behaviors collection.
        //var iconTintBehavior = button.Behaviors.OfType<CommunityToolkit.Maui.Behaviors.IconTintColorBehavior>().FirstOrDefault();

        //if (iconTintBehavior is null)
        //{
        //    // This should not happen if you set it up in XAML
        //    return;
        //}

        //// Now, just change the TintColor property based on the hover state
        //switch (e.State)
        //{
        //    case CommunityToolkit.Maui.Core.HoverState.Hovered:
        //        // When hovered, change the tint color to White
        //        iconTintBehavior.TintColor = Microsoft.Maui.Graphics.Colors.White;
        //        // You might also want to change the button's background
        //        button.BackgroundColor = Microsoft.Maui.Graphics.Colors.RoyalBlue; // Example highlight color
        //        break;

        //    case CommunityToolkit.Maui.Core.HoverState.Default:
        //    default:
        //        // When not hovered, change it back to the original color
        //        iconTintBehavior.TintColor = Microsoft.Maui.Graphics.Colors.DarkSlateBlue;
        //        button.BackgroundColor = Microsoft.Maui.Graphics.Colors.DarkSlateBlue; // Back to original
        //        break;
        //}
    }
}
