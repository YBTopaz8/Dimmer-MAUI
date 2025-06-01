using Dimmer.WinUI.Views.WinuiWindows;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;

using SpringVector3NaturalMotionAnimation = Microsoft.UI.Composition.SpringVector3NaturalMotionAnimation;
using Window = Microsoft.UI.Xaml.Window;
using System.Numerics;

using Dimmer.WinUI.Utils.Models;
using Dimmer.WinUI.Utils.WinMgt;
using Dimmer.WinUI.Views.WinuiWindows;

using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;

using Vanara.Extensions.Reflection;

using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.UI.Composition;

using Button = Microsoft.UI.Xaml.Controls.Button;
using Page = Microsoft.UI.Xaml.Controls.Page;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views.ArtistsSpace;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class SpecificArtistPage : Page
{
    public SpecificArtistPage()
    {
        InitializeComponent();
    }
}
