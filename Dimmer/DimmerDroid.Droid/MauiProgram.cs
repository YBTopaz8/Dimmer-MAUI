using Dimmer;
using Dimmer.Utils;

namespace DimmerDroid.Droid;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseSharedMauiApp();


        MainApplication.ServiceProvider = Bootstrapper.Init(builder);

        return builder.Build();
    }
}
