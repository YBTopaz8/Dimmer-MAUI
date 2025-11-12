namespace Dimmer.WinUI;


public partial class AppShell : Shell
{
    public AppShell(BaseViewModel baseVM)
    {
        InitializeComponent();
        MyViewModel = baseVM;
        BindingContext = MyViewModel;
        Routing.RegisterRoute(nameof(HomePage), typeof(HomePage));
    }

    BaseViewModel MyViewModel { get; }

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

}
