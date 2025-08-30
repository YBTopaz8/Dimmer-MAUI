using DevExpress.Maui.Controls;
using DevExpress.Maui.Editors;

using Dimmer.Data.Models;
using Dimmer.DimmerSearch.TQL;
using Dimmer.Utilities;

using System.ComponentModel;

namespace Dimmer.Views.CustomViewsParts;

public partial class QuickPanelBtmSheet : BottomSheet
{
	public QuickPanelBtmSheet()
	{
		InitializeComponent();
        var vm = IPlatformApplication.Current!.Services.GetService<BaseViewModelAnd>()??throw new NullReferenceException("BaseViewModelAnd is not registered in the service collection.");
        this.BindingContext =vm;

        this.MyViewModel =vm;

        // Initialize collections for live updates
        var realm = MyViewModel.RealmFactory.GetRealmInstance();
        _liveArtists = new ObservableCollection<string>(realm.All<ArtistModel>().AsEnumerable().Select(x => x.Name));
        _liveAlbums = new ObservableCollection<string>(realm.All<AlbumModel>().AsEnumerable().Select(x => x.Name));
        _liveGenres = new ObservableCollection<string>(realm.All<GenreModel>().AsEnumerable().Select(x => x.Name));

    }

    public ObservableCollection<string> _liveArtists;
    public ObservableCollection<string> _liveAlbums;
    public ObservableCollection<string> _liveGenres;

    public BaseViewModelAnd MyViewModel { get; set; }

    private void ByTitle()
    {
       

    }

    private void SearchBy_TextChanged(object sender, EventArgs e)
    {

            return;
        
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

        this.Close();
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
        MyViewModel.SetCurrentlyPickedSongForContext(paramss);

    }


    private async void GotoArtistBtn_Clicked(object sender, EventArgs e)
    {

        var song = MyViewModel.SelectedSong;
        if (song is null)
        {
            return;
        }
        await MyViewModel.SelectedArtistAndNavtoPage(song);

        await this.CloseAsync();

        await Shell.Current.GoToAsync(nameof(ArtistsPage), true);


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
        MyViewModel.CurrentSortProperty = sortProperty;


        SortOrder newOrder;

        // Toggle order if sorting by the same property again
        newOrder = (MyViewModel.CurrentSortOrder == SortOrder.Asc) ? SortOrder.Desc : SortOrder.Asc;


        MyViewModel.CurrentSortOrder = newOrder;
        MyViewModel.CurrentSortOrderInt = (int)newOrder;
        // Optional: Update UI to show sort indicators (e.g., change chip appearance)
        bool flowControl = SortIndeed();
        if (!flowControl)
        {
            return;
        }

        // Optional: Scroll to top after sorting
        // if (
        // {
        //     
        // }
    }

    private void AddToPlaylist_Clicked(object sender, EventArgs e)
    {
        var send = (DXButton)sender;
        var song = send.CommandParameter as SongModelView;
        var pl = MyViewModel.AllPlaylists;
        var listt = new List<SongModelView>();
        listt.Add(song);

        MyViewModel.AddToPlaylist("Playlists", listt, MyViewModel.CurrentTqlQuery);
    }

