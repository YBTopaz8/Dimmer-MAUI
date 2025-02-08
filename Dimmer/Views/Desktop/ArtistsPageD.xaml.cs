

using Syncfusion.Maui.Toolkit.EffectsView;

namespace Dimmer_MAUI.Views.Desktop;

public partial class ArtistsPageD : ContentPage
{
    public ArtistsPageD(HomePageVM homePageVM)
    {
        InitializeComponent();
        MyViewModel = homePageVM;
        this.BindingContext = homePageVM;
        MyViewModel.GetAllArtistsCommand.Execute(null);
    }

  
    public HomePageVM MyViewModel { get; }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (MyViewModel.SelectedAlbumOnArtistPage is not null)
        {
            MyViewModel.SelectedAlbumOnArtistPage.IsCurrentlySelected = false;
        }
        if (MyViewModel.SelectedArtistOnArtistPage is not null)
        {
            MyViewModel.SelectedArtistOnArtistPage.IsCurrentlySelected = false;
        }
       

        //AllAlbumsColView.SelectedItem = MyViewModel.SelectedAlbumOnArtistPage;

        MyViewModel.CurrentPage = PageEnum.AllArtistsPage;
        MyViewModel.CurrentPageMainLayout = MainDock;
        AllArtistsColView.SelectedItem = MyViewModel.SelectedArtistOnArtistPage;

        if (MyViewModel.MySelectedSong is null)
        {

            if (MyViewModel.TemporarilyPickedSong is not null)
            {
                MyViewModel.MySelectedSong = MyViewModel.TemporarilyPickedSong;
                MyViewModel.GetAllArtistsAlbum(song: MyViewModel.TemporarilyPickedSong, isFromSong: true);
                
            }
        }
        else
        {
            MyViewModel.GetAllArtistsAlbum();
        }
        if (MyViewModel.SelectedArtistOnArtistPage is not null)
        {
            AllArtistsColView.ScrollTo(MyViewModel.SelectedArtistOnArtistPage, null, ScrollToPosition.Center, false);
            AllArtistsColView.SelectedItem = MyViewModel.SelectedArtistOnArtistPage;
        }
        
        
        
    }

    private async void SetSongCoverAsAlbumCover_Clicked(object sender, EventArgs e)
    {
        var send = (MenuFlyoutItem)sender;
        var song = send.BindingContext as SongModelView;
        await MyViewModel.SetSongCoverAsAlbumCover(song!);
    }

    private void SearchBar_TextChanged(object sender, TextChangedEventArgs e)
    {
        MyViewModel.SearchArtistCommand.Execute(SearchArtistBar.Text);
    }
    private void ShowArtistAlbums_Tapped(object sender, TappedEventArgs e)
    {        
        var send = (View)sender;

        var curSel = send.BindingContext as AlbumModelView;
        MyViewModel.AllArtistsAlbumSongs=MyViewModel.GetAllSongsFromAlbumID(curSel!.LocalDeviceId);
        //await MyViewModel.GetAllAlbumInfos(curSel);
        //await MyViewModel.ShowSpecificArtistsSongsWithAlbum(curSel);
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

    private void PointerGestureRecognizer_PointerEntered(object sender, PointerEventArgs e)
    {
        var send = (Border)sender;
        var song = send.BindingContext! as SongModelView;
        MyViewModel.SetContextMenuSong(song!);
    }

    private void ImageButton_Clicked(object sender, EventArgs e)
    {
        MyViewModel.AllArtistsAlbumSongs = MyViewModel.GetAllSongsFromArtistID(MyViewModel.SelectedArtistOnArtistPage.LocalDeviceId);
    }


    ArtistModelView currentlySelectedArtist;
    private void ArtistView_TouchDown(object sender, EventArgs e)
    {
        SfEffectsView view = (SfEffectsView)sender;
        ArtistModelView artist = (view.BindingContext as ArtistModelView)!;

        //artist.IsCurrentlySelected = true;
        MyViewModel.GetAllArtistAlbumFromArtistModel(artist);

        //await MyViewModel.GetAllArtistAlbumFromArtist(artist);


        //var AlbumArtist = MyViewModel.AllLinks!.FirstOrDefault(x => x.ArtistId == artist.LocalDeviceId)!.AlbumId;
        //var album = MyViewModel.AllAlbums.FirstOrDefault(x => x.LocalDeviceId == AlbumArtist);
        //MyViewModel.GetAllArtistsAlbum(album: album, isFromSong: false);
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

}