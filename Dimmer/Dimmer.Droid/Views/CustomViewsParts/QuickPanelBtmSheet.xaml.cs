using DevExpress.Maui.Controls;

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
    }
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
        MyViewModel.BaseVM.SetCurrentlyPickedSongForContext(paramss);

    }


    private async void GotoArtistBtn_Clicked(object sender, EventArgs e)
    {

        var song = MyViewModel.BaseVM.SelectedSongForContext;
        if (song is null)
        {
            return;
        }
        await MyViewModel.BaseVM.SelectedArtistAndNavtoPage(song);

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
        // if (
        // {
        //     
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

        await Shell.Current.GoToAsync(nameof(SingleSongPage));
        await this.CloseAsync();
    }
    SortOrder internalOrder = SortOrder.Asc;
    private bool SortIndeed()
    {
        internalOrder =  internalOrder== SortOrder.Asc ? SortOrder.Desc : SortOrder.Asc;

        MyViewModel.BaseVM.CurrentSortOrder = internalOrder;

        switch (MyViewModel.BaseVM.CurrentSortProperty)
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
}