namespace Dimmer.Views.Artist;

public partial class AllArtistsPage : ContentPage
{
	public AllArtistsPage(BaseViewModelAnd myViewModel)
	{
		InitializeComponent();
		MyViewModel = myViewModel;
	}
    public BaseViewModelAnd MyViewModel { get; }


}