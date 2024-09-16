namespace Dimmer_MAUI.Views.Mobile;

public partial class SingleSongShell : ContentPage
{
	public SingleSongShell(HomePageVM homePageVM)
	{
		InitializeComponent();
        HomePageVM = homePageVM;
        BindingContext = homePageVM;
    }

    public HomePageVM HomePageVM { get; }

    private void TabV_SelectedTabChanged(object sender, UraniumUI.Material.Controls.TabItem e)
    {
        if (e != null && e.Title == "Stats")
        {
            HomePageVM.ShowSingleSongStatsCommand.Execute(HomePageVM.SelectedSongToOpenBtmSheet);
        }
    }
}