using Java.Interop;

namespace Dimmer.Views.SingleSong;

public partial class DetailsOverview : ContentPage
{
	public DetailsOverview(BaseViewModelAnd baseViewModel)
	{
		InitializeComponent();
		MyViewModel = baseViewModel;
		
		BindingContext = MyViewModel.SelectedSong;
	}
    public BaseViewModelAnd MyViewModel { get; }

}