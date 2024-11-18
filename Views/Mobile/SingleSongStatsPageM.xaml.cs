namespace Dimmer_MAUI.Views.Mobile;

public partial class SingleSongStatsPageM : ContentPage
{
	public SingleSongStatsPageM(HomePageVM homePageVM)
    {
        InitializeComponent();
        this.BindingContext = homePageVM;
        HomePageVM = homePageVM;

    }


    public HomePageVM HomePageVM { get; }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        HomePageVM.CurrentPage = PageEnum.FullStatsPage;
        HomePageVM.ShowSingleSongStatsCommand.Execute(HomePageVM.SongPickedForStats);
    }

  //  private async void CoverFlowV_ItemSwiped(CardsView view, PanCardView.EventArgs.ItemSwipedEventArgs args)
  //  {
  //      Debug.WriteLine(view.BindingContext.ToString());
  //      var w = view.ItemsSource;
        
		//var song = view.BindingContext as SingleSongStatistics;
  //      if (song is null)
  //      {
  //          return;
  //      }
  //      HomePageVM.ShowSingleSongStatsCommand.Execute(song.Song);

  //      LineChartBor.WidthRequest = LineChartBor.Width + 1;


  //      await Task.Delay(250);

  //      LineChartBor.WidthRequest = LineChartBor.Width - 1;
  //  }
}