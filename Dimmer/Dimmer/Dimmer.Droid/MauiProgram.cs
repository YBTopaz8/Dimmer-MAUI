
namespace Dimmer.Droid;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseDevExpress(useLocalization: false)
            .UseDevExpressCollectionView()
            .UseDevExpressControls()
            .UseDevExpressDataGrid()
            .UseDevExpressEditors()
            .UseDevExpressGauges()
            .UseSharedMauiApp();

        builder.Services.AddSingleton<IDimmerAudioService, AudioService>();
        builder.Services.AddSingleton<HomePage>();
        builder.Services.AddSingleton<SettingsPage>();

        builder.Services.AddSingleton<HomePageViewModel>();

        builder.Services.AddSingleton<BaseViewModelAnd>();



        builder.Services.AddScoped<IAppUtil, AppUtil>();
        return builder.Build();
    }

}
