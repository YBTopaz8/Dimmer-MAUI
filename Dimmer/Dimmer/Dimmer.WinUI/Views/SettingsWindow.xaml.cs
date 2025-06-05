
public partial class SettingsWindow : Window
{
    public BaseViewModel MyViewModel { get; internal set; }
    public SettingsWindow(BaseViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        MyViewModel=vm;

        this.Height = 800;
        this.Width = 800;
    }


    protected override void OnCreated()
    {
        base.OnCreated();

    }

    protected override void OnDestroying()
    {
        base.OnDestroying();
    }
    private void ChangeFolder_Clicked(object sender, EventArgs e)
    {


        var selectedFolder = (string)((ImageButton)sender).CommandParameter;
        //await MyViewModel.SelectSongFromFolder(selectedFolder);
    }

    private void DeleteBtn_Clicked(object sender, EventArgs e)
    {
        var send = (ImageButton)sender;
        var param = send.CommandParameter.ToString();
        MyViewModel.DeleteFolderPath(param);
    }
    private void AddNewMusicFolder_Clicked(object sender, EventArgs e)
    {
        //await MyViewModel.SelectSongFromFolder();
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
                break;
            case "1":
                break;
            default:

                break;
        }

    }

    private void ShowBtmSheet_Clicked(object sender, EventArgs e)
    {
    }

    private void SettingsNavChips_SelectionChanged(object sender, Syncfusion.Maui.Toolkit.Chips.SelectionChangedEventArgs e)
    {

    }

    private void SettingsNavChips_ChipClicked(object sender, EventArgs e)
    {

    }
}