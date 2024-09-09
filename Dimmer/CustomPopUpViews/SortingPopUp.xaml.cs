namespace Dimmer_MAUI.CustomPopUpViews;

public partial class SortingPopUp : Popup
{
    public HomePageVM HomePageVM { get; }
    int CurrentIndex;
    public SortingPopUp(HomePageVM homePageVM, SortingEnum currentSort)
	{
		InitializeComponent();
        HomePageVM = homePageVM;
        this.BindingContext = homePageVM;
        CurrentIndex = (int)currentSort;

        RBtnGroup.SelectedIndex = CurrentIndex;

        this.ResultWhenUserTapsOutsideOfPopup = (SortingEnum)RBtnGroup.SelectedIndex;
    }
    private void CloseButton_Clicked(object sender, EventArgs e) => Close(null);
    private void OkButton_Clicked(object sender, EventArgs e) => Close(RBtnGroup.SelectedIndex);
    
}