


using ImageButton = Microsoft.Maui.Controls.ImageButton;

namespace Dimmer;

public partial class AppShell : Shell
{
    public AppShell(BaseViewModelAnd baseViewModel)
    {

        InitializeComponent();

        MyViewModel =baseViewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if ((MyViewModel is null))
        {
            return;
        }
        this.BindingContext = MyViewModel;
        
    }

    public BaseViewModelAnd MyViewModel { get; internal set; }
    private void SidePaneChip_Clicked(object sender, EventArgs e)
    {

        
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

   
    private void NavBtnClicked_Clicked(object sender, EventArgs e)
    {
        var send = (Button)sender;
        //var param = send.CommandParameter.ToString();
        //switch (param)
        //{
        //    case "0":
        //        break;
        //    case "1":
        //        break;
        //    default:

        //        break;
        //}

    }

    private void ShowBtmSheet_Clicked(object sender, EventArgs e)
    {
    }

    
    private CancellationTokenSource _lyricsCts;
    private bool _isLyricsProcessing = false;

   
}