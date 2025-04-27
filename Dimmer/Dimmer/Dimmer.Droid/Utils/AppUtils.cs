namespace Dimmer.Utils;
public class AppUtil : IAppUtil
{
    public Shell GetShell()
    {
        return new AppShell();  
    }

    public Window LoadWindow()
    {
        Window window = new Window();
        window.Page = GetShell();
        return window;
    }
}
