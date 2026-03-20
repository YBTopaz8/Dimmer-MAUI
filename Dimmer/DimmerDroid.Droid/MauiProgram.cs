using CommunityToolkit.Maui.Storage;
using DevExpress.Maui;
using Dimmer;
using Dimmer.Utils;

namespace DimmerDroid.Droid;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseDevExpress()
            .UseDevExpressCharts()
            .UseDevExpressCollectionView()
            .UseDevExpressDataGrid()
            .UseDevExpressEditors()
            .UseDevExpressGauges()
            .UseDevExpressTreeView()
            
            .UseSharedMauiApp();

        // 1. Register all services (NO provider is built here anymore)
        Bootstrapper.Init(builder);

        // 2. Build the app. THIS creates the ONE AND ONLY valid DI Container.
        var app = builder.Build();

        // 3. NOW assign the official MAUI provider to your global static variable
        MainApplication.ServiceProvider = app.Services;

        // 4. Return the app
        return app;
    }
}
