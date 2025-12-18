namespace Dimmer;

public static class MauiProgramExtensions
{
    public static MauiAppBuilder UseSharedMauiApp(this MauiAppBuilder builder)
    {
        builder
            .UseMauiApp<App>()
            //.UseSkiaSharp()
            .UseMauiCommunityToolkit(options =>
            {
                options.SetShouldSuppressExceptionsInAnimations(true);
                options.SetShouldSuppressExceptionsInBehaviors(true);
                options.SetShouldSuppressExceptionsInConverters(true);

            })
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("nothingfont.otf", "AleySans");
                fonts.AddFont("MaterialIconsOutlined-Regular.otf", "MatOutReg");
                fonts.AddFont("MaterialIconsRound-Regular.otf", "MatRoundReg");
                fonts.AddFont("MaterialIcons-Regular.otf", "MatReg");
                fonts.AddFont("MaterialIconsSharp-Regular.otf", "MatSharpReg");
                fonts.AddFont("MaterialIconsTwoTone-Regular.otf", "MatTwoToneReg");
                fonts.AddFont("FontAwesomeRegular400.otf", "FontAwesomeRegular");
                fonts.AddFont("FontAwesome6FreeSolid900.otf", "FontAwesomeSolid");
                fonts.AddFont("FABrandsRegular400.otf", "FontAwesomeBrands");
            });
           

#if DEBUG
        builder.Logging.AddDebug();
#endif

     

        return builder;
    }
}
