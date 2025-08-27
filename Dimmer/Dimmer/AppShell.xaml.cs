using System.Linq.Dynamic.Core.Exceptions;
using System.Threading.Tasks;

namespace Dimmer;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
    }
   
}
