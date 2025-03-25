namespace Dimmer_MAUI.CustomPopUpViews;

public partial class SongContextMenuPopupView : Popup
{
    public HomePageVM MyViewModel { get; }
    public SongContextMenuPopupView(HomePageVM homePageVM, SongModelView selectedSong)
    {
        InitializeComponent();
        MyViewModel = homePageVM;
        this.BindingContext = homePageVM;
        //RBtnGroup.SelectedIndex = CurrentIndex;

        //this.ResultWhenUserTapsOutsideOfPopup = (SortingEnum)RBtnGroup.SelectedIndex;
    }
    private void SfChipGroup_ChipClicked(object sender, EventArgs e)
    {
        SfChip ee = (Syncfusion.Maui.Toolkit.Chips.SfChip)sender;
        string? param = ee.CommandParameter.ToString();
        if (param is null)
        {
            return;
        }
        CurrentIndex = int.Parse(param);
        Close(CurrentIndex);

    }
    int CurrentIndex=0;
    private void CloseButton_Clicked(object sender, EventArgs e) => Close(null);
    private void OkButton_Clicked(object sender, EventArgs e) => Close(CurrentIndex);
}