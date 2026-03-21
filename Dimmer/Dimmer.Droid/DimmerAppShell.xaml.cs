using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer;

public partial class DimmerAppShell : Shell
{
	public DimmerAppShell(BaseViewModelAnd baseVM)
	{
        InitializeComponent();
        MyViewModel = baseVM;
        BindingContext = MyViewModel;
        //Routing.RegisterRoute(nameof(HomePage), typeof(HomePage));
    }

    BaseViewModelAnd MyViewModel { get; }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (MyViewModel.ShowWelcomeScreen)
        {

        }
    }
    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);

    }

    protected override void OnNavigating(ShellNavigatingEventArgs args)
    {
        base.OnNavigating(args);

        //args.
    }

    protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
    {
        base.OnNavigatedFrom(args);
    }
    protected override void OnNavigatingFrom(NavigatingFromEventArgs args)
    {
        base.OnNavigatingFrom(args);
    }

    private void Shell_Loaded(object sender, EventArgs e)
    {

    }
}
