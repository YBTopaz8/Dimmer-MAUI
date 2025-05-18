namespace Dimmer.WinUI.Views;

public partial class SettingsWindow : Window
{
    public HomeViewModel MyViewModel { get; internal set; }
    public SettingsWindow(HomeViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        MyViewModel=vm;
    }

    private async void ChangeFolder_Clicked(object sender, EventArgs e)
    {
        

        var selectedFolder = (string)((ImageButton)sender).CommandParameter;
        await MyViewModel.SelectSongFromFolder(selectedFolder);
    }

    private void DeleteBtn_Clicked(object sender, EventArgs e)
    {
        var send = (ImageButton)sender;
        var param = send.CommandParameter.ToString();
        MyViewModel.DeleteFolderPath(param);
    }
    private async void AddNewMusicFolder_Clicked(object sender, EventArgs e)
    {
        await MyViewModel.SelectSongFromFolder();
    }
}