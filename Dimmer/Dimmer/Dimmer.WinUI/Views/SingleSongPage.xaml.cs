namespace Dimmer.WinUI.Views;

public partial class SingleSongPage : ContentPage
{
    public SingleSongPage(BaseViewModelWin vm)
    {
        InitializeComponent();
        BindingContext = vm;
        MyViewModel=vm;

    }
    public BaseViewModelWin MyViewModel { get; internal set; }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {

        base.OnNavigatedTo(args);

    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        MyViewModel.SelectedSongOnPage= DeviceStaticUtils.SelectedSongOne;
        MyViewModel.LoadStatsForSong(MyViewModel.SelectedSongOnPage!);

    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        //MyViewModel.UnSetCollectionView();
    }

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

    private void MediaChipBtn_ChipClicked(object sender, EventArgs e)
    {
        //SfChip ee = (Syncfusion.Maui.Toolkit.Chips.SfChip)sender;
        //string? param = ee.CommandParameter.ToString();
        //if (param is null)
        //{
        //    return;
        //}
        //var CurrentIndex = int.Parse(param);
        //switch (CurrentIndex)
        //{
        //    case 0:
        //        MyViewModel.ToggleRepeatMode();
        //        break;
        //    case 1:
        //        MyViewModel.PlayPrevious();
        //        break;
        //    case 2:
        //    case 3:
        //        await MyViewModel.PlayPauseAsync();

        //        break;
        //    case 4:
        //        MyViewModel.PlayNext(true);
        //        break;
        //    case 5:
        //        MyViewModel.IsShuffle = !MyViewModel.IsShuffle;
        //        break;

        //    case 6:
        //        MyViewModel.IncreaseVolume();
        //        break;

        //    default:
        //        break;
        //}
    }
    private void CurrentPositionSlider_DragCompleted(object sender, EventArgs e)
    {
    }

    private void ToggleFav_Clicked(object sender, EventArgs e)
    {
        MyViewModel.ToggleFavSong();
    }
}