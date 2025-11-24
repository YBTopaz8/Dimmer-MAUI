using Microsoft.Extensions.Logging;

namespace Dimmer.Utils;

internal class Bootstrapper
{
    public static IServiceProvider Init()
    {
        var services = new ServiceCollection();

        // 1. Logging (Replaces builder.Logging)
        services.AddLogging(configure =>
        {
            configure.AddDebug();
        });
        services.AddDimmerCoreServices();

        // 2. Register your Core Services (Same as before)
        services.AddSingleton<IDimmerAudioService, AudioService>();
        services.AddSingleton<IAnimationService, AndroidAnimationService>();

        // 3. Register ViewModels
        services.AddSingleton<BaseViewModelAnd>();

        // 4. Register Logic/Data Services
        services.AddScoped<IAppUtil, AppUtil>();

        //services.AddSingleton<IDimmerStateService, DimmerStateService>();
        //services.AddSingleton<MusicDataService>();
        // ... Add ALL your other repos and services here ...
        // services.AddSingleton<IRepository<SongModel>, SongRepository>(); 

        // 5. Handle "Shared" logic
        // If "UseSharedMauiApp" was an extension method in your shared project,
        // Refactor it to accept IServiceCollection instead of MauiAppBuilder.
        // e.g., services.AddSharedServices();

        // 6. Build the provider
        return services.BuildServiceProvider();
    }
}
