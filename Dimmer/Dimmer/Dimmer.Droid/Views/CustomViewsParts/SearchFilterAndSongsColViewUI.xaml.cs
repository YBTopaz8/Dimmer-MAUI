using System.ComponentModel;

using Dimmer.Utilities;

namespace Dimmer.Views.CustomViewsParts;

public partial class SearchFilterAndSongsColViewUI : DXStackLayout
{
    public BaseViewModelAnd MyViewModel { get; internal set; }
    public SearchFilterAndSongsColViewUI()
    {
        var vm = IPlatformApplication.Current!.Services.GetService<BaseViewModelAnd>();
        InitializeComponent();
        MyViewModel=vm;
        BindingContext = vm;
    }
    private void SearchBy_TextChanged(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(SearchBy.Text))
        {
            ByAll();
            return;
        }
        switch (SearchParam)
        {
            case "Title":
                ByTitle();
                break;
            case "Artist":
                ByArtist();
                break;
            case "":
                ByAll();
                break;
            default:
                ByAll();
                break;
        }

    }

    private void ClosePopup(object sender, EventArgs e)
    {

        SongsMenuPopup.Close();
    }



    string SearchParam = string.Empty;

    SongModelView selectedSongPopUp = new SongModelView();
    private void MoreIcon_Clicked(object sender, EventArgs e)
    {
        var send = (DXButton)sender;
        var paramss = send.CommandParameter as SongModelView;
        if (paramss is null)
        {
            return;
        }
        selectedSongPopUp = paramss;
        MyViewModel.BaseVM.SetCurrentlyPickedSongForContext(paramss);
        SongsMenuPopup.Show();

    }


    private async void GotoArtistBtn_Clicked(object sender, EventArgs e)
    {

        var song = MyViewModel.BaseVM.SelectedSongForContext;
        if (song is null)
        {
            return;
        }
        await MyViewModel.BaseVM.SelectedArtistAndNavtoPage(song);

        await SongsMenuPopup.CloseAsync();
        await Shell.Current.GoToAsync(nameof(ArtistsPage), true);
    }

    private async void SongsColView_Tap(object sender, CollectionViewGestureEventArgs e)
    {
        var song = e.Item as SongModelView;
        MyViewModel.BaseVM.PlaySongFromList(song, SongsColView.ItemsSource as IEnumerable<SongModelView>);
        //AndroidTransitionHelper.BeginMaterialContainerTransform(this.RootLayout, HomeView, DetailView);
        //HomeView.IsVisible=false;
        //DetailView.IsVisible=true;

    }
    private void ByTitle()
    {
        if (!string.IsNullOrEmpty(SearchBy.Text))
        {
            if (SearchBy.Text.Length >= 1)
            {

                SongsColView.FilterString = $"Contains([Title], '{SearchBy.Text}')";
            }

        }
    }

    List<SongModelView> songsToDisplay = new();
    private void SortChoose_Clicked(object sender, EventArgs e)
    {

        var chip = sender as DXButton; // Or whatever your SfChip type is
        if (chip == null || chip.CommandParameter == null)
            return;

        string sortProperty = chip.CommandParameter.ToString();
        if (string.IsNullOrEmpty(sortProperty))
            return;


        // Update current sort state
        MyViewModel.BaseVM.CurrentSortProperty = sortProperty;


        SortOrder newOrder;

        // Toggle order if sorting by the same property again
        newOrder = (MyViewModel.BaseVM.CurrentSortOrder == SortOrder.Asc) ? SortOrder.Desc : SortOrder.Asc;


        MyViewModel.BaseVM.CurrentSortOrder = newOrder;
        MyViewModel.BaseVM.CurrentSortOrderInt = (int)newOrder;
        // Optional: Update UI to show sort indicators (e.g., change chip appearance)
        bool flowControl = SortIndeed();
        if (!flowControl)
        {
            return;
        }

        // Optional: Scroll to top after sorting
        // if (SongsColView.CurrentItems.Count > 0)
        // {
        //     SongsColView.ScrollTo(songs.FirstOrDefault(), ScrollToPosition.Start, true);
        // }
    }

    private void AddToPlaylist_Clicked(object sender, EventArgs e)
    {
        var send = (DXButton)sender;
        var song = send.CommandParameter as SongModelView;
        var pl = MyViewModel.BaseVM.AllPlaylists;
        var listt = new List<SongModelView>();
        listt.Add(song);

        MyViewModel.BaseVM.AddToPlaylist("Playlists", listt);
    }

    private void CloseNowPlayingQueue_Tap(object sender, HandledEventArgs e)
    {

        Debug.WriteLine(this.Parent.GetType());
        //this.IsExpanded = !this.IsExpanded;

    }
    private async void DXButton_Clicked_3(object sender, EventArgs e)
    {
        await this.SongsMenuPopup.CloseAsync();
        await Shell.Current.GoToAsync(nameof(SingleSongPage));
    }
    SortOrder internalOrder = SortOrder.Asc;
    private bool SortIndeed()
    {
        ObservableCollection<SongModelView> songs = SongsColView.ItemsSource as ObservableCollection<SongModelView>
        ;
        if (songs == null || !songs.Any())
            return false;
        internalOrder =  internalOrder== SortOrder.Asc ? SortOrder.Desc : SortOrder.Asc;

        MyViewModel.BaseVM.CurrentSortOrder = internalOrder;

        switch (MyViewModel.BaseVM.CurrentSortProperty)
        {
            case "Title":
                SongsColView.ItemsSource =   CollectionSortHelper.SortByTitle(songs, MyViewModel.BaseVM.CurrentSortOrder);
                songsToDisplay=SongsColView.ItemsSource as List<SongModelView> ?? new List<SongModelView>();
                break;
            case "Artist": // Assuming CommandParameter is "Artist" for ArtistName
                SongsColView.ItemsSource =    CollectionSortHelper.SortByArtistName(songs, MyViewModel.BaseVM.CurrentSortOrder);
                songsToDisplay=SongsColView.ItemsSource as List<SongModelView> ?? new List<SongModelView>();
                break;
            case "Album": // Assuming CommandParameter is "Album" for AlbumName
                SongsColView.ItemsSource =  CollectionSortHelper.SortByAlbumName(songs, MyViewModel.BaseVM.CurrentSortOrder);
                songsToDisplay=SongsColView.ItemsSource as List<SongModelView> ?? new List<SongModelView>();
                break;
            case "Genre":
                SongsColView.ItemsSource =   CollectionSortHelper.SortByGenre(songs, MyViewModel.BaseVM.CurrentSortOrder);
                songsToDisplay=SongsColView.ItemsSource as List<SongModelView> ?? new List<SongModelView>();
                break;
            case "Duration":
                SongsColView.ItemsSource =   CollectionSortHelper.SortByDuration(songs, MyViewModel.BaseVM.CurrentSortOrder);
                songsToDisplay=SongsColView.ItemsSource as List<SongModelView> ?? new List<SongModelView>();
                break;
            case "Year": // Assuming CommandParameter for ReleaseYear
                SongsColView.ItemsSource =   CollectionSortHelper.SortByReleaseYear(songs, MyViewModel.BaseVM.CurrentSortOrder);
                songsToDisplay=SongsColView.ItemsSource as List<SongModelView> ?? new List<SongModelView>();
                break;
            case "DateAdded": // Assuming CommandParameter for DateCreated
                SongsColView.ItemsSource = CollectionSortHelper.SortByDateAdded(songs, MyViewModel.BaseVM.CurrentSortOrder);
                songsToDisplay=SongsColView.ItemsSource as List<SongModelView> ?? new List<SongModelView>();
                break;
            default:
                System.Diagnostics.Debug.WriteLine($"Unsupported sort property: {MyViewModel.BaseVM.CurrentSortProperty}");
                // Reset sort state if property is unknown, or do nothing
                MyViewModel.BaseVM.CurrentSortProperty = string.Empty;
                MyViewModel.BaseVM.CurrentTotalSongsOnDisplay= songsToDisplay.Count;
                break;

        }
        MyViewModel.BaseVM.CurrentSortOrderInt = (int)MyViewModel.BaseVM.CurrentSortOrder;

        return true;
    }

    private void SortCategory_LongPress(object sender, HandledEventArgs e)
    {
        SortIndeed();
    }
    private void ByAll()
    {
        if (!string.IsNullOrEmpty(SearchBy.Text))
        {
            if (SearchBy.Text.Length >= 1)
            {
                SongsColView.FilterString =
                    $"Contains([Title], '{SearchBy.Text}') OR " +
                    $"Contains([ArtistName], '{SearchBy.Text}') OR " +
                    $"Contains([AlbumName], '{SearchBy.Text}')";
            }
            else
            {
                SongsColView.FilterString = string.Empty;
            }
        }
        else
        {
            SongsColView.FilterString = string.Empty;
        }
    }
    private void ByArtist()
    {
        if (!string.IsNullOrEmpty(SearchBy.Text))
        {
            if (SearchBy.Text.Length >= 1)
            {
                SongsColView.FilterString = $"Contains([ArtistName], '{SearchBy.Text}')";

            }
            else
            {
                SongsColView.FilterString = string.Empty;
            }
        }
    }

    private void Sort_Clicked(object sender, EventArgs e)
    {
        SortBottomSheet.Show();
    }

    private void ArtistsChip_LongPress(object sender, HandledEventArgs e)
    {

    }

    private void SongsColView_LongPress(object sender, CollectionViewGestureEventArgs e)
    {
        SongsColView.Commands.ShowDetailForm.Execute(null);
    }

    private void DXButton_Clicked_1(object sender, EventArgs e)
    {

    }

    private void SongsColView_Loaded(object sender, EventArgs e)
    {
        MyViewModel.LoadTheCurrentColView(SongsColView);
    }
}