using CommunityToolkit.Maui;


using Microsoft.Maui.LifecycleEvents;

namespace Dimmer.Droid;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseSharedMauiApp();
        
        builder
              .ConfigureEssentials(essentials =>
              {
                  essentials
                      .AddAppAction("play_last_audio", "Play Last Audio", icon: "atom")
                      .AddAppAction("toggle_playback", "Play/Pause Audio", icon: "playpause")
                        .AddAppAction("next_audio", "Next Audio", icon: "next")
                        .AddAppAction("previous_audio", "Previous Audio", icon: "previous")

                      // Provide actual icon resource                      
                      //.AddAppAction("browse_audio", "Browse Audio Files", icon: "browse_action_icon")
                      //.AddAppAction("app_settings", "App Settings", subtitle: "Configure preferences")
                      .OnAppAction(MainApplication.HandleAppAction);

              })
            .UseMauiCommunityToolkit(options =>
            {
                options.SetShouldEnableSnackbarOnWindows(true);
                options.SetShouldSuppressExceptionsInAnimations(true);
                options.SetShouldSuppressExceptionsInBehaviors(true);
                options.SetShouldSuppressExceptionsInConverters(true);

            })

            .UseSharedMauiApp();

        builder.Services.AddSingleton<IDimmerAudioService, AudioService>();
        builder.Services.AddSingleton<IAnimationService, AndroidAnimationService>();
        builder.Services.AddSingleton<AnimationSettingsViewModel>();



        builder.Services.AddSingleton<BaseViewModelAnd>();
        builder.Services.AddSingleton<ChatViewModelAnd>();


        builder.Services.AddSingleton<QuickSettingsTileService>()
        .ConfigureMauiHandlers(handlers =>
        {
            handlers.AddHandler<Shell, MyShellRenderer>();
            
        });


        builder.ConfigureLifecycleEvents(events =>
        {
            events.AddAndroid(android =>
            {
                android.OnCreate((activity, bundle) =>
                {
                    // Handle OnCreate event
                    // You can access the activity and bundle parameters here
                });
                android.OnStart((activity) =>
                {
                    // Handle OnStart event
                });
                android.OnResume((activity) =>
                {
                    // stop any background services if running and save state to db to future restore
                    // Handle OnResume event
                });
                android.OnPause((activity) =>
                {
                    // Handle OnPause event
                });
                android.OnStop((activity) =>
                {
                    // i could do background work here
                    // say i have to save something to the db, or db has a model of bg jobs
                    // i could start a background service here to handle it

                    // Handle OnStop event
                });
                android.OnDestroy((activity) =>
                {

                    // Handle OnDestroy event
                });
                android.OnNewIntent((activity, intent) =>
                {
                    // Handle OnNewIntent event
                    MainApplication.HandleIntent(intent);
                });
                android.OnRestoreInstanceState((activity, bundle) =>
                {
                    // Handle OnRestoreInstanceState event
                });
                android.OnSaveInstanceState((activity, bundle) =>
                {
                    // Handle OnSaveInstanceState event
                    // what can i use this for?

                });

                android.OnRequestPermissionsResult((Android.App.Activity activity, int requestCode, string[] permissions, Android.Content.PM.Permission[] grantResults) =>
                {
                    // Handle OnRequestPermissionsResult event
                    Android.Content.PM.Permission[] permissions1 = grantResults;
                    //Microsoft.Maui.ApplicationModel.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);


                });
                android.OnRestart((activity) =>
                {
                    // Handle OnRestart event
                });

                android.OnPostCreate((activity, bundle) =>
                {
                    //
                    // Handle OnPostCreate event
                });
                android.OnPostResume((activity) =>
                {
                    // Handle OnPostResume event
                });


                android.OnActivityResult((activity, requestCode, resultCode, data) =>
                {
                    // Handle OnActivityResult event
                });
                android.OnConfigurationChanged((activity, newConfig) =>
                {
                    // Handle OnConfigurationChanged event
                });
                android.OnApplicationLowMemory((activity) =>
                {
                    // save app state here, release resources
                    // invoke app to foreground and ask to close or keep alive? 



                    // Handle OnLowMemory event
                });
                android.OnApplicationTrimMemory((activity, level) =>
                {
                    // Handle OnTrimMemory event
                });


            });



        });

        builder.Services.AddScoped<IAppUtil, AppUtil>();
        return builder.Build();
    }
}
