using Dimmer.DimmerSearch;

using Microsoft.Maui.Platform;

using Syncfusion.Maui.Toolkit.Charts;

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

        await MyViewModel.LoadSongLastFMData();
        await MyViewModel.LoadSongLastFMMoreData();
        MyViewModel.LoadStatsForSelectedSong(null);

        _availableLayouts = new List<DataTemplate>
        {

            (DataTemplate)Resources["GridOfFour"]
        };
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
        if (!SongView.IsVisible)
        {
            await Task.WhenAll(ArtistView.DimmOutCompletelyAndHide(), SongView.DimmInCompletelyAndShow());

            return;
        }

        var send = (SfChip)sender;
        var song = send.CommandParameter as SongModelView;
        var val = song.OtherArtistsName;
        char[] dividers = new char[] { ',', ';', ':', '|', '-' };

        var namesList = val
            .Split(dividers, StringSplitOptions.RemoveEmptyEntries) // Split by dividers and remove empty results
            .Select(name => name.Trim())                           // Trim whitespace from each name
            .ToArray();                                             // Convert to a List


        var res = await Shell.Current.DisplayActionSheet("Select Artist", "Cancel", string.Empty, namesList);

        if (string.IsNullOrEmpty(res))
        {
            return;
        }
        MyViewModel.SearchSongSB_TextChanged(StaticMethods.SetQuotedSearch("artist", res));
        await Task.WhenAll(SongView.DimmOutCompletelyAndHide(), ArtistView.DimmInCompletelyAndShow());
        return;
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
        await send.FadeOut(300, 0.3);
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
}