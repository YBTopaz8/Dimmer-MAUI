

using View = Microsoft.Maui.Controls.View;
using ImageButton = Microsoft.Maui.Controls.ImageButton;
using Dimmer.ViewsAndPages;

namespace Dimmer;

public partial class AppShell : Shell
{
    public AppShell(BaseViewModelAnd baseViewModel)
    {

        InitializeComponent();

        Routing.RegisterRoute(nameof(HomePage), typeof(HomePage));

        MyViewModel =baseViewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if ((MyViewModel is null))
        {
            return;
        }
        this.BindingContext = MyViewModel;
         MyViewModel.InitializeAllVMCoreComponentsAsync();
        
    }

    public BaseViewModelAnd MyViewModel { get; internal set; }
    private void SidePaneChip_Clicked(object sender, EventArgs e)
    {

        
    }

   


    private void ChangeFolder_Clicked(object sender, EventArgs e)
    {
        var selectedFolder = (string)((ImageButton)sender).CommandParameter;
        //await  MyViewModel.AddMusicFolderAsync(selectedFolder);
    }

    private void DeleteBtn_Clicked(object sender, EventArgs e)
    {
        var send = (ImageButton)sender;
        var param = send.CommandParameter.ToString();
        MyViewModel.DeleteFolderPath(param);
    }
    private async void AddNewMusicFolder_Clicked(object sender, EventArgs e)
    {
        await MyViewModel.AddMusicFolderViaPickerAsync();
    }

   
    private void NavBtnClicked_Clicked(object sender, EventArgs e)
    {
        var send = (Button)sender;
        var param = send.CommandParameter.ToString();
        switch (param)
        {
            case "0":
                break;
            case "1":
                break;
            default:

                break;
        }

    }

    private void ShowBtmSheet_Clicked(object sender, EventArgs e)
    {
    }

    
    private CancellationTokenSource _lyricsCts;
    private bool _isLyricsProcessing = false;


    private async void SettingsNavChips_ChipClicked(object sender, EventArgs e)
    {
        this.IsBusy=true;
    }

    private async void Logintolastfm_Clicked(object sender, EventArgs e)
    {

        await MyViewModel.LoginToLastfm();
    }
    private void FindDuplicatesBtn_Clicked(object sender, EventArgs e)
    {
    }

    private void FindDupes_Clicked(object sender, EventArgs e)
    {
    }

    private void ThemeToggleBtn_Clicked(object sender, EventArgs e)
    {

        var currentAppTheme = Application.Current?.UserAppTheme;
        if (currentAppTheme == AppTheme.Dark)
        {
            Application.Current?.UserAppTheme = AppTheme.Light;
        }
        else if (currentAppTheme == AppTheme.Light)
        {


            Application.Current?.UserAppTheme = AppTheme.Dark;
        }
        else if(currentAppTheme  == AppTheme.Unspecified)
        {
            Application.Current?.UserAppTheme = AppTheme.Light;
        }

    }

    private async void RescanLyrics_Clicked(object sender, EventArgs e)
    {
        // Cancel and dispose previous CTS if it exists
        if (_lyricsCts != null)
        {
            _lyricsCts.Cancel();
            _lyricsCts.Dispose();
        }
        _lyricsCts = new CancellationTokenSource();
        await MyViewModel.LoadSongDataAsync(null, _lyricsCts);
    }

    private void ToggleAppFlyoutState_Clicked(object sender, EventArgs e)
    {
        var currentState = this.FlyoutIsPresented;
        if (currentState)
        {
            this.FlyoutIsPresented = false;
            this.FlyoutBehavior = FlyoutBehavior.Flyout;
            //this.FlyoutWidth = 0; // Optionally set width to 0 to hide the flyout completely
        }
        else
        {
            this.FlyoutIsPresented = true;
            this.FlyoutBehavior = FlyoutBehavior.Flyout;
        }
    }

    private async void NavigateToSelectedSongPageContextMenuAsync(object sender, EventArgs e)
    {

        await MyViewModel.ProcessAndMoveToViewSong(MyViewModel.CurrentPlayingSongView);
    }

    private async void DXButton_Clicked(object sender, EventArgs e)
    {
    }

