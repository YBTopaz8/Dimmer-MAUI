using System.Threading.Tasks;

namespace Dimmer.WinUI.Views;

public partial class SettingsWindow : Window
{
    public BaseViewModelWin MyViewModel { get; internal set; }
    public SettingsWindow(BaseViewModelWin vm)
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
    private async void ChangeFolder_Clicked(object sender, EventArgs e)
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
        await MyViewModel.PickFolderToScan();
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