using Material.Components.Maui.Extensions;
using Plugin.ContextMenuContainer;
using Xceed.Maui.Toolkit;
using The49.Maui.BottomSheet;
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
            .UseUraniumUIMaterial()
            //.UseCardsView()
            .UseBottomSheet()
            .UseMauiAudio()
            
            .UseXceedMauiToolkit(FluentDesignAccentColor.DarkPurple)
            .ConfigureContextMenuContainer()
            .UseMaterialComponents()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });
        
#if DEBUG
		builder.Logging.AddDebug();
#endif



        builder.Services.AddSingleton(INativeAudioService => NativeAudioService.Current);
        builder.Services.AddSingleton(FolderPicker.Default);
        builder.Services.AddSingleton(Microsoft.Maui.Storage.FilePicker.Default);
        builder.Services.AddSingleton(FileSaver.Default);

        /* Registering the DataAccess Services */
        builder.Services.AddSingleton<IDataBaseService, DataBaseService>();
        builder.Services.AddSingleton<ISongsManagementService, SongsManagementService>();
        builder.Services.AddSingleton<IStatsManagementService, StatsManagementService>();
        builder.Services.AddSingleton<IPlaylistManagementService, PlayListManagementService>();

        /* Registering the Utilities services */
        builder.Services.AddSingleton<IPlayBackService, PlaybackManagerService>();
        builder.Services.AddSingleton<ILyricsService, LyricsService>();

        builder.Services.AddSingleton<NowPlayingBottomPage>();
        /* Registering the ViewModels */
        builder.Services.AddSingleton<HomePageVM>();
        
        
        /* Registering the Desktop Views */
        builder.Services.AddSingleton<HomeD>();

        /* Registering the Mobile Views */
        builder.Services.AddSingleton<HomePageM>();
        builder.Services.AddSingleton<NowPlayingPageM>();
        return builder.Build();
    }
}
