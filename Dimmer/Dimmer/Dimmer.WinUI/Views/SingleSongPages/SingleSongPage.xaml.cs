using Dimmer.DimmerSearch;

using Microsoft.Maui.Platform;

using Syncfusion.Maui.Toolkit.Charts;

using System.Xml.Linq;

namespace Dimmer.WinUI.Views.SingleSongPages;

public partial class SingleSongPage : ContentPage
{
    public SingleSongPage(BaseViewModelWin vm)
    {
        InitializeComponent();
        BindingContext = vm;
        MyViewModel = vm;
        
    }

    public BaseViewModelWin MyViewModel { get; }

    protected async override void OnAppearing()
    {
        base.OnAppearing();
        MyViewModel.CurrentPageContext=CurrentPage.SingleSongPage;
        //await MyViewModel.LoadSongLastFMData();
        //await MyViewModel.LoadSongLastFMMoreData();

        _availableLayouts = new List<DataTemplate>
        {

            (DataTemplate)Resources["GridOfFour"]
        };
        await MyViewModel.LoadSelectedSongLastFMData();

    }
    private async void PlaySongGestRec_Tapped(object sender, EventArgs e)
    {
        var send = (Microsoft.Maui.Controls.View)sender;
        var song = MyViewModel.SelectedSong;
        if (song is null)
        {
            return;
        }
        if (MyViewModel.CurrentPlayingSongView == song)
        {
            await MyViewModel.PlayPauseToggle();
        }
        else
        {
            await MyViewModel.PlaySong(song);
        }
    }
    private List<DataTemplate> _availableLayouts;
    private int _currentLayoutIndex = 0;
    private void ChangeLayout_Clicked(object sender, EventArgs e)
    {
        // Move to the next layout index
        _currentLayoutIndex++;

        // If the index goes beyond the list of layouts, cycle back to the start
        if (_currentLayoutIndex >= _availableLayouts.Count)
        {
            _currentLayoutIndex = 0;
        }
        var tem = _availableLayouts[_currentLayoutIndex];
        // Apply the new layout to the CollectionView
        SongsColView.ItemTemplate = _availableLayouts[_currentLayoutIndex];
    }


    private void DataPointSelectionBehavior_SelectionChanging(object sender, Syncfusion.Maui.Toolkit.Charts.ChartSelectionChangingEventArgs e)
    {
        return;
        var ee = e.NewIndexes;
        var old = e.OldIndexes;

        var pie = sender as PieSeries;
        if (e.NewIndexes is null || e.NewIndexes.Count<1)
        {
            return;

        }
        var indexx = e.NewIndexes[0];
        var itemm = pie.ItemsSource as ObservableCollection<DimmerStats>;
        if (itemm is null)
        {
            return;
        }
        var songg = itemm[indexx];
        if (songg is null)
        {
            return;
        }
        if (songg.Song is null)
        {
            return;
        }
        var sss = MyViewModel.SearchResults.First(x => x.Id==songg.Song.Id);


        MyViewModel.SelectedSong=sss;
        //ColViewOfTopSongs.SelectedItem=       songg;
        Debug.WriteLine($"Selected Song: {songg.Song.Title}, Played {songg.Count} times.");

        //var actSong=((PieSeries)sender)
        Debug.WriteLine(ee);
        Debug.WriteLine(old);
    }

    private void ViewFileFolder_TouchDown(object sender, EventArgs e)
    {
        var song = MyViewModel.SelectedSong;
        if (song is not null && !string.IsNullOrWhiteSpace(song.FilePath) && System.IO.File.Exists(song.FilePath))
        {
            string argument = "/select, \"" + song.FilePath + "\"";
            System.Diagnostics.Process.Start("explorer.exe", argument);
        }
    }

    private async void AddNoteBtn_Clicked(object sender, EventArgs e)
    {
        var send = (Button)sender;
        var song = send.CommandParameter as SongModelView;
        if (song is null)
        {
            return;
        }
        await MyViewModel.SaveUserNoteToDbLegacy(song);
    }

    private async void ViewArtist_Clicked(object sender, EventArgs e)
    {
        // It's generally better practice to check for visibility before proceeding.
        if (SongView.IsVisible)
        {
            // If the SongView is already visible, maybe you want to do nothing or something else.
            // Based on your original logic, it seems you want to hide SongView and show ArtistView
            // when an artist is selected from the action sheet later.
            // The original return here might have been a bug if the intent was to always show the artist list.
        }

        var send = (SfChip)sender;
        if (send.CommandParameter is not SongModelView song)
            return; // Modern C# pattern matching

        var val = song.OtherArtistsName;
        if (string.IsNullOrWhiteSpace(val))
            return; // No artists to show

        char[] dividers = new char[] { ',', ';', ':', '|', '-' };

        var namesList = val
            .Split(dividers, StringSplitOptions.RemoveEmptyEntries) // Split by dividers
            .Select(name => name.Trim())                            // Trim whitespace from each name
            .Where(name => !string.IsNullOrWhiteSpace(name))        // Keep names that are NOT null or whitespace
            .ToArray();                                             // Convert to an array

        // If after all filtering there are no names, there is no need to show the action sheet.
        if (namesList.Length == 0)
        {
            return;
        }

        var selectedArtist = await Shell.Current.DisplayActionSheet("Select Artist", "Cancel", null, namesList);

        if (string.IsNullOrEmpty(selectedArtist) || selectedArtist == "Cancel")
        {
            return;
        }

        MyViewModel.SearchSongSB_TextChanged(StaticMethods.SetQuotedSearch("artist", selectedArtist));

        // You might want to ensure ArtistView is visible before this animation.
        if (!ArtistAlbumView.IsVisible)
        {
            await Task.WhenAll(SongView.DimmOutCompletelyAndHide(), ArtistAlbumView.DimmInCompletelyAndShow());
        }
    }

