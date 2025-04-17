﻿using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Storage;
using Dimmer.Data;
using Dimmer.Services;
using Microsoft.Extensions.Logging;
using Syncfusion.Maui.Toolkit.Hosting;

namespace Dimmer;

public static class MauiProgramExtensions
{
    public static MauiAppBuilder UseSharedMauiApp(this MauiAppBuilder builder)
    {
        builder
            .UseMauiApp<App>()
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
            })
            .ConfigureSyncfusionToolkit();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        builder.Services.AddSingleton<IRealmFactory, RealmFactory>();
     
        builder.Services.AddSingleton<ISettingsService, DimmerSettingsService>();
        builder.Services.AddSingleton<IPlayerStateService, PlayerStateService>();

        builder.Services.AddSingleton<SubscriptionManager>();

        builder.Services.AddSingleton<IFolderMonitorService, FolderMonitorService>();

        builder.Services.AddTransient(typeof(IRepository<>), typeof(RealmCoreRepo<>));
        builder.Services.AddTransient(typeof(IQueueManager<>), typeof(QueueManager<>));


        var mapper = AutoMapperConf.ConfigureAutoMapper();
        builder.Services.AddSingleton(mapper);

        builder.Services.AddSingleton<BaseAppFlow>();
        builder.Services.AddSingleton<SongsMgtFlow>();
        builder.Services.AddSingleton<AlbumsMgtFlow>();
        builder.Services.AddSingleton<BaseViewModel>();
        builder.Services.AddSingleton<BaseAlbumViewModel>();
        builder.Services.AddSingleton(FolderPicker.Default);
        builder.Services.AddSingleton(FileSaver.Default);
        return builder;
    }
}
