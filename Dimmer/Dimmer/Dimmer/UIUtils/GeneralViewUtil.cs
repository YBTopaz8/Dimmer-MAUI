using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.UIUtils;
public static class GeneralViewUtil
{
    public static void PointerOnView(View theView)
    {
        theView.BackgroundColor = Colors.DarkSlateBlue;
    }
    public static void PointerOffView(View theView)
    {
        theView.BackgroundColor = Colors.Transparent;
    }
}
