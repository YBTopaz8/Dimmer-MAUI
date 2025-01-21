

namespace Dimmer_MAUI.Views.CustomViews;

public partial class FetchLyricsResultsView : ContentView
{
    HomePageVM MyViewModel { get; set; }
    public FetchLyricsResultsView()
	{
		InitializeComponent();

        var vm = IPlatformApplication.Current!.Services.GetService<HomePageVM>();
        MyViewModel = vm;
        
	}

    private async void ViewLyricsBtn_Clicked(object sender, EventArgs e)
    {
        var send = (Button)sender;
        var title = send.Text;
        var thisContent = send.BindingContext as Dimmer_MAUI.Utilities.Models.Content;
        if (title == "Synced Lyrics")
        {

            await MyViewModel.ShowSingleLyricsPreviewPopup(thisContent, false);
        }else
        if (title == "Plain Lyrics")
        {

            await MyViewModel.ShowSingleLyricsPreviewPopup(thisContent, true);
        }
    }

    //private async void SaveSelectedLyricsToFile(object sender, EventArgs e)
    //{
    //    var send = (Button)sender;
    //    var cont = send.BindingContext as Content;
    //    if (!string.IsNullOrEmpty(cont.syncedLyrics))
    //    {
    //       await MyViewModel.SaveSelectedLyricsToFile(true);
    //    }
    //    else
    //    {
    //        await MyViewModel.SaveSelectedLyricsToFile(false);
    //    }
    //    //await Shell.Current.ShowPopupAsync(new ViewLyricsPopUp(cont, send.Text));
    //}
}