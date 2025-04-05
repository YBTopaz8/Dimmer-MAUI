using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Storage;
using Dimmer.Data;
using Dimmer.Interfaces.IDatabase;
using Dimmer.Orchestration;
using Dimmer.ViewModel;
using Microsoft.Extensions.Logging;
using Syncfusion.Maui.Toolkit.Hosting;

namespace Dimmer;

public static class MauiProgramExtensions
{
    public static MauiAppBuilder UseSharedMauiApp(this MauiAppBuilder builder)
    {
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit(options =>
            {
                options.SetShouldSuppressExceptionsInAnimations(true);
                options.SetShouldSuppressExceptionsInBehaviors(true);
                options.SetShouldSuppressExceptionsInConverters(true);

            })
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            })
            .ConfigureSyncfusionToolkit();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        builder.Services.AddSingleton<IRealmFactory, RealmFactory>();
        builder.Services.AddSingleton<Realm>(serviceProvider =>
        {
            return BaseDBInstance.GetRealm();
            
        });

        
        var mapper = AutoMapperConf.ConfigureAutoMapper();
        builder.Services.AddSingleton(mapper);

        builder.Services.AddSingleton<BaseAppFlow>();
        builder.Services.AddSingleton<BaseViewModel>();
        builder.Services.AddSingleton(FolderPicker.Default);
        builder.Services.AddSingleton(FileSaver.Default);
        return builder;
    }
}
