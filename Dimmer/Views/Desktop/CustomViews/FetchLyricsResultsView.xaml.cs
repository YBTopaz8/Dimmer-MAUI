namespace Dimmer_MAUI.Views.CustomViews;

public partial class FetchLyricsResultsView : ContentView
{
    HomePageVM VM { get; set; }
    public FetchLyricsResultsView()
	{
		InitializeComponent();

        var vm = IPlatformApplication.Current.Services.GetService<HomePageVM>();
        VM = vm;
	}

    private void AllSyncLyr_SizeChanged(object sender, EventArgs e)
    {

    }

    private async void Button_Clicked(object sender, EventArgs e)
    {
        var send = (Button)sender;
        var cont = send.BindingContext as Content;
        if (!string.IsNullOrEmpty(cont.syncedLyrics))
        {
           await VM.SaveSelectedLyricsToFile(true, cont.syncedLyrics);
        }
        else
        {
            await VM.SaveSelectedLyricsToFile(false, cont.plainLyrics!);
        }
        Debug.WriteLine(cont.GetType());
        //await Shell.Current.ShowPopupAsync(new ViewLyricsPopUp(cont, send.Text));
    }
}