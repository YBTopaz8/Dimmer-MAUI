#if WINDOWS
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.UI.Xaml;
using DragEventArgs = Microsoft.Maui.Controls.DragEventArgs;
//using TableViewColumnsCollection = YB.MauiDataGridView.TableViewColumnsCollection;
#endif

namespace Dimmer_MAUI.Views.Desktop;

public partial class MainPageD : ContentPage
{
#if WINDOWS
#endif
    //only pass lazy to ctor if needed, else some parts mightn't work
    public MainPageD(Lazy<HomePageVM> homePageVM)
    {
        InitializeComponent();
        MyViewModel = homePageVM.Value;
        this.BindingContext = homePageVM.Value;

    }
    public HomePageVM MyViewModel { get; }

    bool isIniAssign;
    protected override async void OnAppearing()

    {
        base.OnAppearing();
        MyViewModel.CurrentPage = PageEnum.MainPage;
        MyViewModel.CurrentPageMainLayout = MainDock;
        SongsColView.ItemsSource = MyViewModel.DisplayedSongs;

        if (SongsColView.ItemsSource is ICollection<SongModelView> itemssource && itemssource.Count != MyViewModel.DisplayedSongs?.Count)
        {
            SongsColView.ItemsSource = MyViewModel.DisplayedSongs;
        }
        if (!isIniAssign)
        {

            await MyViewModel.AssignCV(SongsColView);

            isIniAssign = true;
        }
        ScrollToSong_Clicked(this, EventArgs.Empty);


    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
    }

    private void ScrollToSong_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (MyViewModel.PickedSong is null || MyViewModel.TemporarilyPickedSong is null)
            {
                return;
            }

