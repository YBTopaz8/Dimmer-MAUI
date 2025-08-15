﻿using Dimmer.Data.RealmStaticFilters;
using Dimmer.DimmerLive.Interfaces.Services;
using Dimmer.DimmerSearch.Interfaces;
using Dimmer.DimmerSearch.Interfaces.Implementations;
using Dimmer.DimmerSearch.TQL.TQLCommands;
using Dimmer.Interfaces.IDatabase;
using Dimmer.Interfaces.Services.Interfaces;
using Dimmer.Interfaces.Services.Interfaces.FileProcessing;
using Dimmer.Interfaces.Services.Lyrics;
using Dimmer.Interfaces.Services.Lyrics.Orchestrator;
using Dimmer.LastFM;

using Microsoft.Extensions.Configuration;
using Microsoft.Maui.LifecycleEvents;
using Microsoft.Maui.Platform;

using SkiaSharp.Views.Maui.Controls.Hosting;

using System.Reflection;

namespace Dimmer;

public static class MauiProgramExtensions
{
    public static MauiAppBuilder UseSharedMauiApp(this MauiAppBuilder builder)
    {
        builder
            .UseMauiApp<App>()
            .UseSkiaSharp()
            .UseMauiCommunityToolkit(options =>
            {
                options.SetShouldSuppressExceptionsInAnimations(true);
                options.SetShouldSuppressExceptionsInBehaviors(true);
                options.SetShouldSuppressExceptionsInConverters(true);

            })
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("nothingfont.otf", "AleySans");
                fonts.AddFont("FontAwesomeRegular400.otf", "FontAwesomeRegular");
                fonts.AddFont("FontAwesome6FreeSolid900.otf", "FontAwesomeSolid");
                fonts.AddFont("FABrandsRegular400.otf", "FontAwesomeBrands");
            })
            .ConfigureSyncfusionToolkit();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        builder.Services.AddSingleton<ILyricsMetadataService, LyricsMetadataService>();

        // Configure a named HttpClient for LrcLib
        builder.Services.AddHttpClient("LrcLib", client =>
        {
            client.BaseAddress = new Uri("https://lrclib.net/");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Dimmer/2.0 (https://github.com/YBTopaz8/Dimmer-MAUI)");
        });

        
        builder.Services.AddSingleton(FolderPicker.Default);
        builder.Services.AddSingleton(FilePicker.Default);
        builder.Services.AddSingleton(FileSaver.Default);

        builder.Services.AddSingleton<IRealmFactory, RealmFactory>();

        builder.Services.AddSingleton<IDeviceConnectivityService, DeviceConnectivityService>();
        builder.Services.AddSingleton<ISettingsService, DimmerSettingsService>();
        builder.Services.AddSingleton<IDimmerStateService, DimmerStateService>();
        builder.Services.AddSingleton<IErrorHandler, ErrorHandler>();
        builder.Services.AddSingleton<IDimmerLiveStateService, DimmerLiveStateService>();
        builder.Services.AddSingleton<IFolderMgtService, FolderMgtService>();

        builder.Services.AddSingleton<IDeviceSessionService, ParseDeviceSessionService>();
        builder.Services.AddSingleton<SubscriptionManager>();
        builder.Services.AddSingleton<MusicDataService>();
        builder.Services.AddSingleton<IDimmerPlayEventRepository, DimmerPlayEventRepository>();

        builder.Services.AddSingleton<IFolderMonitorService, FolderMonitorService>();
        builder.Services.AddSingleton<ILibraryScannerService, LibraryScannerService>();
        builder.Services.AddSingleton<IAppInitializerService, AppInitializerService>();
        builder.Services.AddSingleton<IDialogueService, DialogueService>();
        builder.Services.AddSingleton<ICoverArtService, CoverArtService>();

        builder.Services.AddSingleton(typeof(IRepository<>), typeof(RealmCoreRepo<>));
        builder.Services.AddSingleton(typeof(IQueueManager<>), typeof(QueueManager<>));


        IMapper? mapper = AutoMapperConf.ConfigureAutoMapper();

        builder.Services.AddSingleton(mapper);


        builder.Services.AddSingleton<IAuthenticationService, ParseAuthenticationService>();
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddSingleton<DeviceConnectivityService>();
        builder.Services.AddSingleton<SessionTransferViewModel>();
        builder.Services.AddSingleton<IPresenceService, ParsePresenceService>();
        builder.Services.AddSingleton<SessionTransferViewModel>();






        builder.Services.AddSingleton<BaseAppFlow>();
        builder.Services.AddSingleton<LyricsMgtFlow>();

        builder.Services.AddSingleton<MusicArtistryService>();
        builder.Services.AddSingleton<MusicRelationshipService>();
        builder.Services.AddSingleton<MusicMetadataService>();
        builder.Services.AddSingleton<MusicPowerUserService>();

        builder.Services.AddSingleton<ILastfmService, LastfmService>();
        builder.Services.AddSingleton<BaseViewModel>();
        builder.Services.AddSingleton(FolderPicker.Default);
        builder.Services.AddSingleton(FileSaver.Default);

            
        builder.Services.AddSingleton<IDuplicateFinderService, DuplicateFinderService>();

        builder.Services.AddSingleton<StatisticsService>();
        builder.Services.AddSingleton<StatisticsViewModel>();




        var assembly = Assembly.GetExecutingAssembly();

        const string resourceName = "Dimmer.appsettings.json";

        var ress = assembly.GetManifestResourceNames();


        using var stream = assembly.GetManifestResourceStream(resourceName);

        // This null check will prevent the crash and tell you exactly what's wrong.
        if (stream == null)
        {
            // If you hit this, the resource name is still wrong or the build action is not set.
            throw new FileNotFoundException(
                $"Could not find the embedded resource '{resourceName}'. " +
                "Ensure the 'Build Action' is set to 'Embedded resource' for Dimmer.appsettings.json",
                resourceName);
        }

        // This section will now work without crashing.
        var config = new ConfigurationBuilder()
                    .AddJsonStream(stream)
                    .Build();

        builder.Configuration.AddConfiguration(config);

        // Register LastfmSettings
        builder.Services.Configure<LastfmSettings>(builder.Configuration.GetSection("Lastfm"));


        //builder.Services.AddSingleton<ILocalLyricsProvider, EmbeddedLyricsProvider>();
        //builder.Services.AddSingleton<ILocalLyricsProvider, LocalLrcFileProvider>();
        builder.Services.AddSingleton<IOnlineLyricsProvider, LrcLibProvider>();

        // 2. Register the persistence service.
        builder.Services.AddSingleton<ILyricsPersistenceService, LyricsPersistenceService>();

        return builder;
    }
}
