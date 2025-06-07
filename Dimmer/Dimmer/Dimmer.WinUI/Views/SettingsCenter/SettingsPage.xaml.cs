namespace Dimmer.WinUI.Views.SettingsCenter;

public partial class SettingsPage : ContentPage
{
    public BaseViewModelWin MyViewModel { get; internal set; }
    public SettingsPage(BaseViewModelWin vm)
    {
        InitializeComponent();
        BindingContext = vm;
        MyViewModel=vm;

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
    private async void AddNewMusicFolder_Clicked(object sender, EventArgs e)
    {
        await MyViewModel.AddMusicFolderViaPickerAsync();
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