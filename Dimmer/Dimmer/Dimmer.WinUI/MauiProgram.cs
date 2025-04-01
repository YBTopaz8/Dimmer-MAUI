
using Dimmer.Interfaces;
using Dimmer.Interfaces.IDatabase;
using Dimmer.WinUI.DimmerAudio;
using Dimmer.WinUI.Utils;
using Dimmer.WinUI.Views;

namespace Dimmer.WinUI;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseSharedMauiApp();
        builder.Services.AddSingleton<IDimmerAudioService, AudioService>();
        builder.Services.AddScoped<HomePage>();
        builder.Services.AddScoped<HomeViewModel>();
        builder.Services.AddScoped<IAppUtil, AppUtil>();

        return builder.Build();
    }
}
