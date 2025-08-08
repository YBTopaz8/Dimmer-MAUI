using AndroidX.Lifecycle;

using DevExpress.Maui.Editors;

using Dimmer.DimmerLive;
using Dimmer.DimmerSearch;

using System.ComponentModel;

namespace Dimmer.Views.CustomViewsParts;

public partial class TopBeforeColView : DXExpander
{
	public TopBeforeColView()
	{
		InitializeComponent();
        var vm = IPlatformApplication.Current!.Services.GetService<BaseViewModelAnd>()??throw new NullReferenceException("BaseViewModelAnd is not registered in the service collection.");
        this.BindingContext =vm;

        this.MyViewModel =vm;
    }
    public BaseViewModelAnd MyViewModel { get; set; }

    private CancellationTokenSource _lyricsCts;
    private bool _isLyricsProcessing = false;
    private async void RefreshLyrics_Clicked(object sender, EventArgs e)
    {
        if (_isLyricsProcessing)
        {
            // Optionally, offer to cancel the running process
            bool cancel = await  Shell.Current.DisplayAlert("Processing...", "Lyrics are already being processed. Cancel the current operation?", "Yes, Cancel", "No");
            if (cancel)
            {
                _lyricsCts?.Cancel();
            }
            return;
        }

        _isLyricsProcessing = true;
        MyProgressBar.IsVisible = true; // Show a progress bar
        MyProgressLabel.IsVisible = true; // Show a label

        // Create a new CancellationTokenSource for this operation
        _lyricsCts = new CancellationTokenSource();

        // The IProgress<T> object automatically marshals calls to the UI thread.
        var progressReporter = new Progress<LyricsProcessingProgress>(progress =>
        {
            // This code runs on the UI thread safely!
            MyProgressBar.Progress = (double)progress.ProcessedCount / progress.TotalCount;
            MyProgressLabel.Text = $"Processing: {progress.CurrentFile}";
        });

        try
        {

            await MyViewModel.LoadSongDataAsync(progressReporter, _lyricsCts);
            await Shell.Current.DisplayAlert("Complete", "Lyrics processing finished!", "OK");

        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlert("Cancelled", "The operation was cancelled.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"An unexpected error occurred: {ex.Message}", "OK");
        }
        finally
        {
            // Clean up and hide UI elements
            _isLyricsProcessing = false;
            MyProgressBar.IsVisible = false;
            MyProgressLabel.IsVisible = false;
        }
    }
    private async void BtmBar_RequestFocusOnMainView(object sender, EventArgs e)
    {
        //if (!TopBeforeColView.IsExpanded)
        //{
        //    TopBeforeColView.IsExpanded= !TopBeforeColView.IsExpanded;

        //    SearchBy.Focus();
        //    await OpenedKeyboardToolbar.DimmInCompletelyAndShow();
        //}
        //else
        //{
        //    TopBeforeColView.IsExpanded=false;
        //}
    }

    private async void OpenDevExpressFilter_Tap(object sender, HandledEventArgs e)
    {
        //myPageSKAV.IsOpened = !myPageSKAV.IsOpened;
        SearchBy.Unfocus();
        //await OpenedKeyboardToolbar.DimmOutCompletelyAndHide();

    }
    private void SearchBy_Focused(object sender, FocusEventArgs e)
    {

    }
    private void ScrollToCurrSong_Tap(object sender, HandledEventArgs e)
    {
        //int itemHandle = SongsColView.FindItemHandle(MyViewModel.BaseVM.CurrentPlayingSongView);
        //SongsColView.ScrollTo(itemHandle, DXScrollToPosition.Start);

    }
    private async void ArtistsChip_LongPress(object sender, HandledEventArgs e)
    {
        var send = (Chip)sender;
        string inputString = send.LongPressCommandParameter as string;

        char[] dividers = new char[] { ',', ';', ':', '|', '-', '/' };

        var namesList = inputString
            .Split(dividers, StringSplitOptions.RemoveEmptyEntries) // Split by dividers and remove empty results
            .Select(name => name.Trim())                           // Trim whitespace from each name
            .ToArray();                                             // Convert to a List


        var res = await Shell.Current.DisplayActionSheet("Select Artist", "Cancel", string.Empty, namesList);

        if (string.IsNullOrEmpty(res))
        {
            return;
        }
        var ss = StaticMethods.SetQuotedSearch("artist", res);

        SearchBy.Text =ss;
    }

    private void AlbumFilter_LongPress(object sender, HandledEventArgs e)
    {
        var send = (Chip)sender;
        SearchBy.Text=
        StaticMethods.SetQuotedSearch("album", send.LongPressCommandParameter as string);
    }

    // The "Years" methods remain unchanged.
    private void QuickFilterYears_LongPress(object sender, HandledEventArgs e)
    {

        var send = (Chip)sender;
        SearchBy.Text=
        StaticMethods.SetQuotedSearch("year", send.LongPressCommandParameter as string);
    }

    private void SearchBy_TextChanged(object sender, EventArgs e)
    {

    }

    private void Settings_Tap(object sender, HandledEventArgs e)
    {

    }
}