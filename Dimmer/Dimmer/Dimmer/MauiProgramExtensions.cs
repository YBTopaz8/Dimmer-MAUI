

using Dimmer.Interfaces.IServices;
using Dimmer.Services;

namespace Dimmer;

public static class MauiProgramExtensions
{
    public static MauiAppBuilder UseSharedMauiApp(this MauiAppBuilder builder)
    {
        builder
            .UseMauiApp<App>()
            .UseBarcodeReader()

            .UseMauiCommunityToolkit(options =>
            {
                options.SetShouldSuppressExceptionsInAnimations(true);
                options.SetShouldSuppressExceptionsInBehaviors(true);
                options.SetShouldSuppressExceptionsInConverters(true);

            })
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSansRegular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSansSemibold.ttf", "OpenSansSemibold");
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
        builder.Services.AddSingleton<IDimmerStateService, DimmerStateService>();
        builder.Services.AddSingleton<IDimmerLiveStateService, DimmerLiveStateService>();
        builder.Services.AddSingleton<IFolderMgtService, FolderMgtService>();

        builder.Services.AddSingleton<SubscriptionManager>();

        builder.Services.AddSingleton<IFolderMonitorService, FolderMonitorService>();
        builder.Services.AddSingleton<ILibraryScannerService, LibraryScannerService>();


        builder.Services.AddSingleton(typeof(IRepository<>), typeof(RealmCoreRepo<>));
        builder.Services.AddSingleton(typeof(IQueueManager<>), typeof(QueueManager<>));


        IMapper? mapper = AutoMapperConf.ConfigureAutoMapper();
        builder.Services.AddSingleton(mapper);

        builder.Services.AddSingleton<BaseAppFlow>();
        builder.Services.AddSingleton<SongsMgtFlow>();
        builder.Services.AddSingleton<AlbumsMgtFlow>();
        builder.Services.AddSingleton<PlayListMgtFlow>();
        builder.Services.AddSingleton<LyricsMgtFlow>();



        builder.Services.AddSingleton<BaseViewModel>();
        builder.Services.AddSingleton(FolderPicker.Default);
        builder.Services.AddSingleton(FileSaver.Default);
        return builder;
    }
}
