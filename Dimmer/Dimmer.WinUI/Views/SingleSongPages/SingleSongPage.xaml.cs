using Dimmer.DimmerSearch;

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
        MyViewModel.CurrentPageContext=CurrentPage.SingleSongPage;
        //await MyViewModel.LoadSongLastFMData();
        //await MyViewModel.LoadSongLastFMMoreData();

        _availableLayouts = new List<DataTemplate>
        {

            (DataTemplate)Resources["GridOfFour"]
        };
        await MyViewModel.LoadSelectedSongLastFMData();

    }
    private List<DataTemplate> _availableLayouts;
    private int _currentLayoutIndex = 0;
    private void ChangeLayout_Clicked(object sender, EventArgs e)
    {

        _currentLayoutIndex++;


        if (_currentLayoutIndex >= _availableLayouts.Count)
        {
            _currentLayoutIndex = 0;
        }
        var tem = _availableLayouts[_currentLayoutIndex];

        SongsColView.ItemTemplate = _availableLayouts[_currentLayoutIndex];
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
        await MyViewModel.SaveUserNoteToSong(song);
    }

    private async void ViewArtist_Clicked(object sender, EventArgs e)
    {
        
        if (SongTabView.IsVisible)
        {
            
            
            
            
        }

        var send = (SfChip)sender;
        if (send.CommandParameter is not SongModelView song)
            return; 

        var val = song.OtherArtistsName;
        if (string.IsNullOrWhiteSpace(val))
            return; 

        char[] dividers = new char[] { ',', ';', ':', '|', '-' };

        var namesList = val
            .Split(dividers, StringSplitOptions.RemoveEmptyEntries) 
            .Select(name => name.Trim())                            
            .Where(name => !string.IsNullOrWhiteSpace(name))        
            .ToArray();                                             

        
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

        
        if (!ArtistAlbumView.IsVisible)
        {
            await Task.WhenAll(SongTabView.DimmOutCompletelyAndHide(), ArtistAlbumView.DimmInCompletelyAndShow());
        }
    }

    private void OnLabelClicked(object sender, EventArgs e)
    {

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
        await send.FadeOut(300, 0.7);
    }

    private async void SongViewPointer_PointerEntered(object sender, PointerEventArgs e)
    {
        //if (MyViewModel.SelectedSong is null)
        //{
        
        //}
        var send = (View)sender;
        var songBindingContext = send.BindingContext as SongModelView;
        if (songBindingContext is null)
        {
            return;
        }
        MyViewModel.SelectedSong = songBindingContext;
        await send.FadeIn(300, 1);
    }

    private async void ViewSongMFI_Clicked(object sender, EventArgs e)
    {
        var send = (MenuFlyoutItem)sender;
        var song = send.CommandParameter as SongModelView;
        if (song is null)
        {
            return;
        }
        await this.FadeOut(200, 0.7);
        MyViewModel.SelectedSong = song;
        

            await Task.WhenAll(SongTabView.DimmInCompletelyAndShow(), ArtistAlbumView.DimmOutCompletelyAndHide());

            await MyViewModel.LoadSelectedSongLastFMData();
        
        await this.FadeIn(450, 1);

    }
    private async void OnAddQuickNoteClicked(object sender, EventArgs e)
    {
        var send = (MenuFlyoutItem)sender;
        var song = send.BindingContext as SongModelView;
        if (song is null)
        {
            return;
        }
        


        await MyViewModel.SaveUserNoteToSong(song);


    }
    private bool _isThrottling = false;
    private readonly int throttleDelay = 300; 
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
        if (!SongTabView.IsVisible)
        {
            await Task.WhenAll(SongTabView.DimmInCompletelyAndShow(), ArtistAlbumView.DimmOutCompletelyAndHide());

            await MyViewModel.LoadSelectedSongLastFMData();
            return;
        }
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

        await Task.WhenAll(ArtistAlbumView.DimmOutCompletelyAndHide(), SongTabView.DimmInCompletelyAndShow());

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
        
        if (!SongTabView.IsVisible)
        {
            await Task.WhenAll(SongTabView.DimmInCompletelyAndShow(), ArtistAlbumView.DimmOutCompletelyAndHide());

            await MyViewModel.LoadSelectedSongLastFMData();
            return;
        }

        

        
        songToRestore = MyViewModel.SelectedSong;

        
        if (songToRestore is null)
            return;

        
        
        Button send = (Button)sender;
        var artistName = send.Text;
        await Task.WhenAll(ArtistAlbumView.DimmInCompletelyAndShow(), SongTabView.DimmOutCompletelyAndHide());

        MyViewModel.SearchSongSB_TextChanged(StaticMethods.PresetQueries.ByArtist(artistName));

        
      
        
        
        
        if (MyViewModel.SearchResults.Contains(songToRestore))
        {
            MyViewModel.SelectedSong = songToRestore;
        }
    }
    SongModelView? songToRestore { get; set; }
    private async void ToggleViewAlbum_Clicked(object sender, EventArgs e)
    {
  
        
        if (!SongTabView.IsVisible)
        {
            await Task.WhenAll(SongTabView.DimmInCompletelyAndShow(), ArtistAlbumView.DimmOutCompletelyAndHide());

            await MyViewModel.LoadSelectedSongLastFMData();
            return;
        }

        

        
        songToRestore = MyViewModel.SelectedSong;

        
        if (songToRestore is null)
            return;

        
        
        Button send = (Button)sender;
        var artistName = send.Text;

        await Task.WhenAll(ArtistAlbumView.DimmInCompletelyAndShow(), SongTabView.DimmOutCompletelyAndHide());

        MyViewModel.SearchSongSB_TextChanged(StaticMethods.PresetQueries.ByAlbum(artistName)+ " " +StaticMethods.PresetQueries.SortByTitleAsc());

        
       
        
        
        
        if (MyViewModel.SearchResults.Contains(songToRestore))
        {
            MyViewModel.SelectedSong = songToRestore;
        }
    }

    private async void NavigateToSelectedSongPageContextMenuAsync(object sender, EventArgs e)
    {

        var view = (Microsoft.Maui.Controls.MenuFlyoutItem)sender;
        var selectedSec = view.BindingContext as SongModelView;
        if (selectedSec is null)
        {
            return;
        }
        MyViewModel.SelectedSong = selectedSec;
        MyViewModel.CurrentPageContext = CurrentPage.SingleSongPage;
        await MyViewModel.LoadSelectedSongLastFMData();
    }

    private async void ViewSongDetails_PointerPressed(object sender, PointerEventArgs e)
    {
        await SongDetailsPopup.ShowAsync();
    }

    private void QuickFilterGest_PointerReleased(object sender, PointerEventArgs e)
    {

    }

    private async void SaveCurrentCoverToDisc_Clicked(object sender, EventArgs e)
    {
        await MyViewModel.SaveCurrentCoverToDisc(MyViewModel.SelectedSong);

    }

    private async void ApplyCurrentImageToAllSongsInAlbum_Clicked(object sender, EventArgs e)
    {
        await MyViewModel.ApplyCurrentImageToAllSongsInAlbum(MyViewModel.SelectedSong);

    }

    private async void ApplyCurrentImageToMainArtist_Clicked(object sender, EventArgs e)
    {
        await MyViewModel.ApplyCurrentImageToMainArtist(MyViewModel.SelectedSong);
    }

    private void Button_Clicked(object sender, EventArgs e)
    {

    }

    private void OpenFileInFileExplorer_Clicked(object sender, EventArgs e)
    {

        // open AND select the file in file explorer

        var song = MyViewModel.SelectedSong;
        if (song is null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(song.FilePath) && System.IO.File.Exists(song.FilePath))
        {
            try
            {
                // The argument string is correct.
                string argument = $"/select, \"{song.FilePath}\"";

                // Use ProcessStartInfo for more control and reliability.
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = argument,
                    // These are the defaults, but it's good to be explicit
                    UseShellExecute = true,
                    CreateNoWindow = true
                };

                System.Diagnostics.Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                // Log the exception or show an error message to the user.
                Console.WriteLine($"Error opening file explorer: {ex.Message}");
            }
        }


    }

    private async void ApplyCurrentImageToCurrentPlayingSong_Clicked(object sender, EventArgs e)
    {
        await MyViewModel.ApplyCurrentImageToAllSongsInAlbum(MyViewModel.CurrentPlayingSongView);
    }

    private void SongTitleEntry_Focused(object sender, FocusEventArgs e)
    {
        SongTitleEntry.Text = MyViewModel.SelectedSong?.Title;
    }

    private void SongAlbumName_Focused(object sender, FocusEventArgs e)
    {

        SongArtistNameEntry.Text = MyViewModel.SelectedSong?.AlbumName;
    }

    private void SongArtistNameEntry_Focused(object sender, FocusEventArgs e)
    {

        SongAlbumName.Text = MyViewModel.SelectedSong?.ArtistName;
    }

    private void SongTabView_SelectionChanged(object sender, Syncfusion.Maui.Toolkit.TabView.TabSelectionChangedEventArgs e)
    {

    }

    private async void PasteFromClipBoard_Clicked(object sender, EventArgs e)
    {
        if (MyViewModel.SelectedSong is null)
        {
            return;
        }
        var clipboardText = await Microsoft.Maui.ApplicationModel.DataTransfer.Clipboard.Default.GetTextAsync();
        if (string.IsNullOrWhiteSpace(clipboardText))
        {
            return;
        }

        PlainLyricsEditor.Text = clipboardText;

        MyViewModel.StartLyricsEditingSession(clipboardText);
    }

    private void ShowBtmSheet_Clicked(object sender, EventArgs e)
    {

    }

    private void MoreBtn_Clicked(object sender, EventArgs e)
    {
        Button btn = (Button)sender;
        //btn.ShowContextMenu();
    }

    private void TimestampCurrentLyricLineBtn_Clicked(object sender, EventArgs e)
    {
        var send = (ImageButton)sender;

        var param = send.CommandParameter as LyricEditingLineViewModel;

        if (param is null)
        {
            return;
        }

        MyViewModel.TimestampCurrentLyricLine(param);

    }

    private void DeleteCurrentLyricLineBtn_Clicked(object sender, EventArgs e)
    {
        var send = (ImageButton)sender;

        var param = send.CommandParameter as LyricEditingLineViewModel;

        if (param is null)
        {
            return;
        }


        var te = MyViewModel.LyricsInEditor.IndexOf(param);

        if (te < 0)
        {
            return;
        }
        MyViewModel.DeleteTimestampFromLine(param);
    }

    private void AcceptUserEditedLyricsBtn_Clicked(object sender, EventArgs e)
    {

    }


    private void SyncLyricEditor_TextChanged(object sender, TextChangedEventArgs e)
    {
        var send = (Editor)sender;
        var lyVM = send.BindingContext as LyricEditingLineViewModel;
        if (lyVM is null)
        {
            return;
        }
        var newText = e.NewTextValue;
        if (newText is null)
        {
            newText = string.Empty;
        }

        lyVM.Text = newText;

        MyViewModel.SaveLyricEditedByUserBeforeTimestamping(lyVM, newText);
    }

    private void ShowOnlyCompletedPlays_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {

    }

    private void ShowOnlySkippedPlays_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {

    }
}
