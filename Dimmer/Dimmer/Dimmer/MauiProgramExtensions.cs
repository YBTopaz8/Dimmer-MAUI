using Dimmer.Data.RealmStaticFilters;
using Dimmer.Interfaces.Services.Interfaces;

using SkiaSharp.Views.Maui.Controls.Hosting;

namespace Dimmer;

public static class MauiProgramExtensions
{
    public static MauiAppBuilder UseSharedMauiApp(this MauiAppBuilder builder)
    {
        builder
            .UseMauiApp<App>()
            .UseBarcodeReader()
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

        builder.Services.AddSingleton<ISettingsService, DimmerSettingsService>();
        builder.Services.AddSingleton<IDimmerStateService, DimmerStateService>();
        builder.Services.AddSingleton<IErrorHandler, ErrorHandler>();
        builder.Services.AddSingleton<IDimmerLiveStateService, DimmerLiveStateService>();
        builder.Services.AddSingleton<IFolderMgtService, FolderMgtService>();

        builder.Services.AddSingleton<SubscriptionManager>();

        builder.Services.AddSingleton<IFolderMonitorService, FolderMonitorService>();
        builder.Services.AddSingleton<ILibraryScannerService, LibraryScannerService>();
        builder.Services.AddSingleton<IAppInitializerService, AppInitializerService>();


        builder.Services.AddSingleton(typeof(IRepository<>), typeof(RealmCoreRepo<>));
        builder.Services.AddSingleton(typeof(IQueueManager<>), typeof(QueueManager<>));


        IMapper? mapper = AutoMapperConf.ConfigureAutoMapper();
        builder.Services.AddSingleton(mapper);

        builder.Services.AddSingleton<BaseAppFlow>();
        builder.Services.AddSingleton<SongsMgtFlow>();
        builder.Services.AddSingleton<AlbumsMgtFlow>();
        builder.Services.AddSingleton<LyricsMgtFlow>();

        builder.Services.AddSingleton<MusicArtistryService>();
        builder.Services.AddSingleton<MusicRelationshipService>();
        builder.Services.AddSingleton<MusicMetadataService>();
        builder.Services.AddSingleton<MusicPowerUserService>();

        builder.Services.AddSingleton<BaseViewModel>();
        builder.Services.AddSingleton(FolderPicker.Default);
        builder.Services.AddSingleton(FileSaver.Default);
        return builder;
    }
}
