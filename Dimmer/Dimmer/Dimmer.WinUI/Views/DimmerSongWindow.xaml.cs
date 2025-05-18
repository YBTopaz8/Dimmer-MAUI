namespace Dimmer.WinUI.Views;

public partial class DimmerSongWindow : Window
{
    private readonly BaseViewModelWin MyViewModel;

    public DimmerSongWindow(BaseViewModelWin vm)
	{
		InitializeComponent();
		
		BindingContext=vm;
        this.MyViewModel=vm;
    }

    private async void ViewConverationGesture_Tapped(object sender, TappedEventArgs e)
    {
        await MyViewModel.OpenSpecificChatConversationCommand.ExecuteAsync((e.Parameter as string));
    }

    protected async override void OnCreated()
    {
        base.OnCreated();

        await MyViewModel.LoadOnlineData();

        this.MaximumHeight = 800;
        this.Width = 1200;
    }

    private async void ShareProfileBtn_Clicked(object sender, EventArgs e)
    {
        await MyViewModel.ShareProfile(BarCode);
    }

    private void ActualSongView_ChipClicked(object sender, EventArgs e)
    {

        SfChip ee = (SfChip)sender;
        string? param = ee.CommandParameter.ToString();
        if (param is null)
        {
            return;
        }
        var CurrentIndex = int.Parse(param);
        switch (CurrentIndex)
        {
            case 0:

                break;
            case 1:

                break;
            case 2:
                 PlatUtils.OpenAlbumWindow(MyViewModel.TemporarilyPickedSong!);

                break;

            default:
                break;
        }
    }
    private void CurrentPositionSlider_DragCompleted(object sender, EventArgs e)
    {
        var send = (Slider)sender;
        if (MyViewModel.IsPlaying)
        {
            MyViewModel.SeekTo(send.Value, true);
        }

    }
    private async void MediaChipBtn_ChipClicked(object sender, EventArgs e)
    {
        SfChip ee = (SfChip)sender;
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
                MyViewModel.PlayPrevious();
                break;
            case 2:
            case 3:
                await MyViewModel.PlayPauseAsync();

                break;
            case 4:
                MyViewModel.PlayNext(true);
                break;
            case 5:
                MyViewModel.IsShuffle = !MyViewModel.IsShuffle;
                break;

            case 6:
                MyViewModel.IncreaseVolume();
                break;

            default:

                break;
        }
    }

    private async void ViewProfile_Clicked(object sender, EventArgs e)
    {
        await SwitchUIs(0);
    }

    private async void ShareSongBtn_Clicked(object sender, EventArgs e)
    {
        await SwitchUIs(1);
        
    }


    private async Task SwitchUIs(int CurrentIndex)
    {
        Dictionary<int, View> viewss = new Dictionary<int, View>
        {
            {0, UserProfilePage},
            {1, ShareWindow},
            


        };
        if (!viewss.ContainsKey(CurrentIndex))
            return;

        await Task.WhenAll
            (viewss.Select(kvp =>
            kvp.Key == CurrentIndex
            ? kvp.Value.AnimateFadeInFront()
            : kvp.Value.AnimateFadeOutBack()));

    }

    private async void GetSharedSong_SearchButtonPressed(object sender, EventArgs e)
    {
        await MyViewModel.FetchSharedSongById(GetSharedSong.Text);
    }
}