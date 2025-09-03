using Dimmer.DimmerSearch;
using Dimmer.Utilities.CustomAnimations;

using Microsoft.Maui.Controls;

using Syncfusion.Maui.Toolkit.Charts;
using Syncfusion.Maui.Toolkit.Chips;

using Button = Microsoft.Maui.Controls.Button;
using View = Microsoft.Maui.Controls.View;

namespace Dimmer.Views;

public partial class SingleSongPage : ContentPage
{
    public SingleSongPage(BaseViewModelAnd vm)
    {
        InitializeComponent();
        BindingContext = vm;
        MyViewModel = vm;
    }
    public BaseViewModelAnd MyViewModel { get; internal set; }

    protected async override void OnAppearing()
    {
        base.OnAppearing();
        //MyViewModel.LoadStatsForSong(MyViewModel.SelectedSong!);
        await MyViewModel.LoadSelectedSongLastFMData();
        MyViewModel.CurrentPageContext=CurrentPage.SingleSongPage;
        //await MyViewModel.LoadSongLastFMData();
        //await MyViewModel.LoadSongLastFMMoreData();

        _availableLayouts = new List<DataTemplate>
        {

            (DataTemplate)Resources["GridOfFour"]
        };

    }
  
    private async void PlaySongGestRec_Tapped(object sender, EventArgs e)
    {
      
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
        //var send = (View)sender;
        //var song = send.CommandParameter as SongModelView;
        //if (song is null)
        //{
        //    return;
        //}
        await MyViewModel.SaveUserNoteToSong(MyViewModel.SelectedSong);
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

    private async void SongViewPointer_PointerExited(object sender, PointerEventArgs e)
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
        if (MyViewModel.SelectedSong is null)
        {
            Debug.WriteLine("is null");
        }
        base.OnNavigatedTo(args);
    }


    private void SearchSongOnline_Clicked(object sender, EventArgs e)
    {

    }

    private void SaveImageFromLastFM_Clicked(object sender, EventArgs e)
    {

    }

    private void SearchLyrics_Tapped(object sender, TappedEventArgs e)
    {

    }

    private void ToggleViewAlbum_Clicked(object sender, EventArgs e)
    {

    }
}