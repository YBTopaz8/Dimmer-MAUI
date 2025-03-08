

using Syncfusion.Maui.Toolkit.EffectsView;

namespace Dimmer_MAUI.Views.Desktop;

public partial class AlbumsPageD : ContentPage
{
	public AlbumsPageD(HomePageVM homePageVM)
    {
        InitializeComponent();
        MyViewModel = homePageVM;
        BindingContext = homePageVM;
        
        MyViewModel.GetAllAlbums();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();        
        await Task.Delay(1500);
        Shell.Current.FlyoutIsPresented = false;
        MyViewModel.CurrentPage = PageEnum.AllAlbumsPage;
        MyViewModel.CurrentPageMainLayout = MainDock;
        MyViewModel.IsSearchBarVisible = false;

        if (MyViewModel.MySelectedSong is null)
        {
            if (MyViewModel.TemporarilyPickedSong is not null)
            {
                MyViewModel.MySelectedSong = MyViewModel.TemporarilyPickedSong;
                MyViewModel.LoadAllArtistsAlbumsAndLoadAnAlbumSong(song: MyViewModel.TemporarilyPickedSong, isFromSong: true);
            }
        }
        else
        {
            //MyViewModel.LoadAllArtistsAlbumsAndLoadAnAlbumSong();
        }

    }
    Border? SelectedBorderView { get; set; }
    private void ShowArtistAlbums_Tapped(object sender, TappedEventArgs e)
    {
        if (SelectedBorderView != null) 
        {
            SelectedBorderView.Stroke = Colors.Transparent;            
        }
        var send = (Border)sender;
        SelectedBorderView ??= send;
        AlbumModelView curSel = (send.BindingContext as AlbumModelView)!;
       
        SelectedBorderView.Stroke = Colors.DarkSlateBlue;

        MyViewModel.ReCheckSongsBelongingToAlbum(curSel.LocalDeviceId);  
        
    }

    public HomePageVM MyViewModel { get; }


    private void PointerGestureRecognizer_PointerEntered(object sender, PointerEventArgs e)
    {
        var send = (View)sender;
     
        var song = send.BindingContext! as SongModelView;
        MyViewModel.SetContextMenuSong(song!);

        send.BackgroundColor = Colors.DarkSlateBlue;
        
    }

    private void PointerGestureRecognizer_PointerExited(object sender, PointerEventArgs e)
    {
        var send = (View)sender;
        send.BackgroundColor = Microsoft.Maui.Graphics.Colors.Transparent;

    }

    //private void SongInAlbumFromArtistPage_TappedToPlay(object sender, TappedEventArgs e)
    //{
    //    MyViewModel.CurrentQueue = 1;
    //    var s = (View)sender;
    //    var song = s.BindingContext as SongModelView;
    //    MyViewModel.PlaySong(song);
    //}
    private void SongPointerGestureRecognizer_PointerEntered(object sender, PointerEventArgs e)
    {
        var send = (Border)sender;
        var song = send.BindingContext! as SongModelView;
        MyViewModel.SetContextMenuSong(song!);
    }

    private void SongInAlbumFromArtistPage_TappedToPlay(object sender, TappedEventArgs e)
    {
        MyViewModel.CurrentQueue = 1;
        var s = (View)sender;
        var song = s.BindingContext as SongModelView;
        MyViewModel.PlaySong(song!);
    }

    private void SfTabItem_Loaded(object sender, EventArgs e)
    {

    }

    //private void PointerGestureRecognizer_PointerEntered(object sender, PointerEventArgs e)
    //{
    //    var send = (Border)sender;
    //    var song = send.BindingContext! as SongModelView;
    //    MyViewModel.SetContextMenuSong(song!);
    //}



    private void DataPointSelectionBehavior_SelectionChanged(object sender, Syncfusion.Maui.Toolkit.Charts.ChartSelectionChangedEventArgs e)
    {
        var send = sender as DoughnutSeries;
      
        
    }

