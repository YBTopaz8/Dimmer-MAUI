
namespace Dimmer_MAUI.CustomPopUpViews;

public partial class CustomRepeatPopup : Popup
{
	public CustomRepeatPopup(int CurrentRepeatCount, SongsModelView song)
	{
		InitializeComponent();

		RepeatPicker.SelectedIndex = CurrentRepeatCount;
		labelForSong.Text = "Set Repeat Count for " + song.Title;
    }

    private void ApplyButton_Clicked(object sender, EventArgs e)
    {
		this.CloseAsync(RepeatPicker.SelectedIndex);
    }

    protected override Task OnClosed(object? result, bool wasDismissedByTappingOutsideOfPopup, CancellationToken token = default)
    {
        return base.OnClosed(result, wasDismissedByTappingOutsideOfPopup, token);
    }

    private void CloseButton_Clicked(object sender, EventArgs e)
    {
        this.CloseAsync(0);
    }
}