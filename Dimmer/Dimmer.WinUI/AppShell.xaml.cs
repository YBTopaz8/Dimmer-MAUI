using Dimmer.DimmerLive;
using Dimmer.DimmerLive.Interfaces;
using Dimmer.DimmerSearch;
using Dimmer.Interfaces.Services.Interfaces;
using Dimmer.WinUI.Utils.WinMgt;
using Dimmer.WinUI.Views.DimmerLiveUI;
using Dimmer.WinUI.Views.WinUIPages;

using Realms;

using System.Threading.Tasks;


namespace Dimmer.WinUI;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();


        Routing.RegisterRoute(nameof(HomePage), typeof(HomePage));
        Routing.RegisterRoute(nameof(SingleSongPage), typeof(SingleSongPage));
        Routing.RegisterRoute(nameof(OnlinePageManagement), typeof(OnlinePageManagement));
        Routing.RegisterRoute(nameof(ArtistsPage), typeof(ArtistsPage));
        Routing.RegisterRoute(nameof(DimmerLivePage), typeof(DimmerLivePage));
        Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
        Routing.RegisterRoute(nameof(LibSanityPage), typeof(LibSanityPage));
        Routing.RegisterRoute(nameof(ExperimentsPage), typeof(ExperimentsPage));
        Routing.RegisterRoute(nameof(SocialView), typeof(SocialView));
        Routing.RegisterRoute(nameof(AllArtistsPage), typeof(AllArtistsPage));
        
        Routing.RegisterRoute(nameof(ChatView), typeof(ChatView));
        Routing.RegisterRoute(nameof(SessionTransferView), typeof(SessionTransferView));
    }

    protected async override void OnAppearing()
    {
        base.OnAppearing();

        MyViewModel= IPlatformApplication.Current!.Services.GetService<BaseViewModelWin>()!;
        this.BindingContext = MyViewModel;

        await MyViewModel.InitializeAllVMCoreComponentsAsync();
      
    }

    public BaseViewModelWin MyViewModel { get; internal set; }
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

    private async void OpenDimmerLiveSettingsChip_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(DimmerLivePage));
    }
    private void SettingsChip_Clicked(object sender, EventArgs e)
    {

        var winMgr = IPlatformApplication.Current!.Services.GetService<IWindowManagerService>()!;

        winMgr.GetOrCreateUniqueWindow(() => new SettingWin(MyViewModel));
        //await Shell.Current.GoToAsync(nameof(SettingsPage));
    }

    private async void NavTab_SelectionChanged(object sender, Syncfusion.Maui.Toolkit.TabView.TabSelectionChangedEventArgs e)
    {
        if (e.NewIndex == 1)
        {
            await MyViewModel.LoadUserLastFMInfo();
        }
    }

    private void RescanFolderPath_Clicked(object sender, EventArgs e)
    {
        var selectedFolder = (string)((ImageButton)sender).CommandParameter;
        MyViewModel.RescanFolderPath(selectedFolder);
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


    private void SettingsNavChips_ChipClicked(object sender, EventArgs e)
    {

    }

    private async void Logintolastfm_Clicked(object sender, EventArgs e)
    {

        await MyViewModel.LoginToLastfm();
    }

    private void FindDuplicatesBtn_Clicked(object sender, EventArgs e)
    {
        //this.NavTab.SelectedIndex = NavTab.Items.Count - 1;
    }
    private async void NavigateToSelectedSongPageContextMenuAsync(object sender, EventArgs e)
    {

        await MyViewModel.ProcessAndMoveToViewSong(null);
    }
    private  void TogglePanelClicked(object sender, PointerEventArgs e)
    {
        //var properties = e.PlatformArgs.PointerRoutedEventArgs.GetCurrentPoint(send).Properties;

        //var isXB1Pressed = properties.IsXButton1Pressed;

        //if (properties.IsXButton1Pressed)
        //{
        //    this.FlyoutIsPresented = !this.FlyoutIsPresented;
        //}
        //else if (properties.IsXButton2Pressed)
        //{

        //}

    }


    private void ChangeFolder_Clicked(object sender, EventArgs e)
    {

    }

    private async void FindDupes_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(LibSanityPage), true);
    }

    private void SfChip_Clicked(object sender, EventArgs e)
    {

    }

    private void SfChip_Clicked_1(object sender, EventArgs e)
    {

    }

    private async void QuickSearchSfChip_Clicked(object sender, EventArgs e)
    {
        var send = (SfChip)sender;
        var field = send.CommandParameter as string;
        if (field is null)
            return;
        string val = string.Empty;
        if (field is "artist")
        {
            char[] dividers = new char[] { ',', ';', ':', '|', '-' };

            var namesList = MyViewModel.CurrentPlayingSongView.OtherArtistsName
                .Split(dividers, StringSplitOptions.RemoveEmptyEntries) // Split by dividers and remove empty results
                .Select(name => name.Trim())                           // Trim whitespace from each name
                .ToArray();                                             // Convert to a List
            string res =string.Empty;
            if (namesList.Length>1)
            { 
              res   = await Shell.Current.DisplayActionSheet("Select Artist", "Cancel", null, namesList);

                if (string.IsNullOrEmpty(res) || res == "Cancel")
                {
                    return;
                }

            }
            if(namesList.Length==1)
            {
                res=namesList[0];
            }
            MyViewModel.SearchSongSB_TextChanged(StaticMethods.SetQuotedSearch("artist", res));

            return;
        }
        if (field is "title")
        {
            val = MyViewModel.CurrentPlayingSongView.Title;
        }
        if (field is "album")
        {
            val = MyViewModel.CurrentPlayingSongView.AlbumName;
        }
        if (field is "genre")
        {
            val = MyViewModel.CurrentPlayingSongView.GenreName;
        }
        if (field is "len")
        {
            val = MyViewModel.CurrentPlayingSongView.DurationInSeconds.ToString();
        }
        MyViewModel.SearchSongSB_TextChanged(StaticMethods.SetQuotedSearch(field, val));
    }


    private void ViewNPQ_Clicked(object sender, EventArgs e)
    {
        MyViewModel.SearchSongSB_TextChanged(MyViewModel.CurrentPlaybackQuery);
        return;



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

    private void ViewAllSongsWindow_Clicked(object sender, EventArgs e)
    {
       
    }

    private async void QuickFilterGest_PointerReleased(object sender, PointerEventArgs e)
    {
        var ee = e.PlatformArgs.PointerRoutedEventArgs.KeyModifiers;
       
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

            MyViewModel.SearchSongSB_TextChanged(
                StaticMethods.SetQuotedSearch("artist", selectedArtist));

            return;
        }

        MyViewModel.SearchSongSB_TextChanged(StaticMethods.SetQuotedSearch(field, val));

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


    private void AllLyricsColView_SelectionChanged(object sender, Microsoft.Maui.Controls.SelectionChangedEventArgs e)
    {
                var current = e.CurrentSelection as Dimmer.Data.ModelView.LyricPhraseModelView;

        var past = e.PreviousSelection as Dimmer.Data.ModelView.LyricPhraseModelView;

            if (past is not null)
            {

                past.NowPlayingLyricsFontSize = 25;


            }
        
            if (current != null)
            {


                    current.NowPlayingLyricsFontSize = 30;
                
            }
            AllLyricsColView.ScrollTo(item: current, ScrollToPosition.Start, animate: true);
        }
    

    private void Slider_Loaded(object sender, EventArgs e)
    {

    }

    private void VolumeSlider_Loaded(object sender, EventArgs e)
    {

    }

    private void VolumeSlider_Unloaded(object sender, EventArgs e)
    {

    }
    private void SfEffectsView_Loaded(object sender, EventArgs e)
    {
#if WINDOWS
        var send = (SfEffectsView)sender;
        var mainLayout = (Microsoft.UI.Xaml.UIElement)send.Handler!.PlatformView!;
        mainLayout.PointerWheelChanged += MainLayout_PointerWheelChanged;
#endif
    }

#if WINDOWS
    private void MainLayout_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        var pointerPoint = e.GetCurrentPoint(null);
        int mouseWheelDelta = pointerPoint.Properties.MouseWheelDelta;

        if (mouseWheelDelta != 0)
        {
            if (mouseWheelDelta > 0)
            {
                if (MyViewModel.DeviceVolumeLevel >=1)
                {
                    return;
                }
                MyViewModel.IncreaseVolumeLevel();
                // Handle scroll up
            }
            else
            {
                if (MyViewModel.DeviceVolumeLevel <= 0)
                {
                    return;
                }

                MyViewModel.DecreaseVolumeLevel();
                // Handle scroll down
            }
        }

        e.Handled = true;
    }

#endif

    private void SfEffectsView_Unloaded(object sender, EventArgs e)
    {
#if WINDOWS
        var send = (SfEffectsView)sender;
        Microsoft.UI.Xaml.UIElement? mainLayout = (Microsoft.UI.Xaml.UIElement)send.Handler!.PlatformView!;
        mainLayout.PointerWheelChanged -= MainLayout_PointerWheelChanged;
#endif

    }
}