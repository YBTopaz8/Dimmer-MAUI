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
        return builder.Build();
    }
}
