using System.Diagnostics;

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

    protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
    {
        base.OnNavigatedFrom(args);
        HomePageVM.SwitchViewNowPlayingPageCommand.Execute(0);
        HomePageVM.IsOnLyricsSyncMode = false;
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

    private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        
        HomePageVM.CurrentQueue = 0;
        var t = (Grid)sender;
        var song = t.BindingContext as SongsModelView;
        HomePageVM.PlaySongCommand.Execute(song);
        
    }

    private void MenuFlyoutItem_Clicked(object sender, EventArgs e)
    {
        SearchSongSB.Focus();
    }

    bool isPointerEntered;
    private void PointerGestureRecognizer_PointerEntered(object sender, PointerEventArgs e)
    {
        var send = (Grid)sender;
        var song = send.BindingContext! as SongsModelView;
        HomePageVM.SetContextMenuSong(song);
        isPointerEntered = true;
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

    private void PointerGestureRecognizer_PointerExited(object sender, PointerEventArgs e)
    {
        isPointerEntered = false;
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


    Grid mainGrid { get; set; }
    private void GridOfItems_Loaded(object sender, EventArgs e)
    {
        mainGrid = sender as Grid;
    }

    private void CancelMultiSelect_Clicked(object sender, EventArgs e)
    {
        ToggleMultiSelect_Clicked(sender, e);
    }
}