    private void CloseNowPlayingQueue_Tap(object sender, HandledEventArgs e)
    {

        Debug.WriteLine(this.Parent.GetType());
        //this.IsExpanded = !this.IsExpanded;

    }
    private async void DXButton_Clicked_3(object sender, EventArgs e)
    {

        await Shell.Current.GoToAsync(nameof(SingleSongPage));
        await this.CloseAsync();
    }
    SortOrder internalOrder = SortOrder.Asc;
    private bool SortIndeed()
    {
        internalOrder =  internalOrder== SortOrder.Asc ? SortOrder.Desc : SortOrder.Asc;

        MyViewModel.CurrentSortOrder = internalOrder;

        switch (MyViewModel.CurrentSortProperty)
        {
            case "Title":
                
                
                break;
            case "Artist": // Assuming CommandParameter is "Artist" for ArtistName
                
                
                break;
            case "Album": // Assuming CommandParameter is "Album" for AlbumName
                
                
                break;
            case "Genre":
                
                
                break;
            case "Duration":
                
                
                break;
            case "Year": // Assuming CommandParameter for ReleaseYear
                
                
                break;
            case "DateAdded": // Assuming CommandParameter for DateCreated
                
                break;
            default:
                System.Diagnostics.Debug.WriteLine($"Unsupported sort property: {MyViewModel.CurrentSortProperty}");
                // Reset sort state if property is unknown, or do nothing
                MyViewModel.CurrentSortProperty = string.Empty;
                MyViewModel.CurrentTotalSongsOnDisplay= songsToDisplay.Count;
                break;

        }
        MyViewModel.CurrentSortOrderInt = (int)MyViewModel.CurrentSortOrder;

        return true;
    }

    private void SortCategory_LongPress(object sender, HandledEventArgs e)
    {
        SortIndeed();
    }
    private void ByAll()
    {
      
    }
    private void ByArtist()
    {

    }

    private void Sort_Clicked(object sender, EventArgs e)
    {
        //SortBottomSheet.Show();
    }

    private void ArtistsChip_LongPress(object sender, HandledEventArgs e)
    {

    }

  
    private void DXButton_Clicked_1(object sender, EventArgs e)
    {

    }

    private void DXStackLayout_SizeChanged(object sender, EventArgs e)
    {

    }

    private void TextEdit_TextChanged(object sender, EventArgs e)
    {
 var send = (TextEdit)sender;

        MyViewModel.SearchSongSB_TextChanged(send.Text);
    }

    private void AutoCompleteEdit_TextChanged(object sender, DevExpress.Maui.Editors.AutoCompleteEditTextChangedEventArgs e)
    {
        var send = (AutoCompleteEdit)sender;
        var cursorPosition = send.CursorPosition;
        // Get suggestions based on the current text fragment
        var suggestions = AutocompleteEngine.GetSuggestions(
            _liveArtists, _liveAlbums, _liveGenres, send.Text, cursorPosition);
        send.ItemsSource = suggestions;
    

        MyViewModel.SearchSongSB_TextChanged(send.Text);
    }
    

    private void AutoCompleteEdit_SelectionChanged(object sender, EventArgs e)
    {

    }

    private void TextEdit_TextChanged_1(object sender, EventArgs e)
    {

    }

    private void SongsColView_Scrolled(object sender, DXCollectionViewScrolledEventArgs e)
    {

    }

    private void DXButton_Clicked(object sender, EventArgs e)
    {

        BtmSheetTab.SelectedItemIndex = 1;
        MyViewModel.ScrollToSongNowPlayingQueueCommand.Execute(null);
    }

    private void MainSongsColView_Loaded(object sender, EventArgs e)
    {
        MyViewModel.SongsColViewNPQ ??= SongsColView;

    }

    private void ViewSongOnly_TouchDown(object sender, EventArgs e)
    {
        var send = (Microsoft.Maui.Controls.View)sender;
        var song = (SongModelView)send.BindingContext;
        if (song is null)
        {
            return;
        }
        MyViewModel.SelectedSong = song;
        MyViewModel.SelectedSong = song;
        BtmSheetTab.SelectedItemIndex = 0;
    }

    private async void PlaySongClicked(object sender, EventArgs e)
    {
        var send = (DXButton)sender;
        var song = (SongModelView)send.BindingContext;
        await MyViewModel.PlaySong(song);
    }

    private void AlbumFilter_LongPress(object sender, HandledEventArgs e)
    {

    }

    private void MoreIcon_LongPress(object sender, HandledEventArgs e)
    {

    }

    private void MoreIcon_Tap(object sender, HandledEventArgs e)
    {

    }
}