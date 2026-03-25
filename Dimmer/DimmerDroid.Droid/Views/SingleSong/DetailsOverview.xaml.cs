using Java.Interop;

namespace Dimmer.Views.SingleSong;

public partial class DetailsOverview : ContentPage
{
	public DetailsOverview(BaseViewModelAnd baseViewModel)
	{
		InitializeComponent();
		MyViewModel = baseViewModel;
		
	}
    public BaseViewModelAnd MyViewModel { get; }
    protected override bool  OnBackButtonPressed()
    {
        _= Shell.Current.GoToAsync("..");
        return base.OnBackButtonPressed();
    }
    protected override void OnAppearing()
    {
        base.OnAppearing();
        BindingContext = MyViewModel.SelectedSong;
    }
}