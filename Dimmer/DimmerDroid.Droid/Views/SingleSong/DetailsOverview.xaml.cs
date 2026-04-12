using DevExpress.Maui.Core;
using Java.Interop;

namespace Dimmer.Views.SingleSong;

public partial class DetailsOverview : ContentPage
{
	public DetailsOverview(BaseViewModelAnd baseViewModel, StatisticsViewModel statisticsService)
	{
		InitializeComponent();
		MyViewModel = baseViewModel;
        StatsViewModel= statisticsService;
		
	}
    public BaseViewModelAnd MyViewModel { get; }
    public StatisticsViewModel StatsViewModel { get; }
 

    protected async override void OnAppearing()
    {
        base.OnAppearing();
        BindingContext = MyViewModel.SelectedSong;
        StatisticsScrollV.BindingContext = StatsViewModel;

        if(MyViewModel.IsSearchingLyrics)
        {
            SongTabView.SelectedItemIndex = 1;
        }

        await StatsViewModel.LoadSongStatsAsync(MyViewModel.SelectedSong);
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

    private void SongTabView_PropertyChanging(object sender, Microsoft.Maui.Controls.PropertyChangingEventArgs e)
    {
        var propName = e.PropertyName;
        if(propName == nameof(SongTabView.SelectedItemIndex))
        {
            if(SongTabView.SelectedItemIndex==0)
            {
                MyViewModel.ReadySearchViewAndProduceSearchText();
            }
        }
    }

    private void MiniNPExpander_Loaded(object sender, EventArgs e)
    {
        MiniNPExpander.BindingContext = MyViewModel;
    }

    private void StatisticsScrollView_Loaded(object sender, EventArgs e)
    {
        DXScrollView dXScroll=(DXScrollView)sender;
        dXScroll.BindingContext = StatsViewModel;
    }

    private void Label_Loaded(object sender, EventArgs e)
    {

    }
}