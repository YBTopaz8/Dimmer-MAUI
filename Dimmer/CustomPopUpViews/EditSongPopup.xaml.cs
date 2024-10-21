namespace Dimmer_MAUI.CustomPopUpViews;

public partial class EditSongPopup : Popup
{
    public EditSongPopup(HomePageVM homePageVM)
    {
        InitializeComponent();
        BindingContext = homePageVM;
        HomePageVM = homePageVM;
    }
    HomePageVM HomePageVM { get; set; }

    private async void CancelBtn_Clicked(object sender, EventArgs e)
    {
        await this.CloseAsync();
    }
  

}