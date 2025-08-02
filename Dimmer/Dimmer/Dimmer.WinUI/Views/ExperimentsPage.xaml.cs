namespace Dimmer.WinUI.Views;

public partial class ExperimentsPage : ContentPage
{
    public BaseViewModelWin MyViewModel { get; internal set; }
    public ExperimentsPage(BaseViewModelWin vm)
    {
        InitializeComponent();
        BindingContext = vm;
        MyViewModel = vm;
    }
    protected async override void OnAppearing()
    {
        base.OnAppearing();

        await MyViewModel.LoadUserLastFMDataAsync();
    }
}