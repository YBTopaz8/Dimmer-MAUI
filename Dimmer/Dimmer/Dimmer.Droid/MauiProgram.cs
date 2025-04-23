using Dimmer.Interfaces;
using Dimmer.Utils;
using Dimmer.Views;

namespace Dimmer.Droid;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseSharedMauiApp();

        builder.Services.AddSingleton<HomePage>();

        builder.Services.AddScoped<IAppUtil, AppUtil>();
        return builder.Build();
    }

}
