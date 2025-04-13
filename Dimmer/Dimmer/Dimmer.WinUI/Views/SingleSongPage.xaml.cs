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

    private async void MediaChipBtn_ChipClicked(object sender, EventArgs e)
    {
        SfChip ee = (Syncfusion.Maui.Toolkit.Chips.SfChip)sender;
        string? param = ee.CommandParameter.ToString();
        if (param is null)
        {
            return;
        }
        var CurrentIndex = int.Parse(param);
        switch (CurrentIndex)
        {
            case 0:
                MyViewModel.ToggleRepeatMode();
                break;
            case 1:
                await MyViewModel.PlayPrevious();
                break;
            case 2:
            case 3:
                await MyViewModel.PlayPauseSong();

                break;
            case 4:
                await MyViewModel.PlayNext();
                break;
            case 5:
                MyViewModel.IsShuffle = !MyViewModel.IsShuffle;
                break;

            case 6:
                MyViewModel.IncredeVolume();
                break;

            default:
                break;
        }
    }
    private async void CurrentPositionSlider_DragCompleted(object sender, EventArgs e)
    {
        await MyViewModel.SeekSongPosition(currPosPer: CurrentPositionSlider.Value);
    }

}