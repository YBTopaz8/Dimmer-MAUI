namespace Dimmer.Views;

public partial class DimmerVault : ContentPage
{
	public DimmerVault(BaseViewModelAnd vm)
    {
        InitializeComponent();
        this.MyViewModel = vm;
        BindingContext = vm;
    }
    BaseViewModelAnd MyViewModel { get; }


    protected override void OnAppearing()
    {
        base.OnAppearing();


    }
}