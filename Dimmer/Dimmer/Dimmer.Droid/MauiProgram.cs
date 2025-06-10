using Dimmer.Utils.CustomHandlers.CollectionView;
using Dimmer.Views.CustomViewsParts;
using Dimmer.Views.Stats;

using Microsoft.Maui.Controls.Compatibility.Hosting;

namespace Dimmer;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        ThemeManager.UseAndroidSystemColor = true;

        builder
              .ConfigureEssentials(essentials =>
              {
                  essentials
                      .AddAppAction("play_last_audio", "Play Last Audio", icon: "atom")
                      // Provide actual icon resource                      
                      //.AddAppAction("browse_audio", "Browse Audio Files", icon: "browse_action_icon")
                      //.AddAppAction("app_settings", "App Settings", subtitle: "Configure preferences")
                      .OnAppAction(MainApplication.HandleAppAction);
              })
            .UseDevExpress(useLocalization: false)
            .UseDevExpressCollectionView()
            .UseDevExpressControls()
            .UseDevExpressDataGrid()
            .UseDevExpressEditors()
            .UseDevExpressGauges()
            .UseUraniumUI()

            .UseUraniumUIBlurs()
            .UseUraniumUIMaterial()
            .UseSharedMauiApp();

        builder.Services.AddSingleton<IDimmerAudioService, AudioService>();
        builder.Services.AddSingleton<HomePage>();
        builder.Services.AddSingleton<DimmerSettings>();
        builder.Services.AddSingleton<DimmerVault>();
        builder.Services.AddSingleton<IAnimationService, AndroidAnimationService>();
        builder.Services.AddSingleton<SearchSongPage>();
        builder.Services.AddSingleton<ArtistsPage>();


        builder.Services.AddSingleton<BaseViewModelAnd>();
        builder.Services.AddSingleton<BtmBar>();
        builder.Services.AddSingleton<NowPlayingbtmsheet>();
        builder.Services.AddSingleton<PlayHistoryPage>();
        builder.Services.AddSingleton<QuickSettingsTileService>()
        .ConfigureMauiHandlers(handlers =>
        {
            handlers.AddHandler<Shell, MyShellRenderer>();
            handlers.AddHandler<CollectionView, CustomCollectionViewHandler>();

        });

        builder.Services.AddScoped<IAppUtil, AppUtil>();
        return builder.Build();
    }

}
