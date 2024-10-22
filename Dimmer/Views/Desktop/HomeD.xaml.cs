using Dimmer_MAUI.Utilities.OtherUtils;

namespace Dimmer_MAUI.Views.Desktop;

public partial class HomeD : UraniumContentPage
{
    public HomeD(HomePageVM homePageVM)
    {
        InitializeComponent();
        HomePageVM = homePageVM;
        this.BindingContext = homePageVM;

        MediaPlayBackCW.BindingContext = homePageVM;
    }

    public HomePageVM HomePageVM { get; }
    protected override void OnAppearing()
    {
        base.OnAppearing();
        HomePageVM.CurrentPage = PageEnum.MainPage;
        HomePageVM.AssignCV(SongsColView);
    }

    DateTime lastKeyStroke;
    private async void SearchSongSB_TextChanged(object sender, TextChangedEventArgs e)
    {
        lastKeyStroke = DateTime.Now;
        var thisKeyStroke = lastKeyStroke;
        await Task.Delay(750);
        if (thisKeyStroke == lastKeyStroke)
        {
            var searchText = e.NewTextValue;
            if (searchText.Length >= 2)
            {
                HomePageVM.SearchSongCommand.Execute(searchText);
            }
            else
            {
                HomePageVM.SearchSongCommand.Execute(string.Empty);

                await Task.Delay(500);
                if (SongsColView.IsLoaded)
                {
                    SongsColView.ScrollTo(HomePageVM.TemporarilyPickedSong, ScrollToPosition.Start, animate: true);
                }
            }
        }
    }

    private void ScrollToSong_Clicked(object sender, EventArgs e)
    {
        //var s = SongsColView.SelectedItem;
        try
        {
            if (HomePageVM.PickedSong is null)
            {
                HomePageVM.PickedSong = HomePageVM.TemporarilyPickedSong;
            }
            SongsColView.ScrollTo(HomePageVM.TemporarilyPickedSong, position: ScrollToPosition.Start, animate: false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Error when scrolling "+ex.Message);
        }
    }

    int coon;
    private void SongsColView_Loaded(object sender, EventArgs e)
    {
        Debug.WriteLine("refreshes " + coon++);
        if (SongsColView.IsLoaded)
        {
            SongsColView.ScrollTo(HomePageVM.TemporarilyPickedSong, null, ScrollToPosition.Center, animate: false);
            SongsColView.SelectedItem = HomePageVM.TemporarilyPickedSong;
        }
    }

    private async void DoubleTapToPlay_Tapped(object sender, TappedEventArgs e)
    {
        var t = (View)sender;
        HomePageVM.CurrentQueue = 0;
        
        var song = t.BindingContext as SongsModelView;
        HomePageVM.PlaySongCommand.Execute(song);
        
    }

    private void MenuFlyoutItem_Clicked(object sender, EventArgs e)
    {
        SearchSongSB.Focus();
    }

    
    private void SongsColView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SongsColView.IsLoaded && !isPointerEntered)
        {
            SongsColView.ScrollTo(HomePageVM.PickedSong, null, ScrollToPosition.Center, animate: false);
        }
        else
        {            
            if(currentSelectionMode == SelectionMode.Multiple)
            {
                HomePageVM.HandleMultiSelect(SongsColView, e);
            }
        }
    }




    SelectionMode currentSelectionMode;
   public void ToggleMultiSelect_Clicked(object sender, EventArgs e)
   {
        switch (SongsColView.SelectionMode)
        {
            case SelectionMode.None:
                SongsColView.SelectionMode = SelectionMode.Multiple;
                NormalMiniUtilBar.IsVisible = false;
                MultiSelectUtilBar.IsVisible = true;
                HomePageVM.EnableContextMenuItems = false;

                Debug.WriteLine("Now Multi Select");
                break;
            case SelectionMode.Single:
                break;
            case SelectionMode.Multiple:
                SongsColView.SelectionMode = SelectionMode.None;
                
                SongsColView.SelectedItems.Clear();
                HomePageVM.HandleMultiSelect(SongsColView);
                NormalMiniUtilBar.IsVisible = true;
                MultiSelectUtilBar.IsVisible = false;
                HomePageVM.EnableContextMenuItems = true;
                Debug.WriteLine("Back To None");
                break;
            default:
                break;
        }
        currentSelectionMode = SongsColView.SelectionMode;        
    }

    private void CancelMultiSelect_Clicked(object sender, EventArgs e)
    {
        ToggleMultiSelect_Clicked(sender, e);
    }

    protected override void OnNavigatingFrom(NavigatingFromEventArgs args)
    {
        base.OnNavigatingFrom(args);
        if (!isPointerEntered)
        {
            HomePageVM.SelectedSongToOpenBtmSheet = HomePageVM.TemporarilyPickedSong;
        }
    }

    private async void MenuFlyoutItem_Clicked_1(object sender, EventArgs e)
    {
        await HomePageVM.NavigateToArtistsPage(1);
    }

    bool isPointerEntered;
    private void PointerGestureRecognizer_PointerEntered(object sender, PointerEventArgs e)
    {
        var send = (View)sender;
        

        var song = send.BindingContext! as SongsModelView;
        isPointerEntered = true;

        HomePageVM.SetContextMenuSong(song);
    }

    private void PointerGestureRecognizer_PointerExited(object sender, PointerEventArgs e)
    {
        var send = (View)sender;
        isPointerEntered = false;
    }
    private async void PointerGestureRecognizer_PointerPressed(object sender, PointerEventArgs e)
    {
        var send = (View)sender;
        await send.AnimateHighlightPointerPressed(e);
    }

    private async void PointerGestureRecognizer_PointerReleased(object sender, PointerEventArgs e)
    {
        var send = (View)sender;
        await send.AnimateHighlightPointerReleased(e);
    }
}
