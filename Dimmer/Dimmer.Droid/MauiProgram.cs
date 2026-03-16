using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        MauiAppBuilder? builder = MauiApp.CreateBuilder();

        builder
            
            .UseSharedMauiApp()
            ;

        MainApplication.ServiceProvider = Bootstrapper.Init(builder);
        
        return builder.Build();
    }
}