

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
                fonts.AddFont("FontAwesomeRegular400.otf", "FontAwesomeRegular");
                fonts.AddFont("FontAwesome6FreeSolid900.otf", "FontAwesomeSolid");
                fonts.AddFont("FABrandsRegular400.otf", "FontAwesomeBrands");
            })
            .ConfigureSyncfusionToolkit();

#if DEBUG
        builder.Logging.AddDebug();
#endif


        builder.Services.AddSingleton(FolderPicker.Default);
        builder.Services.AddSingleton(FilePicker.Default);
        builder.Services.AddSingleton(FileSaver.Default);

        builder.Services.AddSingleton<IRealmFactory, RealmFactory>();
     
        builder.Services.AddSingleton<ISettingsService, DimmerSettingsService>();
        builder.Services.AddSingleton<IPlayerStateService, PlayerStateService>();

        builder.Services.AddTransient<SubscriptionManager>();

        builder.Services.AddSingleton<IFolderMonitorService, FolderMonitorService>();

        builder.Services.AddTransient(typeof(IRepository<>), typeof(RealmCoreRepo<>));
        builder.Services.AddTransient(typeof(IQueueManager<>), typeof(QueueManager<>));


        IMapper? mapper = AutoMapperConf.ConfigureAutoMapper();
        builder.Services.AddSingleton(mapper);

        builder.Services.AddSingleton<BaseAppFlow>();
        builder.Services.AddSingleton<SongsMgtFlow>();
        builder.Services.AddSingleton<AlbumsMgtFlow>();
        builder.Services.AddSingleton<PlayListMgtFlow>();
        builder.Services.AddSingleton<LyricsMgtFlow>();



        builder.Services.AddSingleton<BaseViewModel>();
        builder.Services.AddTransient<BaseAlbumViewModel>();
        builder.Services.AddSingleton(FolderPicker.Default);
        builder.Services.AddSingleton(FileSaver.Default);
        return builder;
    }
}
