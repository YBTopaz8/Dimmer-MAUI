using Dimmer.Utilities;

namespace Dimmer.Views.CustomViews;

public partial class ShareSongAsStoryPopup : BottomSheet
{
    BaseViewModelAnd MyViewModel; 
	public ShareSongAsStoryPopup(BaseViewModelAnd viewModel)
	{
		InitializeComponent();
		this.BindingContext = viewModel.SelectedSong;
	MyViewModel = viewModel;
    }



    byte[]? blurredBG;
    private async void ShareSongStory_StateChanged(object sender, ValueChangedEventArgs<BottomSheetState> e)
    {
        if (MyViewModel.SelectedSong == null) return;

        switch (e.NewValue)
        {
            case BottomSheetState.FullExpanded:
                 
                
                blurredBG = await ImageFilterUtils.ApplyFilterAsync(MyViewModel.SelectedSong.CoverImagePath, FilterType.DarkAcrylic);
                if (blurredBG is not null)
                {
                    this.BgBlurredImg.Source = ImageSource.FromStream(() => new MemoryStream(blurredBG));
                }
                
                break;
            case BottomSheetState.HalfExpanded:
                break;
            case BottomSheetState.Hidden:
                break;
            default:
                break;
        }
    }
}