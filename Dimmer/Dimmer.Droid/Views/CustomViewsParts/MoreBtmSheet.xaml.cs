using AndroidX.Lifecycle;

namespace Dimmer.Views.CustomViewsParts;

public partial class MoreBtmSheet : BottomSheet
{
    public BaseViewModelAnd MyViewModel { get; internal set; }
    public MoreBtmSheet()
	{
		InitializeComponent();
        MyViewModel = (BaseViewModelAnd)this.BindingContext;
	}

    private void MoreBtmSheet_StateChanged(object sender, ValueChangedEventArgs<BottomSheetState> e)
    {

    }


    private async void OnAddQuickNoteClicked(object sender, HandledEventArgs e)
    {
        var send = (Chip)sender;
        var song = send.TapCommandParameter as SongModelView;
        if (song is null)
        {
            return;
        }
        // Prompt the user for a note


        await MyViewModel.SaveUserNoteToSong(song);
    }


    private async void QuickSearchArtist_Clicked(object sender, HandledEventArgs e)
    {

        var send = (Chip)sender;
        var song = send.BindingContext as SongModelView;
        if (song is null) return;
        var val = song.OtherArtistsName;
        char[] dividers = [',', ';', ':', '|', '-'];

        var namesList = val
            .Split(dividers, StringSplitOptions.RemoveEmptyEntries) // Split by dividers and remove empty results
            .Select(name => name.Trim())                           // Trim whitespace from each name
            .ToArray();                                             // Convert to a List
        if (namesList is not null && namesList.Length == 1)
        {
            MyViewModel.SearchSongSB_TextChanged(StaticMethods.SetQuotedSearch("artist", namesList[0]));

            return;
        }
        var selectedArtist = await Shell.Current.DisplayActionSheet("Select Artist", "Cancel", null, namesList);

        if (string.IsNullOrEmpty(selectedArtist) || selectedArtist == "Cancel")
        {
            return;
        }

        MyViewModel.SearchSongSB_TextChanged(StaticMethods.SetQuotedSearch("artist", selectedArtist));

        return;
    }

    private void QuickSearchAlbum_Clicked(object sender, HandledEventArgs e)
    {
        MyViewModel.SearchSongSB_TextChanged(StaticMethods.SetQuotedSearch("artist", ((Button)sender).CommandParameter.ToString()));

    }

    private void ViewGenreMFI_Clicked(object sender, HandledEventArgs e)
    {

    }

    private void OnLabelClicked(object sender, HandledEventArgs e)
    {

    }

    private void SyncShare_Tap(object sender, HandledEventArgs e)
    {

    }

    private void NavigateToSelectedSongPageContextMenuAsync(object sender, HandledEventArgs e)
    {

    }

    private void CancelSearchAndClose_Tap(object sender, HandledEventArgs e)
    {

    }

    private void ClearAll_Tap(object sender, HandledEventArgs e)
    {

    }

    private void ApplyTQLAndClose_Tap(object sender, HandledEventArgs e)
    {

    }
}