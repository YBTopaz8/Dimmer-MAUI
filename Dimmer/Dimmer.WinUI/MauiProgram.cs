

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



            });


        builder.Services.AddSingleton<IDimmerAudioService, AudioService>();
        builder.Services.AddSingleton<BaseViewModelWin>();
        builder.Services.AddSingleton<IMauiWindowManagerService, WindowManagerService>();
        builder.Services.AddSingleton<IWinUIWindowMgrService, WinUIWindowMgrService>();

        builder.Services.AddTransient<DimmerWin>();

        builder.Services.AddSingleton<SessionTransferVMWin>();



        builder.Services.AddScoped<IAppUtil, AppUtil>();

        builder.ConfigureLifecycleEvents(events =>
        {
            events.AddWindows(wndLifeCycleBuilder =>
            {
                wndLifeCycleBuilder.OnClosed((window, args) =>
                {

                    var winMgr = IPlatformApplication.Current!.Services.GetService<IWinUIWindowMgrService>();
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
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddSingleton<SongDetailPage>();
        builder.Services.AddSingleton<ChatViewModelWin>();
        builder.Services.AddSingleton<TqlTutorialViewModel>();

        builder.Services.AddSingleton<DimmerMultiWindowCoordinator>();
        return builder.Build();
    }

    private static void TouchBehavior_HoverStateChanged(object? sender, CommunityToolkit.Maui.Core.HoverStateChangedEventArgs e)
    {
        // The 'sender' of this event is the TouchBehavior itself.
        // We need to get the Button it is attached to.
        //var touchBehavior = (CommunityToolkit.Maui.Behaviors.TouchBehavior)sender;
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