    private async void QuickFilterGest_PointerReleased(object sender, PointerEventArgs e)
    {
        var ee = e.PlatformArgs.MotionEvent.TouchMajor;
        var eew = e.PlatformArgs.MotionEvent.IsButtonPressed(MotionEventButtonState.Tertiary);
        var tt = e.PlatformArgs.MotionEvent.ActionMasked;
        var wtt = e.PlatformArgs.MotionEvent.ActionButton;
        var swtt = e.PlatformArgs.MotionEvent.Action;
        var cwtt = e.PlatformArgs.MotionEvent.Source;



        var send = (Microsoft.Maui.Controls.View)sender;
        var gest = send.GestureRecognizers[0] as PointerGestureRecognizer;
        if (gest is null)
        {
            return;
        }
        var field = gest.PointerReleasedCommandParameter as string;
        var val = gest.PointerPressedCommandParameter as string;
        if (field is "artist")
        {
            char[] dividers = new char[] { ',', ';', ':', '|', '-' };

            var namesList = val
                .Split(dividers, StringSplitOptions.RemoveEmptyEntries) // Split by dividers and remove empty results
                .Select(name => name.Trim())                           // Trim whitespace from each name
                .ToArray();                                             // Convert to a List


            var selectedArtist = await Shell.Current.DisplayActionSheet("Select Artist", "Cancel", null, namesList);

            if (string.IsNullOrEmpty(selectedArtist) || selectedArtist == "Cancel")
            {
                return;
            }
            MyViewModel.SearchSongForSearchResultHolder(TQlStaticMethods.SetQuotedSearch("artist", selectedArtist));



            return;
        }

        MyViewModel.SearchSongForSearchResultHolder(TQlStaticMethods.SetQuotedSearch(field, val));
    }

    private bool _isThrottling = false;
    private readonly int throttleDelay = 300; // Time in milliseconds
    private async void Slider_DragCompleted(object sender, EventArgs e)
    {
        var send = (DXSlider)sender;
        if (_isThrottling)
            return;

        _isThrottling = true;

        MyViewModel.SeekTrackPosition(send.Value);


        await Task.Delay(throttleDelay);
        _isThrottling = false;
    }

 

    private void ViewDeviceAudio_Clicked(object sender, EventArgs e)
    {
       

        //if (ShellTabView.SelectedIndex == 1)
        //{
        //    ShellTabView.SelectedIndex = 0;
        //    return;
        //}
        //ShellTabView.SelectedIndex = 1;
    }

    private void MoreIcon_Clicked(object sender, EventArgs e)
    {
       
    }

    private void SelectedSongChip_Clicked(object sender, EventArgs e)
    {

    }

    private void SelectedSongChip_TouchUp(object sender, EventArgs e)
    {

    }

    private void SfEffectsView_TouchUp(object sender, EventArgs e)
    {

    }

    private void AddFavoriteRatingToSong_TouchUp(object sender, EventArgs e)
    {

    }

    private void AddFavoriteRatingToSong_Loaded(object sender, EventArgs e)
    {

    }

    private void AddFavoriteRatingToSong_Unloaded(object sender, EventArgs e)
    {

    }

    private async void AddFavoriteRatingToSongPtrGest_PointerReleased(object sender, PointerEventArgs e)
    {

        var platEvents = e.PlatformArgs;
        var routedEvents = platEvents.MotionEvent;

        var ss = routedEvents.Pressure;

    }

    private async void ShowPlaylistHistory_Clicked(object sender, EventArgs e)
    {
        try
        {

        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }

    }

    private void NowPlayingSong_Clicked(object sender, EventArgs e)
    {

    }

    private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {

    }

    private void QuickFilterBtn_Clicked(object sender, EventArgs e)
    {

    }

    private void Quickalbumsearch_Clicked(object sender, EventArgs e)
    {

    }

    private void SetPrefdevice_Clicked(object sender, EventArgs e)
    {
        var send = (View)sender;
        var dev = send.BindingContext as AudioOutputDevice;

        if (dev is null)
            return;

        MyViewModel.SetPreferredAudioDevice(dev);
    }

    private void TogglePanelClicked(object sender, PointerEventArgs e)
    {

    }

    private void NowPlayingQueueGestRecog_PointerReleased(object sender, PointerEventArgs e)
    {

    }

    private void SfEffectsView_Loaded(object sender, EventArgs e)
    {

    }

    private void SfEffectsView_Unloaded(object sender, EventArgs e)
    {

    }

    private void VolumeSlider_Loaded(object sender, EventArgs e)
    {

    }

    private void VolumeSlider_Unloaded(object sender, EventArgs e)
    {

    }

    private void TrackProgressSlider_ValueChanged(object sender, EventArgs e)
    {

    }
    private async void ProgressSlider_TapReleased(object sender, DXTapEventArgs e)
    {
      
    }
    private async void TrackProgressSlider_TapReleased(object sender, DXTapEventArgs e)
    {
        var send = (DXSlider)sender;


        if (_isThrottling)
            return;

        _isThrottling = true;

        MyViewModel.SeekTrackPosition(send.Value);


        await Task.Delay(throttleDelay);
        _isThrottling = false;
    }
}