using DevExpress.Maui;
using Dimmer;

namespace DimmerDroid.Droid;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        try
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
                .UseDevExpressControls()
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
        catch (Exception ex)
        {
            // THIS WILL CATCH THE EXACT REASON IT EXITS!
            System.Diagnostics.Debug.WriteLine("========================================");
            System.Diagnostics.Debug.WriteLine($"CRITICAL STARTUP CRASH: {ex}");
            System.Diagnostics.Debug.WriteLine("========================================");
            throw;
        }
    }
}
