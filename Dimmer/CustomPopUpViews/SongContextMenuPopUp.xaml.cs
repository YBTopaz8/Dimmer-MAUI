namespace Dimmer_MAUI.CustomPopUpViews;

public partial class SongContextMenuPopUp : DXPopup
{
    public HomePageVM MyViewModel { get; }
    public SongContextMenuPopUp()
	{
		InitializeComponent();
        this.MyViewModel = IPlatformApplication.Current!.Services.GetService<HomePageVM>()!;
        BindingContext = MyViewModel;
    }


}