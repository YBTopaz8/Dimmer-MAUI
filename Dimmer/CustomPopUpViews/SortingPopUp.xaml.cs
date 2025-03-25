//using Syncfusion.Maui.Toolkit.Chips;

namespace Dimmer_MAUI.CustomPopUpViews;

public partial class SortingPopUp : Popup
{
    public HomePageVM MyViewModel { get; }
    int CurrentIndex;
    public SortingPopUp(HomePageVM homePageVM, SortingEnum currentSort)
	{
		InitializeComponent();
        MyViewModel = homePageVM;
        this.BindingContext = homePageVM;
        CurrentIndex = (int)currentSort;

        //RBtnGroup.SelectedIndex = CurrentIndex;

        //this.ResultWhenUserTapsOutsideOfPopup = (SortingEnum)RBtnGroup.SelectedIndex;
    }
    private void CloseButton_Clicked(object sender, EventArgs e) => Close(null);
    private void OkButton_Clicked(object sender, EventArgs e) => Close(CurrentIndex);

    private void SfChipGroup_ChipClicked(object sender, EventArgs e)
    {
        SfChip ee = (Syncfusion.Maui.Toolkit.Chips.SfChip)sender;
        string? param = ee.CommandParameter.ToString();
        if (param is null)
        {
            return;
        }
        CurrentIndex =int.Parse(param);
    }
}