            if (SongsColView is null)
                return;
            SongsColView.ScrollTo(MyViewModel.TemporarilyPickedSong, position: ScrollToPosition.Start, animate: false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Error when scrolling " + ex.Message);
        }
    }

    private void SongsColView_Loaded(object sender, EventArgs e)
    {
        try
        {
            MyViewModel.DisplayedSongsColView = SongsColView;
            if (MyViewModel.PickedSong is null || MyViewModel.TemporarilyPickedSong is null)
            {
                return;
            }
            MyViewModel.PickedSong = MyViewModel.TemporarilyPickedSong;

            if (SongsColView is null)
                return;
            SongsColView.ScrollTo(MyViewModel.TemporarilyPickedSong, position: ScrollToPosition.Center, animate: false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Error when scrolling " + ex.Message);
        }
    }

    bool inRatingMode;
    private void ratingViewD_PointerEntered(object sender, PointerEventArgs e)
    {
        inRatingMode = true;
    }

    private void ratingViewD_PointerExited(object sender, PointerEventArgs e)
    {
        inRatingMode = false;
    }


    private void CancelMultiSelect_Clicked(object sender, EventArgs e)
    {
        MyViewModel.IsMultiSelectOn = false;
    }

    protected override void OnNavigatingFrom(NavigatingFromEventArgs args)
    {
        base.OnNavigatingFrom(args);
    }

    private async void NavToArtistClicked(object sender, EventArgs e)
    {
        await MyViewModel.NavigateToArtistsPage(1);
    }

    bool isPointerEntered;

    private void UserHoverOnSongInColView(object sender, PointerEventArgs e)
    {
        var send = (View)sender;
        var song = send.BindingContext! as SongModelView;
        MyViewModel.SetContextMenuSong(song!);
        send.BackgroundColor = Microsoft.Maui.Graphics.Colors.DarkSlateBlue;
        isPointerEntered = true;
    }

    private void UserHoverOutSongInColView(object sender, PointerEventArgs e)
    {
        var send = (View)sender;
        send.BackgroundColor = Microsoft.Maui.Graphics.Colors.Transparent;

    }

    List<string> supportedFilePaths;
    bool isAboutToDropFiles = false;


    private void DropGestureRecognizer_DragOver(object sender, DragEventArgs e)
    {
        try
        {
            if (!isAboutToDropFiles)
            {
                isAboutToDropFiles = true;

                // Use MAUI's DataPackageView, which is cross-platform.
                DataPackageView dataView = e.Data.View;

                e.AcceptedOperation = DataPackageOperation.None; // Default to None.
                supportedFilePaths = new List<string>();
                bool allFilesSupported = true;

                // Check if the DataPackage contains a list of files.
                // MAUI uses the "FileNames" key in the DataPackagePropertySetView
                // to store a list of file paths (as strings).

                if (dataView.Properties.ContainsKey("FileNames") &&
                    dataView.Properties["FileNames"] is IList<string> fileNames)
                {
                    foreach (string filePath in fileNames)
                    {
                        string fileExtension = Path.GetExtension(filePath).ToLowerInvariant();

                        if (fileExtension != ".mp3" && fileExtension != ".flac" &&
                            fileExtension != ".wav" && fileExtension != ".m4a")
                        {
                            allFilesSupported = false;
                        }
                        else
                        {
                            supportedFilePaths.Add(filePath.ToLowerInvariant());
                        }
                    }

                    if (allFilesSupported)
                    {
                        e.AcceptedOperation = DataPackageOperation.Copy;  // Or another appropriate value.

                        // You can't directly manipulate a "dragUI" object here
                        // like you did in the Windows-specific code.  MAUI handles
                        // the drag-and-drop UI visuals at a higher level.
                        // You *could* potentially influence the appearance by setting
                        // e.Data.Image, but standard platform behavior is preferred.
                    }
                    else
                    {
                        e.AcceptedOperation = DataPackageOperation.None;
                        // Similarly, you can't directly set a caption like "Files Not Supported".
                        // The platform handles this. If AcceptedOperation is None, the
                        // platform will typically show a "cannot drop" visual.
                    }
                }
                else
                {
                    e.AcceptedOperation = DataPackageOperation.None;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
        finally
        {
            isAboutToDropFiles = false; // Reset the flag.
        }
    }


    private void FavImagStatView_HoveredAndExited(object sender, EventArgs e)
    {
        var send = (View)sender;
        var song = send.BindingContext! as SongModelView;
        if (song is null)
            return;
        if (song.IsFavorite)
        {
            song.IsFavorite = false;
        }
        else
        {
            song.IsFavorite = true;
        }
    }


    private async void GoToSongOverviewClicked(object sender, EventArgs e)
    {
        await MyViewModel.NavToSingleSongShell();
    }


    SelectionMode currentSelectionMode;
    
    List<SongModelView> selectedSongs;
    View MouseSelectedView;

    private void SfEffectsView_TouchDown(object sender, EventArgs e)
    {
        MouseSelectedView = (View)sender;
        MyViewModel.SetContextMenuSong((SongModelView)(MouseSelectedView).BindingContext);
        if (MyViewModel.IsMultiSelectOn)
        {
            if(!SongsColView.SelectedItems.Contains(MyViewModel.MySelectedSong))
            {
                SongsColView.SelectedItems.Add(MyViewModel.MySelectedSong);
            }
            else
            {
                SongsColView.SelectedItems.Remove(MyViewModel.MySelectedSong);
            }
        }
    }

    private void PlaySong_Tapped(object sender, TappedEventArgs e)
    {
        if (MyViewModel.TemporarilyPickedSong is not null)
        {
            MyViewModel.TemporarilyPickedSong.IsCurrentPlayingHighlight = false;
        }
        if (MyViewModel.PickedSong is not null)
        {
            MyViewModel.PickedSong.IsCurrentPlayingHighlight = false;
        }


        var send = (View)sender;
        var song = (SongModelView)send.BindingContext;
        if (song is not null)
        {
            song.IsCurrentPlayingHighlight = false;
        }

        MyViewModel.PlaySong(song!);
    }

    private void SongsColView_RemainingItemsThresholdReached(object sender, EventArgs e)
    {
        if (MyViewModel.IsOnSearchMode)
        {
            return;
        }

    }

    private void SortBtn_Clicked(object sender, EventArgs e)
    {
        MyViewModel.OpenSortingPopupCommand.Execute(null);
    }

    private void Slider_OnValueChanged(object? sender, ValueChangedEventArgs e)
    {

    }

    private void SongInAlbumFromArtistPage_TappedToPlay(object sender, TappedEventArgs e)
    {
        MyViewModel.CurrentQueue = 1;
        var s = (Border)sender;
        var song = s.BindingContext as SongModelView;
        MyViewModel.PlaySong(song!);
    }

    private void PlayNext_Clicked(object sender, EventArgs e)
    {
        MyViewModel.AddNextInQueueCommand.Execute(MyViewModel.MySelectedSong);
    }

    private void ShowCntxtMenuBtn_Clicked(object sender, EventArgs e)
    {
        MyViewModel.ToggleFlyout();

        //await MyViewModel.ShowContextMenu(ContextMenuPageCaller.MainPage);
    }

    private void ToggleDrawer_Clicked(object sender, EventArgs e)
    {
        MyViewModel.ToggleFlyout();
    }

    private async void DropGestureRecognizer_Drop(object sender, DropEventArgs e)
    {
        supportedFilePaths ??= new();
        isAboutToDropFiles = false;
        MyViewModel.LoadLocalSongFromOutSideApp(supportedFilePaths);
        var send = sender as View;
        if (send is null)
        {
            return;
        }
        send.Opacity = 1;
        if (supportedFilePaths.Count > 0)
        {
            await send.AnimateRippleBounce();
        }
    }

    private void MainBody_Unloaded(object sender, EventArgs e)
    {
#if WINDOWS
        var send = sender as View;

        var mainLayout = (Microsoft.UI.Xaml.UIElement)send.Handler.PlatformView;
        
        mainLayout.PointerPressed -= S_PointerPressed;
#endif
    }
    private void MainBody_Loaded(object sender, EventArgs e)
    {

#if WINDOWS

        var send = sender as View;

        var mainLayout = (Microsoft.UI.Xaml.UIElement)send.Handler.PlatformView;
        
        mainLayout.PointerPressed += S_PointerPressed;
#endif
    }

    private void DropGestureRecognizer_DragLeave(object sender, DragEventArgs e)
    {
        try
        {
            isAboutToDropFiles = false;
            var send = sender as View;
            if (send is null)
            {
                return;
            }
            send.Opacity = 1;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }




#if WINDOWS
    private void MyTable_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {

    }


    private void MyTable_PropertyChanged(Microsoft.UI.Xaml.FrameworkElement sender, PropertyChangedEventArgs args)
    {

    }
    private void S_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        try
        {
            var nativeElement = this.Handler?.PlatformView as Microsoft.UI.Xaml.UIElement;
            if (nativeElement == null)
                return;

            var properties = e.GetCurrentPoint(nativeElement).Properties;

            if (properties != null)
            {
                Debug.WriteLine("Delta: " + properties.MouseWheelDelta);
                Debug.WriteLine("UPdate Kind: " + properties.PointerUpdateKind);
                Debug.WriteLine("Pressure: " + properties.Pressure);

                Debug.WriteLine("By the way! Use to detect keys like CTRL, SHFT etc.. " + e.KeyModifiers);
                if (properties.IsRightButtonPressed)
                {
                    MyViewModel.ToggleFlyout();

                    Debug.WriteLine("Right Mouse was Clicked!");
                }
                if (properties.IsXButton1Pressed)
                {
                    Debug.WriteLine("mouse 4 click!");
                }
                if (properties.IsXButton2Pressed)
                {
                    Debug.WriteLine("mouse 5!");
                }
                if (properties.IsEraser)
                {
                    Debug.WriteLine("eraser use!");

                }
                if (properties.IsMiddleButtonPressed)
                {
                    Debug.WriteLine("mouse wheel click!");
                }
                if (properties.IsHorizontalMouseWheel)
                {
                    Debug.WriteLine("idk..");
                }
            }

        }
        catch (Exception ex)
        {

            throw;
        }
        
    }

    private void S_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        throw new NotImplementedException();
    }

    private void S_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        throw new NotImplementedException();
    }

    private void S_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {


    }




    private void MyTable_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {

    }

    private void MyTable_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {

    }

    private void MyTable_PointerMoved(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {

    }

    private void MyTable_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {

    }

    private void MyTable_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        Debug.WriteLine("d d");
    }

    private void MyTable_KeyUp(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        Debug.WriteLine("k up");
    }

    private void MyTable_PointerReleased(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {

    }

    private void MyTable_PointerWheelChanged(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {

    }


    //private void MyTable_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
    //{
    //    if (MyViewModel.TemporarilyPickedSong is not null)
    //    {
    //        MyViewModel.TemporarilyPickedSong.IsCurrentPlayingHighlight = false;
    //    }
    //    if (MyViewModel.PickedSong is not null)
    //    {
    //        MyViewModel.PickedSong.IsCurrentPlayingHighlight = false;
    //    }

        
    //    FrameworkElement? send = (FrameworkElement)e.OriginalSource;
    //    var song = (SongModelView)send.DataContext;
    //    MyViewModel.MySelectedSong = song;
        
    //    //var song = (SongModelView)send.da;
    //    if (song is not null)
    //    {
    //        song.IsCurrentPlayingHighlight = false;
    //    }

    //    MyViewModel.PlaySong(song);
    //    MyTable.SelectedItem = song;
    //}

    //private void MyTable_RightTapped(object sender, Microsoft.UI.Xaml.Input.RightTappedRoutedEventArgs e)
    //{


    //    FrameworkElement? send = (FrameworkElement)e.OriginalSource;

    //    var song = (SongModelView)send.DataContext;
    //    MyViewModel.MySelectedSong = song;
    //    MyViewModel.ToggleFlyout();
    //    MyTable.SelectedItem = song;
    //    Debug.WriteLine("r tap");
    //}

    private void MyTable_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        Debug.WriteLine("single tap");
    }

#endif
}
public enum ContextMenuPageCaller
{
    MainPage,
    ArtistPage,
    AlbumPage,
    PlaylistPage,
    QueuePage,
    MiniPlaybackBar
}