using System.Reflection;

using Dimmer.DimmerLive.Orchestration;
using Dimmer.Interfaces;
using Dimmer.Interfaces.Services.Interfaces.FileProcessing.FileProcessorUtils;
using Dimmer.Interfaces.Services.Lyrics;
using Dimmer.Interfaces.Services.Lyrics.Orchestrator;
using Dimmer.ViewModel.DimmerLiveVM;

using Microsoft.Extensions.Configuration;

using Parse.LiveQuery;

using static Dimmer.DimmerLive.Orchestration.ParseSetup;

namespace Dimmer;

public static class ServiceRegistration
{
    public static IServiceCollection AddDimmerCoreServices(this IServiceCollection services)
    {

        services.AddSingleton<ILyricsMetadataService, LyricsMetadataService>();

        // Configure a named HttpClient for LrcLib
        services.AddHttpClient("LrcLib", client =>
        {
            client.BaseAddress = new Uri("https://lrclib.net/");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Dimmer/2.0 (https://github.com/YBTopaz8/Dimmer-MAUI)");
        });



        services.AddSingleton(FolderPicker.Default);
        services.AddSingleton(FilePicker.Default);
        services.AddSingleton(FileSaver.Default);

        services.AddSingleton<IRealmFactory, RealmFactory>();

        services.AddSingleton<ISettingsService, DimmerSettingsService>();
        services.AddSingleton<IDimmerStateService, DimmerStateService>();
        services.AddSingleton<IErrorHandler, ErrorHandler>();
        services.AddSingleton<IUiErrorPresenter, ErrorHandler>();
        services.AddSingleton<IFolderMgtService, FolderMgtService>();

        services.AddSingleton<ILiveSessionManagerService, ParseDeviceSessionService>();
        services.AddSingleton<SubscriptionManager>();
        services.AddSingleton<MusicDataService>();
        services.AddSingleton<IDimmerPlayEventRepository, DimmerPlayEventRepository>();

        services.AddSingleton<IFolderMonitorService, FolderMonitorService>();
        services.AddSingleton<AchievementService>();
        services.AddSingleton<ILibraryScannerService, LibraryScannerService>();
        services.AddSingleton<IAppInitializerService, AppInitializerService>();
        services.AddSingleton<IDialogueService, DialogueService>();
        services.AddSingleton<ICoverArtService, CoverArtService>();

        services.AddSingleton(typeof(IRepository<>), typeof(RealmCoreRepo<>));
        services.AddSingleton(typeof(IQueueManager<>), typeof(QueueManager<>));



        services.AddSingleton<IAuthenticationService, ParseAuthenticationService>();
        services.AddTransient<LoginViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<SessionManagementViewModel>();




        services.AddSingleton<BaseAppFlow>();
        services.AddSingleton<LyricsMgtFlow>();

        services.AddSingleton<MusicArtistryService>();
        services.AddSingleton<MusicRelationshipService>();
        services.AddSingleton<MusicMetadataService>();
        services.AddSingleton<MusicPowerUserService>();

        services.AddSingleton<ILastfmService, LastfmService>();
        services.AddSingleton<BaseViewModel>();
        services.AddSingleton(FolderPicker.Default);
        services.AddSingleton(FileSaver.Default);

        services.AddSingleton<IDuplicateFinderService, DuplicateFinderService>();

        services.AddSingleton<StatisticsService>();
        services.AddSingleton<StatisticsViewModel>();



        services.AddSingleton<ParseLiveQueryClient>();


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

        // Build the config object
        var config = BuildConfiguration();

        // Register IConfiguration so you can inject it if needed
        if (config != null)
        {
            services.AddSingleton<IConfiguration>(config);
            // Map the "Lastfm" section to the LastfmSettings class
            services.Configure<LastfmSettings>(config.GetSection("Lastfm"));
            var parseSection = config.GetSection("YBParse");

            if (parseSection.Exists())
            {
                // You must access the keys relative to the section, OR use the full path "YBParse:ApplicationId"
                YBParse.ApplicationId = parseSection["ApplicationId"];
                YBParse.ServerUri = parseSection["ServerUri"];
                YBParse.DotNetKEY = parseSection["DotNetKEY"];
            }
            else
            {
                Console.WriteLine("CRITICAL: 'YBParse' section missing in appsettings.json");
            }
        }


        //services.AddSingleton<ILocalLyricsProvider, EmbeddedLyricsProvider>();
        //services.AddSingleton<ILocalLyricsProvider, LocalLrcFileProvider>();
        services.AddSingleton<IOnlineLyricsProvider, LrcLibProvider>();
        services.AddSingleton<LastFMViewModel>();

        // 2. Register the persistence service.
        services.AddSingleton<ILyricsPersistenceService, LyricsPersistenceService>();


        // Singletons for services that manage state and connections
        services.AddSingleton<IAuthenticationService, ParseAuthenticationService>();
        //services.AddSingleton<ILiveSessionManagerService, ParseDeviceSessionService>();
        services.AddSingleton<IFriendshipService, ParseFriendshipService>();
        services.AddSingleton<IChatService, ParseChatService>();

        // Transients for ViewModels
        services.AddSingleton<SocialViewModel>();

        services.AddSingleton<AchievementsViewModel>();
        services.AddSingleton<SongAchievementsViewModel>();

        services.AddSingleton<ChatViewModel>(); // You'll create this next

        // Feedback service and ViewModels
        services.AddSingleton<IFeedbackService, ParseFeedbackService>();
        services.AddSingleton<FeedbackBoardViewModel>();
        services.AddTransient<FeedbackSubmissionViewModel>();
        services.AddTransient<FeedbackDetailViewModel>();

        RegisterPartAndClasses();
        return services;
    }

    private static void RegisterPartAndClasses()
    {
        if (Connectivity.NetworkAccess == NetworkAccess.Internet && ParseSetup.InitializeParseClient())
        {
            ParseClient.Instance.RegisterSubclass(typeof(UserDeviceSession));
            ParseClient.Instance.RegisterSubclass(typeof(ChatConversation));
            ParseClient.Instance.RegisterSubclass(typeof(ChatMessage));
            ParseClient.Instance.RegisterSubclass(typeof(DimmerSharedSong));
            ParseClient.Instance.RegisterSubclass(typeof(UserModelOnline));
            ParseClient.Instance.RegisterSubclass(typeof(DeviceState));
            ParseClient.Instance.RegisterSubclass(typeof(UserModelOnline));
            ParseClient.Instance.RegisterSubclass(typeof(FriendRequest));
            ParseClient.Instance.RegisterSubclass(typeof(AppUpdateModel));
            ParseClient.Instance.RegisterSubclass(typeof(FeedbackIssue));
            ParseClient.Instance.RegisterSubclass(typeof(FeedbackComment));
            ParseClient.Instance.RegisterSubclass(typeof(FeedbackVote));
            ParseClient.Instance.RegisterSubclass(typeof(FeedbackNotificationSettings));

        }
    }
    private static IConfigurationRoot? BuildConfiguration()
    {
        var assembly = Assembly.GetExecutingAssembly();
        const string resourceName = "Dimmer.appsettings.json";

        using var stream = assembly.GetManifestResourceStream(resourceName);

        if (stream == null)
        {
            // Returning null here allows the app to continue, 
            // but you might want to throw if config is critical.
            Console.WriteLine($"WARNING: Embedded Resource '{resourceName}' not found.");
            return null;
        }

        return new ConfigurationBuilder()
                    .AddJsonStream(stream)
                    .Build();
    }
}