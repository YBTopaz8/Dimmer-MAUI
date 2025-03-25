using System.Threading.Tasks;

namespace Dimmer_MAUI.Views.Desktop;

public partial class PlaylistsPageD : ContentPage
{
    public PlaylistVM MyViewModel { get; }

    public PlaylistsPageD(PlaylistVM vm)
    {
        InitializeComponent();
        BindingContext = vm;
        MyViewModel = vm;
    }
    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (MyViewModel.TemporarilyPickedSong is null)
        {
            return;
        }
        MyViewModel.CurrentPage = PageEnum.PlaylistsPage;
        MyViewModel.CurrentPageMainLayout = MainDock;
        
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        MyViewModel.SelectedPlaylist?.DisplayedSongsFromPlaylist?.Clear();
    }

    private void PlaySong_Tapped(object sender, TappedEventArgs e)
    {
        View send = (View)sender;
        SongModelView? song = (SongModelView)send.BindingContext;

        if (song is not null)
        {
            song.IsCurrentPlayingHighlight = false;
        }

        MyViewModel.PlaySong(song);
    }

    private void StateTrigger_IsActiveChanged(object sender, EventArgs e)
    {

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

                View? send = sender as View;
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
                supportedFilePaths = [];

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
            View? send = sender as View;
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
        supportedFilePaths ??= [];
        isAboutToDropFiles = false;
        MyViewModel.LoadLocalSongFromOutSideApp(supportedFilePaths);
        View? send = sender as View;
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

    private void AddToPlaylist_Tapped(object sender, TappedEventArgs e)
    {
        CreateNewPlayListPageBtmSheet.IsVisible = false;
        AddSongToPlayListPageBtmSheet.IsVisible = true;
    }
    private void ShowPlaylistCreationBtmPage_Clicked(object sender, EventArgs e)
    {
        AddSongToPlayListPageBtmSheet.IsVisible = false;
        CreateNewPlayListPageBtmSheet.IsVisible = true;
    }

    private void CancelAddSongToPlaylist_Clicked(object sender, EventArgs e)
    {
        
    }

    private void CancelCreateNewPlaylist_Clicked(object sender, EventArgs e)
    {
        CreateNewPlayListPageBtmSheet.IsVisible = false;
        AddSongToPlayListPageBtmSheet.IsVisible = true;
    }

    private void CreatePlaylistBtn_Clicked(object sender, EventArgs e)
    {
        //MyViewModel.CreatePlaylistAndAddSongCommand.Execute(NewPlaylistName.Text);
        //this.Close();
    }

    private void PlaylistsCV_ItemSelected(object sender, SelectedItemChangedEventArgs e)
    {

    }

    private void AddToPlayListGR_Tapped(object sender, TappedEventArgs e)
    {
        View send = (View)sender;
        PlaylistModelView playlist = (PlaylistModelView)send.BindingContext;
        if (MyViewModel.MySelectedSong is null)
        {
            return;
        }
        //MyViewModel.AddToPlaylist(playlist);

    }
    int CurrentIndex;
    private async void SortBtn_Clicked(object sender, EventArgs e)
    {

        //popup.Show();
        await MyViewModel.OpenSortingPopup();
    }

    private void ShowContextMenu_Clicked(object sender, EventArgs e)
    {
        ContextMenuView.IsVisible = true;

    }

    private void CloseContxtMenu_Clicked(object sender, EventArgs e)
    {
        ContextMenuView.IsVisible = false;

    }
    private void SfChipGroup_ChipClicked(object sender, EventArgs e)
    {
        SfChip ee = (Syncfusion.Maui.Toolkit.Chips.SfChip)sender;
        string? param = ee.CommandParameter.ToString();
        if (param is null)
        {
            return;
        }

        CurrentIndex =int.Parse(param);
        //popup.Dismiss();
    }
}