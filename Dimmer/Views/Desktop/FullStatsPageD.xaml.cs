namespace Dimmer_MAUI.Views.Desktop;

public partial class FullStatsPageD : ContentPage
{
    public FullStatsPageD(HomePageVM homePageVM)
    {
        InitializeComponent();
        this.BindingContext = homePageVM;
        HomePageVM = homePageVM;
    }
    public HomePageVM HomePageVM { get; }

    protected override void OnAppearing()
    {
        if (HomePageVM.TemporarilyPickedSong is null)
        {
            return;
        }
        base.OnAppearing();
        HomePageVM.CurrentPage = PageEnum.FullStatsPage;
        HomePageVM.ShowGeneralTopXSongsCommand.Execute(null);
    }



  
}
//private void Calendar_SelectedDatesChanged(object sender, ValueChangedEventArgs<Collection<DateTime>> e)
//{
//    if (ToggleCalendar.IsChecked)
//    {
//        var send = (Xceed.Maui.Toolkit.Calendar)sender;
//        HomePageVM.ShowTopTenSongsForSpecificDayCommand.Execute(send.SelectedDate);
//    }
//}
