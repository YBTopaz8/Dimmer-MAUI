using Dimmer.DimmerLive;
using Dimmer.NativeServices;

namespace Dimmer.Utils;

internal class Bootstrapper
{
    public static IServiceProvider Init()
    {
        try
        {
            var services = new ServiceCollection();
            LinkerKeepAlive.Keep();
        // 1. Logging (Replaces builder.Logging)
        services.AddLogging(configure =>
        {
            configure.AddDebug();
        });
        services.AddDimmerCoreServices();

            // Platform-specific Bluetooth service
            services.AddSingleton<IBluetoothService, AndroidBluetoothService>();

            services.AddSingleton<AndroidFolderPicker>();
            // Register Android-specific error presenter (overrides the default one from core services)
            services.AddSingleton<IUiErrorPresenter, AndroidErrorPresenter>();
            // 2. Register your Core Services (Same as before)
            services.AddSingleton<IDimmerAudioService, AudioService>();
        services.AddSingleton<IAnimationService, AndroidAnimationService>();

        // 3. Register ViewModels
        services.AddSingleton<BaseViewModelAnd>();
        services.AddSingleton<LoginViewModelAnd>();

        // 4. Register Logic/Data Services
        services.AddScoped<IAppUtil, AppUtil>();


            // 6. Build the provider
            var provider = services.BuildServiceProvider();

            Log.Error("DIMMER_BOOT", "Bootstrapper finished successfully.");
            return provider;
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
