//using Dimmer.DimmerLive.Models;
using Dimmer.Data.Models;
using Dimmer.WinUI.Utils.Models;

using System.Diagnostics;
using System.Threading.Tasks;

using Application = Microsoft.Maui.Controls.Application;

namespace Dimmer.WinUI.Views;

public partial class HomePage : ContentPage
{
    public BaseViewModelWin MyViewModel { get; internal set; }
    public HomePage(BaseViewModelWin vm)
    {
        InitializeComponent();
        BindingContext = vm;
        MyViewModel=vm;

    }


}