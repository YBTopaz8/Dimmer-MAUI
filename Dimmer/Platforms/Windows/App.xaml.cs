using Microsoft.Windows.AppLifecycle;
using Windows.ApplicationModel.Activation;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer_MAUI.WinUI;
/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : MauiWinUIApplication
{
    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        var mainInstance = AppInstance.FindOrRegisterForKey("MainDimmer");
        if (!mainInstance.IsCurrent)
        {
            var currentInstance = AppInstance.GetCurrent();
            var args = currentInstance.GetActivatedEventArgs();
            mainInstance.RedirectActivationToAsync(args).GetAwaiter().GetResult();

            Process.GetCurrentProcess().Kill();
            return;
        }
        mainInstance.Activated += MainInstance_Activated;

        this.InitializeComponent();
    }

    
    private void MainInstance_Activated(object? sender, AppActivationArguments e)
    {
            HandleActivated(e);
        //TODO :  Optimize this. HandleActivated is called an unknown number of time and is slow with many songs
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);
        
        var activatedArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
        do
        {
            HandleActivated(activatedArgs);

        }while (activatedArgs.Kind == ExtendedActivationKind.File && paths?.Length < 0);

    }

    string[]? paths = Array.Empty<string>();
    private void HandleActivated(AppActivationArguments args)
    {
        switch (args.Kind)
        {
            case ExtendedActivationKind.File:
                var fileArgs = args.Data as IFileActivatedEventArgs;
                if (fileArgs is not null)
                {
                    paths = fileArgs.Files.Select(file => (file as StorageFile)?.Path).ToArray();
                    HandleFiles(paths);
                }

                break;
            default:
                break;
        }

        //switch (args.Kind)
        //{
        //    case ExtendedActivationKind.File:
        //        var fileArgs = args.Data as IFileActivatedEventArgs;
        //        if (fileArgs is not null)
        //        {
        //            var pathss = fileArgs.Files.Select(file => (file as StorageFile)?.Path).ToArray();
        //            paths = pathss;
        //            HandleFiles(pathss);
        //        }
        //        break;
        //    default:
        //        break;
        //}
    }

    private void HandleFiles(string?[] paths)
    {
        if (paths.Length < 1)
            return;
        
        var home = IPlatformApplication.Current!.Services.GetService<HomePageVM>();
        home!.LoadLocalSongFromOutSideApp(paths!);

    }
}

