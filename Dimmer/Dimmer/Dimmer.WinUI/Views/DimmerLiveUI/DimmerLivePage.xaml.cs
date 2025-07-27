using ReactiveUI;

using Syncfusion.Maui.Toolkit.Carousel;

namespace Dimmer.WinUI.Views.DimmerLiveUI;

public partial class DimmerLivePage : ContentPage
{

    public DimmerLivePage(BaseViewModelWin viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        BindingContext = viewModel;

    }

    public BaseViewModelWin ViewModel { get; set; }
  
    private void OnSignUpClicked(object sender, EventArgs e)
    {

    }
    protected async override void OnAppearing()
    {
        base.OnAppearing();

     await   ViewModel.InitializeDimmerLiveData();
    }

    private void OnLoginClicked(object sender, EventArgs e)
    {

    }

    private void AcceptBtn_Clicked(object sender, EventArgs e)
    {

    }

    private void RejectBtn_Clicked(object sender, EventArgs e)
    {

    }
}