using AndroidX.Lifecycle;

using Dimmer.DimmerSearch;
using Dimmer.ViewModel;
using Dimmer.Views.Stats;

using Syncfusion.Maui.Toolkit.Chips;

using System.Threading.Tasks;

using Button = Microsoft.Maui.Controls.Button;
using ImageButton = Microsoft.Maui.Controls.ImageButton;

namespace Dimmer;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(HomePage), typeof(HomePage));
        Routing.RegisterRoute(nameof(DimmerSettings), typeof(DimmerSettings));
        Routing.RegisterRoute(nameof(SearchSongPage), typeof(SearchSongPage));
        Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
        Routing.RegisterRoute(nameof(ArtistsPage), typeof(ArtistsPage));
        Routing.RegisterRoute(nameof(SingleSongPage), typeof(SingleSongPage));
        Routing.RegisterRoute(nameof(PlayHistoryPage), typeof(PlayHistoryPage));
        Routing.RegisterRoute(nameof(AnimationSettingsPage), typeof(AnimationSettingsPage));
        Routing.RegisterRoute(nameof(ChatView), typeof(ChatView));
        Routing.RegisterRoute(nameof(AlbumPage), typeof(AlbumPage));
        Routing.RegisterRoute(nameof(DimmerVault), typeof(DimmerVault));
    }

    protected async override void OnAppearing()
    {
        base.OnAppearing();

        MyViewModel  = IPlatformApplication.Current.Services.GetService<BaseViewModelAnd>();
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

        var send = (SfChip)sender;
        var param = send.CommandParameter.ToString();
        switch (param)
        {
            case "Artists":

                break;

            default:
                break;
        }

    }

   

    private async void NavTab_SelectionChanged(object sender, Syncfusion.Maui.Toolkit.TabView.TabSelectionChangedEventArgs e)
    {
        if (e.NewIndex == 1)
        {
            await MyViewModel.LoadUserLastFMInfo();
        }
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

    private void FirstTimeTabView_SelectionChanged(object sender, Syncfusion.Maui.Toolkit.TabView.TabSelectionChangedEventArgs e)
    {

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

    private void SettingsNavChips_SelectionChanged(object sender, Syncfusion.Maui.Toolkit.Chips.SelectionChangedEventArgs e)
    {

    }
    private CancellationTokenSource _lyricsCts;
    private bool _isLyricsProcessing = false;


    private async void SettingsNavChips_ChipClicked(object sender, EventArgs e)
    {
        this.IsBusy=true;
        await Shell.Current.GoToAsync(nameof(SettingsPage));
        this.IsBusy=false;
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

        await Shell.Current.GoToAsync(nameof(ChatView), true);
        this.FlyoutIsPresented = false;
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
            MyViewModel.SearchSongSB_TextChanged(StaticMethods.SetQuotedSearch("artist", selectedArtist));



            return;
        }

        MyViewModel.SearchSongSB_TextChanged(StaticMethods.SetQuotedSearch(field, val));
    }

    private bool _isThrottling = false;
    private readonly int throttleDelay = 300; // Time in milliseconds
    private async void Slider_DragCompleted(object sender, EventArgs e)
    {
        var send = (Slider)sender;
        if (_isThrottling)
            return;

        _isThrottling = true;

        MyViewModel.SeekTrackPosition(send.Value);


        await Task.Delay(throttleDelay);
        _isThrottling = false;
    }
}