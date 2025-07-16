using Dimmer.DimmerLive;
using Dimmer.Interfaces.Services.Interfaces;

namespace Dimmer.WinUI.Views.SettingsCenter;

public partial class SettingsPage : ContentPage
{
    public BaseViewModelWin MyViewModel { get; internal set; }
    public SettingsPage(BaseViewModelWin vm)
    {
        InitializeComponent();
        BindingContext = vm;
        MyViewModel=vm;

    }


    private void ChangeFolder_Clicked(object sender, EventArgs e)
    {
        var selectedFolder = (string)((ImageButton)sender).CommandParameter;
        //await  MyViewModel.AddMusicFolderAsync(selectedFolder);
    }

    private void DeleteBtn_Clicked(object sender, EventArgs e)
    {
        var send = (ImageButton)sender;
        var param = send.CommandParameter.ToString();
        MyViewModel.DeleteFolderPath(param);
    }
    private async void AddNewMusicFolder_Clicked(object sender, EventArgs e)
    {
        await MyViewModel.AddMusicFolderViaPickerAsync();
    }

    private void FirstTimeTabView_SelectionChanged(object sender, Syncfusion.Maui.Toolkit.TabView.TabSelectionChangedEventArgs e)
    {

    }

    private void NavBtnClicked_Clicked(object sender, EventArgs e)
    {
        var send = (Button)sender;
        var param = send.CommandParameter.ToString();
        switch (param)
        {
            case "0":
                break;
            case "1":
                break;
            default:

                break;
        }

    }

    private void ShowBtmSheet_Clicked(object sender, EventArgs e)
    {
    }

    private void SettingsNavChips_SelectionChanged(object sender, Syncfusion.Maui.Toolkit.Chips.SelectionChangedEventArgs e)
    {

    }
    private CancellationTokenSource _lyricsCts;
    private bool _isLyricsProcessing = false;
    private async void RefreshLyrics_Clicked(object sender, EventArgs e)
    {
        var res = await DisplayAlert("Refresh Lyrics", "This will process all songs in the library to update lyrics. Do you want to continue?", "Yes", "No");

        if (!res)
        {
            return; // User cancelled the operation
        }


        if (_isLyricsProcessing)
        {
            bool cancel = await DisplayAlert("Processing...", "Lyrics are already being processed. Cancel the current operation?", "Yes, Cancel", "No");
            if (cancel)
            {
                _lyricsCts?.Cancel();
            }
            return;
        }

        _isLyricsProcessing = true;
        //MyProgressBar.IsVisible = true; // Show a progress bar
        //MyProgressLabel.IsVisible = true; // Show a label



        _lyricsCts = new CancellationTokenSource();



        var progressReporter = new Progress<LyricsProcessingProgress>(progress =>
        {
            //MyProgressBar.Progress = (double)progress.ProcessedCount / progress.TotalCount;
            //MyProgressLabel.Text = $"Processing: {progress.CurrentFile}";
        });

        try
        {
            MyViewModel.SearchSongSB_TextChanged(string.Empty); // Clear the search bar to refresh the list
            var songsToRefresh = MyViewModel.SearchResults; // Or your full master list
            var lryServ = IPlatformApplication.Current.Services.GetService<ILyricsMetadataService>();
            await SongDataProcessor.ProcessLyricsAsync(songsToRefresh, lryServ, progressReporter, _lyricsCts.Token);

            await DisplayAlert("Complete", "Lyrics processing finished!", "OK");
        }
        catch (OperationCanceledException)
        {
            await DisplayAlert("Cancelled", "The operation was cancelled.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"An unexpected error occurred: {ex.Message}", "OK");
        }
        finally
        {
            _isLyricsProcessing = false;
            //MyProgressBar.IsVisible = false;
            //MyProgressLabel.IsVisible = false;
        }
    }


    private void SettingsNavChips_ChipClicked(object sender, EventArgs e)
    {

    }

    private async void Logintolastfm_Clicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(LastFMEmail.Text) || string.IsNullOrEmpty(LastFMPassword.Text))
        {
            await DisplayAlert("Error", "Please enter your Last.fm credentials.", "OK");
            return;
        }
        await MyViewModel.LoginToLastfm(LastFMEmail.Text, LastFMPassword.Text);
    }
}