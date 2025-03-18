#if WINDOWS
using DevExpress.Maui.CollectionView;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.UI.Xaml;
using System.Diagnostics;
using System.Threading.Tasks;

//using YB.MauiDataGridView;
using DragEventArgs = Microsoft.Maui.Controls.DragEventArgs;

//using TableViewColumnsCollection = YB.MauiDataGridView.TableViewColumnsCollection;
#endif

namespace Dimmer_MAUI.Views.Desktop;

public partial class MainPageD : ContentPage
{

    public ObservableCollection<MyDataItem> MyData { get; set; } = [];


    //only pass lazy to ctor if needed, else some parts mightn't work
    public MainPageD(Lazy<HomePageVM> homePageVM)
    {

        InitializeComponent();
        MyViewModel = homePageVM.Value;
        this.BindingContext = homePageVM.Value;
//#if WINDOWS
//        var dataGrid = new YB.MauiDataGridView.MauiDataGrid
//        {
//            ItemsSource = MyData,
//            AutoGenerateColumns = false,

//            SelectionMode = (Microsoft.UI.Xaml.Controls.ListViewSelectionMode)ListViewSelectionMode.Single,

//        };
//        // --- Create DataTemplate (in C#) ---
//        var cellTemplate = new Microsoft.Maui.Controls.DataTemplate(() =>
//        {
//            // Create a layout for each cell (e.g., HorizontalStackLayout)
//            var cellLayout = new HorizontalStackLayout
//            {
//                Spacing = 5,
//                Padding = new Microsoft.Maui.Thickness(5)
//            };

//            // Create Labels and bind them to the properties of MyDataItem
//            var titleLabel = new Label();
//            titleLabel.SetBinding(Label.TextProperty, new Binding(nameof(MyDataItem.Name))); // Bind to Title

//            var artistLabel = new Label();
//            artistLabel.SetBinding(Label.TextProperty, new Binding(nameof(MyDataItem.Age))); //Bind to Artist

//            // Add the Labels to the cell layout
//            cellLayout.Children.Add(titleLabel);
//            cellLayout.Children.Add(artistLabel);


//            // Return the root element of the cell template (the layout)
//            return cellLayout;
//        });
//        // --- Create Columns and Assign DataTemplate ---
//        var titleColumn = new TableViewTextColumn
//        {
//            Header = "Name",
//            //No Binding here, as template already specified
//            Width = new Microsoft.UI.Xaml.GridLength(1, Microsoft.UI.Xaml.GridUnitType.Star),

            
//            //CellTemplate = cellTemplate // Assign the DataTemplate
//        };
//        var artistColumn = new TableViewTextColumn
//        {
//            Header = "Name",
//            //No Binding here, as template already specified
//            Width = new Microsoft.UI.Xaml.GridLength(1, Microsoft.UI.Xaml.GridUnitType.Star),
//            //CellTemplate = cellTemplate // Assign the DataTemplate
//            Binding = new Microsoft.UI.Xaml.Data.Binding { Path = new Microsoft.UI.Xaml.PropertyPath("Name") }
//        };
//        TableViewColumnsCollection cols = new TableViewColumnsCollection();
//        cols.Add(titleColumn);
//        cols.Add(artistColumn);
//        dataGrid.Columns = cols;
        
//        // --- Add Sample Data ---   
//        MyData.Add(new MyDataItem { Name = "Song 1"});
//        MyData.Add(new MyDataItem { Name = "Song 2" });

//        // --- Create Layout ---
//        Content = new VerticalStackLayout
//        {
//            Children = { dataGrid }
//        };
//#endif
    }


    // Define a simple data class
    public class MyDataItem
    {
        public string? Name { get; set; }
        public int Age { get; set; }
    }
    HomePageVM MyViewModel { get; }

    bool isIniAssign;
    protected override async void OnAppearing()

    {
        base.OnAppearing();
        if (MyViewModel is null)
        {
            return;
        }
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

    List<string>? supportedFilePaths;
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
                supportedFilePaths = [];
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
    
    List<SongModelView>? selectedSongs;
    View? MouseSelectedView;

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
        supportedFilePaths ??= [];
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
    public bool ClickToPreview { get; set; } = true;

    private void DataPointSelectionBehavior_SelectionChanged(object sender, Syncfusion.Maui.Toolkit.Charts.ChartSelectionChangedEventArgs e)
    {
        var send = sender as PieSeries;
        var itemss = send.ItemsSource as ObservableCollection<DimmData>;

        var song = MyViewModel.DisplayedSongs.FirstOrDefault(X => X.LocalDeviceId == itemss[e.NewIndexes[0]].SongId);

        MyViewModel.MySelectedSong = song;
        if (ClickToPreview)
        {
            MyViewModel.PlaySong(song, true);
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
                    //MyViewModel.ToggleFlyout(); 
                    return;
                    MyViewModel.CalculateGeneralSongStatistics(MyViewModel.MySelectedSong.LocalDeviceId);
                    SongStatsView.IsVisible = !SongStatsView.IsVisible;
                   
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


    private void MyTable_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        Debug.WriteLine("single tap");
    }
#endif
 

    private void CntxtMenuChipGroup_ChipClicked(object sender, EventArgs e)
    {
        var ee = (Syncfusion.Maui.Toolkit.Chips.SfChip)sender;
        var param = ee.CommandParameter.ToString();
        if (param is null)
        {
            return;
        }
        switch (param)
        {
            case "0":
                MyViewModel.IsMultiSelectOn= false;
                break;
            case "1":
                MyViewModel.ToggleFlyout();
                //MyViewModel.AddNextInQueueCommand.Execute(MyViewModel.MySelectedSong);
                break;
            case "2":
                // share song
                //MyViewModel.AddToQueueCommand.Execute(MyViewModel.MySelectedSong);
                break;
                case "3":
                MyViewModel.IsContextMenuExpanded= true;
                
                // add to playlist
                break;
            case "4":
                // view Stats
                break;
            default:
                break;
        }
    }

    private void NewPlaylistEntry_TextChanged(object sender, TextChangedEventArgs e)
    {

    }

    private async void AddToPLBtn_Clicked(object sender, EventArgs e) //0 for create new, 1 for create new and, 2 for add to existing
    {
        var send = (Button)sender;
        var param = send.CommandParameter.ToString();
        if (param is null)
        {
            return;
        }
        switch (param)
        {
            case "0":

                

                var newPlaylist = new PlaylistModelView();
                newPlaylist.Name = NewPlaylistEntry.Text;
                if (string.IsNullOrEmpty(NewPlaylistEntry.Text))
                {
                    await Shell.Current.DisplayAlert("Error", "Please enter a name for the playlist", "OK");
                    return;
                }
                List<string?>? songIds = new();
                var songs = SongsColView.SelectedItems;
                
                foreach (var item in songs)
                {
                    var songId = ((SongModelView)item).LocalDeviceId;
                    songIds.Add(songId);
                }
                if (songIds.Count<1)
                {
                    await Shell.Current.DisplayAlert("Error", "No Songs Selected", "OK");
                    return;
                }
                MyViewModel.AddToPlaylist( newPlaylist, songIds);
                break;
            case "1":
                //MyViewModel.AddToPlaylist(NewPlaylistEntry.Text);
                break;
            default:
                break;
        }
    }
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