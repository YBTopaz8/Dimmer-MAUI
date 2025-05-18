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


    protected override void OnCreated()
    {
        base.OnCreated();

    }

    protected override void OnDestroying()
    {
     MyViewModel.IsSettingWindoOpened=false;
        base.OnDestroying();
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
                FirstTimeTabView.SelectedIndex--;
                break;
            case "1":
                FirstTimeTabView.SelectedIndex++;
                break;
            default:
                
                break;
        }

    }
}