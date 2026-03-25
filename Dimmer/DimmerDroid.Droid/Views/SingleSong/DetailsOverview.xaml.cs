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


          private void SongTitleLabel_SizeChanged(object sender, EventArgs e)
    {
        double startX = TitleLabel.Width;
        double endX = -TitleLabel.Width;

        //now marquee the text
        var animation = new Animation(v => TitleLabel.TranslationX = v, startX, endX);
        animation.Commit(this, "MarqueeAnimation", 16, 10000, Easing.Linear, (v, c) => TitleLabel.TranslationX = startX, () => true);
    }

    private void LyricsTabVSL_Loaded(object sender, EventArgs e)
    {
        LyricsTabVSL.BindingContext = MyViewModel;
    }
}