using System.Linq.Dynamic.Core.Exceptions;
using System.Threading.Tasks;

namespace Dimmer;

public partial class AppShell : Shell
{
    public AppShell(BaseViewModel baseVM)
    {
        InitializeComponent();
        MyViewModel = baseVM;
        BindingContext = MyViewModel;
    }

    BaseViewModel MyViewModel { get; }

    protected override  void OnAppearing()
    {
        base.OnAppearing();
        if (MyViewModel.ShowWelcomeScreen)
        {

        }
    }
   
}
