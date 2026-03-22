using Dimmer.DimmerLive;
using Dimmer.NativeServices;
using Dimmer.Views.CustomViews;
using Dimmer.Views.Settings;
using Dimmer.Views.SingleSong;

namespace Dimmer.Utils;

internal class Bootstrapper
{



    public static void Init(MauiAppBuilder builder)
    {
        try
        {
            
                var services = builder.Services;
            

            services.AddDimmerCoreServices();


          

            services.AddSingleton<IDimmerAudioService, AudioService>();

            services.AddSingleton<IBluetoothService, AndroidBluetoothService>();

        // 3. Register ViewModels
        services.AddSingleton<BaseViewModelAnd>();
        services.AddSingleton<SettingsPage>();

        services.AddSingleton<DetailsOverview>();
        services.AddSingleton<PlaybackQueueBtmSheet>();
        services.AddSingleton<NowPlayingBottomSheet>();
        services.AddSingleton<AlbumPage>();
        services.AddSingleton<ArtistPage>();
        

        // 4. Register Logic/Data Services
        services.AddSingleton<IAppUtil, AppUtil>();


           
        }
        catch (Exception ex)
        {
            Log.Error("DIMMER_BOOT_FAIL", $"CRITICAL: {ex.Message}");
            Log.Error("DIMMER_BOOT_FAIL", $"Inner: {ex.InnerException?.Message}");
            Log.Error("DIMMER_BOOT_FAIL", $"Stack: {ex.StackTrace}");
            throw; // Rethrow so TransitionActivity catches it
        }
    }
}
