namespace Dimmer.Views.CustomViews;

public partial class SingleSongInCVBottomSheet : BottomSheet
{
	public SingleSongInCVBottomSheet( StatisticsViewModel statsVM)
	{
		InitializeComponent();
        StatsVM = statsVM;
    }
	SongModelView SelectedSong { get; set; }
    public StatisticsViewModel StatsVM { get; }

	//private async void SongInCVPopup_Opening(object sender, CancelEventArgs e)
	//{
	//	try
	//	{

	//		await StatsVM.LoadSongQuickStatsAsync(SelectedSong);
	//	}
	//	catch (Exception ex)
	//	{
	//		Debug.WriteLine(ex.Message);
	//	}
	//}

	public void SetSelectedSongToShow(SongModelView selectedSong)

	{
		SelectedSong = selectedSong;
		this.BindingContext = SelectedSong;
	}

    private void NavChip_Tapped(object sender, HandledEventArgs e)
    {
		var text = ((Chip)sender).Text;

		var curIndex = PopupSlideView.CurrentIndex;
		if (!string.IsNullOrEmpty(text))
		{
			text = text.ToLower();
			switch (text)
			{
				case "prev":
					PopupSlideView.CurrentIndex--;

                    break;
				case "next":
					PopupSlideView.CurrentIndex++;
					break;
				default:
					break;
			}

		}
    }

    private void PopupSlideView_CurrentItemChanged(object sender, ValueChangedEventArgs<object> e)
    {
		if (PopupSlideView.CurrentIndex == 0)
		{
			PrevChip.IsEnabled = false;
            NextChip.IsEnabled = true;
        }
		else if (PopupSlideView.CurrentIndex == PopupSlideView.Items.Count - 1)
		{
			NextChip.IsEnabled = false;
            PrevChip.IsEnabled = true;
        }

		else
		{
			PrevChip.IsEnabled = true;
            NextChip.IsEnabled = true;
        }
    }
}