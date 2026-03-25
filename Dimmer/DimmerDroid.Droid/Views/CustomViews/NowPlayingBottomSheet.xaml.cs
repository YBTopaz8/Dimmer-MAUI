global using Animation = Microsoft.Maui.Controls.Animation;

namespace Dimmer.Views.CustomViews;

public partial class NowPlayingBottomSheet : BottomSheet
{
	public NowPlayingBottomSheet()
	{
		InitializeComponent();
        MyViewModel = IPlatformApplication.Current!.Services.GetService<BaseViewModelAnd>();
        BindingContext = MyViewModel;
	}
    BaseViewModelAnd? MyViewModel { get;}

    SongModelView? songForLyrics;
    private async void LyricsChip_Tap(object sender, HandledEventArgs e)
    {
        if(MyViewModel != null)
        {
            songForLyrics = MyViewModel.SelectedSong;
            MyViewModel.SelectedSong = MyViewModel.CurrentPlayingSongView;
            await MyViewModel.SearchLyricsAsync();
        }
    }

  

    private void SongTitleLabel_SizeChanged(object sender, EventArgs e)
    {
        double startX = SongTitleLabel.Width;
        double endX = -SongTitleLabel.Width;

        //now marquee the text
        var animation = new Animation(v => SongTitleLabel.TranslationX = v, startX, endX);
        animation.Commit(this, "MarqueeAnimation", 16, 10000, Easing.Linear, (v, c) => SongTitleLabel.TranslationX = startX, () => true);
    }

    private void PlaybackChip_Tap(object sender, HandledEventArgs e)
    {

    }
}