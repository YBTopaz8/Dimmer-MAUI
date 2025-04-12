namespace Dimmer.WinUI.Views;

public partial class SingleSongPage : ContentPage
{
    public SingleSongPageViewModel MyViewModel { get; }
    public SingleSongPage(SingleSongPageViewModel vm)
	{
		InitializeComponent();
		MyViewModel = vm;
        BindingContext = vm;

    }
    protected override void OnAppearing()
    {
        base.OnAppearing();
        MyViewModel.CurrentlySelectedPage = Utilities.Enums.CurrentPage.NowPlayingPage;
        
    }

}