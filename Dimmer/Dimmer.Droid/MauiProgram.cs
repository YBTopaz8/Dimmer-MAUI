﻿using CommunityToolkit.Maui;

using Dimmer.Utils.CustomHandlers;
using Dimmer.Utils.CustomHandlers.CollectionView;
using Dimmer.Views.CustomViewsParts;
using Dimmer.Views.Stats;

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
            .UseMauiCommunityToolkit(options =>
            {
                options.SetShouldSuppressExceptionsInAnimations(true);
                options.SetShouldSuppressExceptionsInBehaviors(true);
                options.SetShouldSuppressExceptionsInConverters(true);

            })
            .UseDevExpressControls()
            .UseDevExpressDataGrid()
            .UseDevExpressEditors()
            .UseDevExpressGauges()

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
        builder.Services.AddSingleton<QuickPanelBtmSheet>();
        builder.Services.AddSingleton<MoreModal>();
        builder.Services.AddSingleton<MainViewExpander>();
        builder.Services.AddSingleton<TopBeforeColView>();
        builder.Services.AddSingleton<NowPlayingUISection>();
        builder.Services.AddSingleton<NowPlayingbtmsheet>();
        //builder.Services.AddSingleton<SearchFilterAndSongsColViewUI>();
        builder.Services.AddSingleton<PlayHistoryPage>();
        builder.Services.AddSingleton<QuickSettingsTileService>()
        .ConfigureMauiHandlers(handlers =>
        {
            handlers.AddHandler<Shell, MyShellRenderer>();
            handlers.AddHandler<CollectionView, CustomCollectionViewHandler>();
        });
        builder.Services.AddSingleton<PlayerViewModel>();

        builder.Services.AddScoped<IAppUtil, AppUtil>();
        return builder.Build();
    }

}
