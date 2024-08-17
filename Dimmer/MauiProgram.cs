#if ANDROID
using Dimmer_MAUI.Platforms.Android.MAudioLib;
#elif WINDOWS
using Dimmer_MAUI.Platforms.Windows;
#endif

namespace Dimmer_MAUI;
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseUraniumUI()
            .UseUraniumUIBlurs()
            .UseUraniumUIMaterial()
            .UseBottomSheet()
            .ConfigureContextMenuContainer()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddMaterialIconFonts();
            });
        
#if DEBUG
		builder.Logging.AddDebug();
#endif

#if ANDROID || WINDOWS
        builder.Services.AddSingleton<INativeAudioService, NativeAudioService>();

        builder.Services.AddSingleton(INativeAudioService => NativeAudioService.Current);
#endif

        builder.Services.AddSingleton(FolderPicker.Default);
        builder.Services.AddSingleton(FilePicker.Default);
        builder.Services.AddSingleton(FileSaver.Default);

        builder.Services.AddSingleton<NowPlayingSongPageBtmSheet>();
        builder.Services.AddTransient<SongMenuBtmSheet>();

        /* Registering the DataAccess Services */
        builder.Services.AddSingleton<IDataBaseService, DataBaseService>();
        builder.Services.AddSingleton<ISongsManagementService, SongsManagementService>();
        builder.Services.AddSingleton<IStatsManagementService, StatsManagementService>();
        builder.Services.AddSingleton<IPlaylistManagementService, PlayListManagementService>();
        
        /* Registering the Utilities services */
        builder.Services.AddSingleton<IPlaybackUtilsService, PlaybackUtilsService>();
        builder.Services.AddSingleton<ILyricsService, LyricsService>();

        /* Registering the ViewModels */
        builder.Services.AddSingleton<HomePageVM>();
        
        
        /* Registering the Desktop Views */
        builder.Services.AddSingleton<HomeD>();
        builder.Services.AddSingleton<NowPlayingD>();
        builder.Services.AddSingleton<PlaylistsPageD>();

        /* Registering the Mobile Views */
        builder.Services.AddSingleton<HomePageM>();
        builder.Services.AddSingleton<PlaylistsPageM>();
        builder.Services.AddSingleton<SinglePlaylistPageM>();

        return builder.Build();
    }
}