    private void TabView_SelectionChanged(object sender, Syncfusion.Maui.Toolkit.TabView.TabSelectionChangedEventArgs e)
    {

    }
    private void PlayNext_Clicked(object sender, EventArgs e)
    {
        var send = (MenuFlyoutItem)sender;
        var song = send.BindingContext as SongModelView;
        MyViewModel.AddNextInQueueCommand.Execute(song);
    }


    List<string> supportedFilePaths;
    bool isAboutToDropFiles = false;
    private async void DropGestureRecognizer_DragOver(object sender, DragEventArgs e)
    {
        try
        {

            if (!isAboutToDropFiles)
            {
                isAboutToDropFiles = true;

                var send = sender as View;
                if (send is null)
                {
                    return;
                }
                send.Opacity = 0.7;
#if WINDOWS
                var WindowsEventArgs = e.PlatformArgs.DragEventArgs;
                var dragUI = WindowsEventArgs.DragUIOverride;


                var items = await WindowsEventArgs.DataView.GetStorageItemsAsync();
                e.AcceptedOperation = DataPackageOperation.None;
                supportedFilePaths = new List<string>();

                if (items.Count > 0)
                {
                    foreach (var item in items)
                    {
                        if (item is Windows.Storage.StorageFile file)
                        {
                            /// Check file extension
                            string fileExtension = file.FileType.ToLower();
                            if (fileExtension != ".mp3" && fileExtension != ".flac" &&
                                fileExtension != ".wav" && fileExtension != ".m4a")
                            {
                                e.AcceptedOperation = DataPackageOperation.None;
                                dragUI.IsGlyphVisible = true;
                                dragUI.Caption = $"{fileExtension.ToUpper()} Files Not Supported";
                                continue;
                                //break;  // If any invalid file is found, break the loop
                            }
                            else
                            {
                                dragUI.IsGlyphVisible = false;
                                dragUI.Caption = "Drop to Play!";
                                Debug.WriteLine($"File is {item.Path}");
                                supportedFilePaths.Add(item.Path.ToLower());
                            }
                        }
                    }

                }
#endif
            }

        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
        //return Task.CompletedTask;
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
    private void SearchBar_TextChanged(object sender, TextChangedEventArgs e)
    {
        MyViewModel.SearchAlbum(SearchArtistBar.Text);
    }
    private void FirstLetterLabel_TouchDown(object sender, EventArgs e)
    {

    }
    private void SearchSongInAlbum_TextChanged(object sender, TextChangedEventArgs e)
    {
        MyViewModel.SearchSongFromArtistAlbumsSongsCommand.Execute(SearchSongInAlbum.Text);
    }
    string FilterLetter = string.Empty;
    private void ArtistLetterGestureRecog_Tapped(object sender, TappedEventArgs e)
    {
        var send = (Label)sender;
        var letter = send.Text;

        MyViewModel.FilterAlbumList(letter);
        FilterLetter = letter;
        FilterLetterLabel.Text = letter;
    }

    private void SfEffectsView_TouchDown(object sender, EventArgs e)
    {

    }
    private void ResetAlbumSongsList_Clicked(object sender, EventArgs e)
    {
        MyViewModel.LoadArtistAlbumsAndSongs(MyViewModel.SelectedAlbumOnAlbumPage);
    }

    private void ImageButton_Clicked(object sender, EventArgs e)
    {
        MyViewModel.AllArtistsAlbumSongs = MyViewModel.GetAllSongsFromAlbumID(MyViewModel.SelectedAlbumOnAlbumPage.LocalDeviceId);
    }
    private void PlaySong_Tapped(object sender, TappedEventArgs e)
    {
        var send = (View)sender;
        var song = (SongModelView)send.BindingContext;

        if (song is not null)
        {
            song.IsCurrentPlayingHighlight = false;
        }

        MyViewModel.PlaySong(song);
    }

    private void AlbumView_TouchDown(object sender, EventArgs e)
    {
        SfEffectsView view = (SfEffectsView)sender;
        AlbumModelView artist = (view.BindingContext as AlbumModelView)!;

        MyViewModel.LoadArtistAlbumsAndSongs(artist);


    }
}