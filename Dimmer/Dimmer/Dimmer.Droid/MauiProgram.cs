
namespace Dimmer.Droid;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
              .ConfigureEssentials(essentials =>
              {
                  essentials
                      .AddAppAction("play_last_audio", "Play Last Audio", icon: "play_action_icon") // Provide actual icon resource
                      .AddAppAction("browse_audio", "Browse Audio Files", icon: "browse_action_icon")
                      .AddAppAction("app_settings", "App Settings", subtitle: "Configure preferences")
                      .OnAppAction(MainApplication.HandleAppAction); 
              })
            .UseDevExpress(useLocalization: false)
            .UseDevExpressCollectionView()
            .UseDevExpressControls()
            .UseDevExpressDataGrid()
            .UseDevExpressEditors()
            .UseDevExpressGauges()
            .UseSharedMauiApp();

        builder.Services.AddSingleton<IDimmerAudioService, AudioService>();
        builder.Services.AddSingleton<HomePage>();
        builder.Services.AddSingleton<SettingsPage>();

        builder.Services.AddSingleton<HomePageViewModel>();

        builder.Services.AddSingleton<BaseViewModelAnd>();
        builder.Services.AddSingleton<QuickSettingsTileService>();


        builder.Services.AddScoped<IAppUtil, AppUtil>();
        return builder.Build();
    }

}
