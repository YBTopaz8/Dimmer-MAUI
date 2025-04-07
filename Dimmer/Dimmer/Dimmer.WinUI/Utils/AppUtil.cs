namespace Dimmer.WinUI.Utils;
public class AppUtil : IAppUtil
{
    public Shell GetShell()
    {
        return new AppShell();
    }
}
