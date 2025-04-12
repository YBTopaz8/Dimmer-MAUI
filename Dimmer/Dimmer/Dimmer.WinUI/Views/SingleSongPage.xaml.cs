using Syncfusion.Maui.Toolkit.Chips;

namespace Dimmer.WinUI.Views;

public partial class SingleSongPage : ContentPage
{
    public SingleSongPageViewModel MyViewModel { get; }
    public SingleSongPage(SingleSongPageViewModel vm)
    {
        InitializeComponent();
        MyViewModel = vm;
        BindingContext = vm;

    }
    protected override void OnAppearing()
    {
        base.OnAppearing();
        MyViewModel.CurrentlySelectedPage = Utilities.Enums.CurrentPage.NowPlayingPage;

    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        MyViewModel.UnSetCollectionView();
    }

    private async void LyricsColView_SelectionChanged(object sender, Microsoft.Maui.Controls.SelectionChangedEventArgs e)
    { }
    private void SfChipGroup_ChipClicked(object sender, EventArgs e)
    {
        SfChip ee = (Syncfusion.Maui.Toolkit.Chips.SfChip)sender;
        string? param = ee.CommandParameter.ToString();
        if (param is null)
        {
            return;
        }

        // TODO CALL METHOD TO SWITCH UI FROM VM
        // =int.Parse(param); switch view
    }

}