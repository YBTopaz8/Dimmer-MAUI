using Dimmer.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
