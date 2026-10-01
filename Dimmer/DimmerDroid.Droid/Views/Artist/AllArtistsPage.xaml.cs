using DevExpress.Maui.Editors;
using Hqub.Lastfm.Entities;

namespace Dimmer.Views.Artist;

public partial class AllArtistsPage : ContentPage
{
	public AllArtistsPage(BaseViewModelAnd myViewModel)
	{
		InitializeComponent();
		MyViewModel = myViewModel;
		BindingContext = myViewModel;
	}
    public BaseViewModelAnd MyViewModel { get; }

    protected async override void OnAppearing()
    {
        base.OnAppearing();

        await Task.Delay(250);
        if (!MyViewModel.IsArtistInitialized)
        {
            MyViewModel.SetupArtistPipeline();
        }

        // Apply initial clean A-Z sort natively in DevExpress
        ApplyNativeSort(nameof(ArtistModelView.Name), DataSortOrder.Ascending);
    }



    private async void NavigateToArtistDetailsButton_Tapped(object sender, DevExpress.Maui.Core.DXTapEventArgs e)
    {
        
        DXButton send= (DXButton)sender;
        var artist = send.CommandParameter as ArtistModelView;

        MyViewModel.SetSelectedArtist(artist);
        await Shell.Current.GoToAsync(nameof(ArtistPage));


    }



    private async void ArtistSongsCV_Tap(object sender, DevExpress.Maui.CollectionView.CollectionViewGestureEventArgs e)
    {
        var song = e.Item as SongModelView;
        await MyViewModel.PlaySongAsync(song, CurrentPage.HomePage, MyViewModel.SearchResults);

    }

    private void AddSongToNextInPlaylist_Tap(object sender, DXTapEventArgs e)
    {
        DXButton send = (DXButton)sender;
        var song = send.CommandParameter as SongModelView;
        if(song is null)
            return;
        MyViewModel.AddToNext(new List<SongModelView> { song });
    }

    private void DXCollectionView_PullToRefresh(object sender, EventArgs e)
    {
        //MyViewModel.LoadAlbumAndArtistDetailsFromLastFM
    }


    // ==========================================================
    // 🔍 1. NATIVE DEVEXPRESS SEARCH FILTER
    // ==========================================================
    private void ArtistSearchEdit_TextChanged(object sender, EventArgs e)
    {
        string query = ArtistSearchEdit.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(query))
        {
            ArtistsCV.FilterString = string.Empty; // Instant reset
            return;
        }

        // Native DevExpress C++ Filter Expression (Case-insensitive contains)
        string escaped = query.Replace("'", "''");
        ArtistsCV.FilterString = $"Contains([Name], '{escaped}')";
    }

    // ==========================================================
    // 🏷️ 2. QUICK FILTER CHIPS (Favorites, Most Played)
    // ==========================================================
    private void FilterChips_SelectionChanged(object sender, EventArgs e)
    {
        if (ArtistsCV is null || !ArtistsCV.IsLoaded)
        {
            return;
        }
        var chipGroup = (ChoiceChipGroup)sender;
        string selected = chipGroup.SelectedItem?.ToString() ?? "";

        if (selected.Contains("Favorites"))
        {
            ArtistsCV.FilterString = "[IsFavorite] = True";
        }
        else if (selected.Contains("Most Played"))
        {
            ArtistsCV.FilterString = "[TotalCompletedPlays] > 0";
            ApplyNativeSort(nameof(ArtistModelView.TotalCompletedPlays), DataSortOrder.Descending);
        }
        else
        {
            ArtistsCV.FilterString = string.Empty;
            ApplyNativeSort(nameof(ArtistModelView.Name), DataSortOrder.Ascending);
        }
    }

    // ==========================================================
    // 🔃 3. PURE DEVEXPRESS NATIVE SORTING
    // ==========================================================
    private void OpenSortMenu_Clicked(object sender, EventArgs e) => SortBottomSheet.Show();

    private void SortAZ_Clicked(object sender, EventArgs e)
    {
        ApplyNativeSort(nameof(ArtistModelView.Name), DataSortOrder.Ascending);
        SortBottomSheet.Close();
    }

    private void SortZA_Clicked(object sender, EventArgs e)
    {
        ApplyNativeSort(nameof(ArtistModelView.Name), DataSortOrder.Descending);
        SortBottomSheet.Close();
    }

    private void SortMostPlayed_Clicked(object sender, EventArgs e)
    {
        ApplyNativeSort(nameof(ArtistModelView.TotalCompletedPlays), DataSortOrder.Descending);
        SortBottomSheet.Close();
    }

    private void SortMostSongs_Clicked(object sender, EventArgs e)
    {
        ApplyNativeSort(nameof(ArtistModelView.TotalSongsByArtist), DataSortOrder.Descending);
        SortBottomSheet.Close();
    }

    private void ApplyNativeSort(string propertyName, DataSortOrder sortOrder)
    {
        ArtistsCV.SortDescriptions.Clear();
        ArtistsCV.SortDescriptions.Add(new DevExpress.Maui.CollectionView.SortDescription
        {
            FieldName = propertyName,
            SortOrder = sortOrder
        });
    }



    private async void NavigateToArtistDetails_Clicked(object sender, EventArgs e)
    {
        var btn = (DXButton)sender;
        if (btn.CommandParameter is ArtistModelView artist)
        {
            MyViewModel.SetSelectedArtist(artist);
            await Shell.Current.GoToAsync(nameof(ArtistPage), true);
        }
    }



    private void AddSingleSongToNext_Clicked(object sender, EventArgs e)
    {
        var btn = (DXButton)sender;
        if (btn.CommandParameter is SongModelView song)
        {
            MyViewModel.AddToNext(new List<SongModelView> { song });
        }
    }



    private void ArtistHeader_Tapped(object sender, TappedEventArgs e)
    {
        
    }

    private void ArtistHeaderBtn_Tap(object sender, DXTapEventArgs e)
    {
        var send = (DXButton)sender;
        var artist = send.BindingContext as ArtistModelView;

        if (artist is null) return;

        
        
    }

    private void ArtistExpander_StateChanged(object sender, ValueChangedEventArgs<BottomSheetState> e)
    {

    }
    bool isLoadingArtist;
    private void ArtistsCV_TapConfirmed(object sender, DevExpress.Maui.CollectionView.CollectionViewGestureEventArgs e)
    {
       
    }

    private void ArtistsCV_Tap(object sender, DevExpress.Maui.CollectionView.CollectionViewGestureEventArgs e)
    {
        var artist = ArtistsCV.GetItem(e.ItemHandle) as ArtistModelView;

        if (artist is null) return;
        artist.IsLoadingArtist = true;
        // 2. Lazy load songs/albums only when expanded the first time!
        if ((artist.AlbumsByArtist == null || artist.AlbumsByArtist.Count == 0))
        {

            artist.RefreshAlbumAndSongsFromDB(MyViewModel.RealmFactory, IncludeSongsInAlbum: true);
        }
        artist.IsLoadingArtist = false;
        ArtistExpander.Show();
    }

    private void PlayAll_Clicked(object sender, EventArgs e)
    {

    }
}