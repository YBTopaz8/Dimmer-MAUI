namespace Dimmer.Views.SingleSong;

public partial class LyricsManualSyncPage : ContentPage
{
    private BaseViewModelAnd ViewModel => (BaseViewModelAnd)BindingContext;

    public LyricsManualSyncPage(BaseViewModelAnd viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private void SyncButton_Clicked(object sender, EventArgs e)
    {
        // 1. Android Haptic Feedback (Gives a physical click feeling on tap)
        try
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
        }
        catch { /* Fallback for unsupported devices */ }

        // 2. Find the active line
        var targetLine = ViewModel.LyricsInEditor?.FirstOrDefault(x => x.IsCurrentLine);
        if (targetLine != null)
        {
            // Execute the stamping logic (which includes the 250ms human-reaction offset)
            ViewModel.TimestampCurrentLyricLineCommand.Execute(targetLine);

            // 3. Smoothly auto-scroll DevExpress to the newly highlighted line!
            var nextLine = ViewModel.LyricsInEditor?.FirstOrDefault(x => x.IsCurrentLine);
            if (nextLine != null)
            {
                var handle = LyricsCV.FindItemHandle(nextLine);
                LyricsCV.ScrollTo(handle, DXScrollToPosition.Start);
            }
        }
    }

    private async void SkipBackwards_Clicked(object sender, EventArgs e)
    {
        double newPos = Math.Max(0, ViewModel.AudioService.CurrentPosition - 5.0);
        await ViewModel.AudioService.SeekAsync(newPos);
    }

    private async void SkipForward_Clicked(object sender, EventArgs e)
    {
        double newPos = Math.Min(ViewModel.CurrentPlayingSongView.DurationInSeconds, ViewModel.AudioService.CurrentPosition + 5.0);
        await ViewModel.AudioService.SeekAsync(newPos);
    }

    private async void CancelBtn_Clicked(object sender, EventArgs e)
    {
        ViewModel.CancelLyricsEditingSessionCommand.Execute(null);
        await Shell.Current.GoToAsync("..");
    }
}