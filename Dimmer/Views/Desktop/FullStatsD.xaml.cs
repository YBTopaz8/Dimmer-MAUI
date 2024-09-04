namespace Dimmer_MAUI.Views.Desktop;

public partial class FullStatsD : ContentPage
{
	public FullStatsD(HomePageVM homePageVM)
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
        HomePageVM.ShowGeneralTopTenSongsCommand.Execute(null);
    }

    //private void Calendar_SelectedDatesChanged(object sender, ValueChangedEventArgs<Collection<DateTime>> e)
    //{
    //    if (ToggleCalendar.IsChecked)
    //    {
    //        var send = (Xceed.Maui.Toolkit.Calendar)sender;
    //        HomePageVM.ShowTopTenSongsForSpecificDayCommand.Execute(send.SelectedDate);
    //    }
    //}
}