    private void SidePage_DragOver(object sender, DragEventArgs e)
    {
        
    }

    private void SidePage_Drop(object sender, DropEventArgs e)
    {
        var dd = e.Data;
        var s = dd.Properties.Values;
        var ss = dd.Properties.Keys;
    }

    private void SongDrag_DragStarting(object sender, DragStartingEventArgs e)
    {
        var dd = e.Data;
        var ee = dd.View;
        var re = dd.Properties;
        dd.Text = "Drop at the to view";
    }

    private async    void SongViewPointer_PointerExited(object sender, PointerEventArgs e)
    {
        var send = (View)sender;
        await send.FadeOut(300, 0.5);
    }

    private async void SongViewPointer_PointerEntered(object sender, PointerEventArgs e)
    {
        var send = (View)sender;
        await send.FadeIn(300, 0.3);
    }

    private async void ViewSongMFI_Clicked(object sender, EventArgs e)
    {
        var send = (MenuFlyoutItem)sender;
        var song = send.CommandParameter as SongModelView;

        await this.FadeOut(200, 0.7);
        MyViewModel.SelectedSong = song;
        await this.FadeIn(350, 1);

    }
    private async void OnAddQuickNoteClicked(object sender, EventArgs e)
    {
        var send = (MenuFlyoutItem)sender;
        var song = send.BindingContext as SongModelView;
        if (song is null)
        {
            return;
        }
        // Prompt the user for a note


        await MyViewModel.SaveUserNoteToDbLegacy(song);


    }
    private bool _isThrottling = false;
    private readonly int throttleDelay = 300; // Time in milliseconds
    private async void Slider_DragCompleted(object sender, EventArgs e)
    {
        var send = (Slider)sender;
        if (_isThrottling)
            return;

        _isThrottling = true;

        MyViewModel.SeekTrackPosition(send.Value);


        await Task.Delay(throttleDelay);
        _isThrottling = false;
    }

    private async void SongViewPointer_PointerPressed(object sender, PointerEventArgs e)
    {
        var send = (View)sender;
        var contxt = send.BindingContext as SongModelView;

        await this.FadeOut(200, 0.7);
        MyViewModel.SelectedSong = contxt;
        await this.FadeIn(350, 1);
    }
    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        if(MyViewModel.SelectedSong is null)
        {
            Debug.WriteLine("is null");
        }
        base.OnNavigatedTo(args);
    }

    private async void ViewSongDetails_Clicked(object sender, EventArgs e)
    {

        await Task.WhenAll(ArtistAlbumView.DimmOutCompletelyAndHide(), SongView.DimmInCompletelyAndShow());

    }

    private async void TopExpanderView_Expanded(object sender, Syncfusion.Maui.Toolkit.Expander.ExpandedAndCollapsedEventArgs e)
    {
        await RestOfLeftUI.FadeOut(300, 0.4);
    }

    private async void TopExpanderView_Collapsed(object sender, Syncfusion.Maui.Toolkit.Expander.ExpandedAndCollapsedEventArgs e)
    {
        await RestOfLeftUI.FadeIn(300, 1);
    }

    private void SearchSongOnline_Clicked(object sender, EventArgs e)
    {

    }

    private void SaveImageFromLastFM_Clicked(object sender, EventArgs e)
    {

    }

    private async void ToggleViewArtist_Clicked(object sender, EventArgs e)
    {
        if (!SongView.IsVisible)
        {
            await Task.WhenAll(SongView.DimmInCompletelyAndShow(), ArtistAlbumView.DimmOutCompletelyAndHide());
            return;
        }

        Button send = (Button)sender;
        var prop = send.Text;
        MyViewModel.SearchSongSB_TextChanged(StaticMethods.PresetQueries.ByArtist(prop));
        await Task.WhenAll(ArtistAlbumView.DimmInCompletelyAndShow(), SongView.DimmOutCompletelyAndHide());
    }

    private async void ToggleViewAlbum_Clicked(object sender, EventArgs e)
    {
        if (!SongView.IsVisible)
        {
            await Task.WhenAll(SongView.DimmInCompletelyAndShow(), ArtistAlbumView.DimmOutCompletelyAndHide());
            return;
        }

        Button send = (Button)sender;
        var prop = send.Text;
        MyViewModel.SearchSongSB_TextChanged(StaticMethods.PresetQueries.ByAlbum(prop)+ " " +StaticMethods.PresetQueries.SortByTitleAsc());
        await Task.WhenAll(ArtistAlbumView.DimmInCompletelyAndShow(), SongView.DimmOutCompletelyAndHide());
    }
}
