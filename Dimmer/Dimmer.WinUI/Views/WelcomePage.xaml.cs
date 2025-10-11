using Dimmer.Resources.Localization;

namespace Dimmer.WinUI.Views;

public partial class WelcomePage : ContentPage
{
	public WelcomePage(BaseViewModelWin vm)
	{
		InitializeComponent();
        MyViewModel = vm;
        BindingContext = vm;

        MyViewModel.MainWindowActivatedEventHandler += MyViewModel_MainWindowActivated;
    }

    private void MyViewModel_MainWindowActivated(object? sender, EventArgs e)
    {
        if(WelcomeTabView.SelectedIndex ==1)
        {
            MyViewModel.CompleteLastFMLoginCommand.Execute(null);
        }
    }

    BaseViewModelWin MyViewModel { get; }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        //Shell.SetBackButtonBehavior(this, new BackButtonBehavior
        //{
        //    IsVisible = false,
            
        //    IsEnabled = false
        //});
    }


    private void ChangeFolder_Clicked(object sender, EventArgs e)
    {

    }

    private void DeleteBtn_Clicked(object sender, EventArgs e)
    {
        var send = (ImageButton)sender;
        var param = send.CommandParameter.ToString();
        MyViewModel.DeleteFolderPath(param);
    }

    private void NextBtn_Clicked(object sender, EventArgs e)
    {
     
    }

    private void RescanFolder_Clicked(object sender, EventArgs e)
    {

        var send = (SfChip)sender;
        var comParam = send.CommandParameter as string;

        if (comParam is null)
            return;

        MyViewModel.ReScanMusicFolderByPassingToServiceCommand.Execute(comParam);
    }

    private async void AddNewMusicFolder_Clicked(object sender, EventArgs e)
    {

        await MyViewModel.AddMusicFolderViaPickerAsync();
    }

    private void WelcomeTabView_Loaded(object sender, EventArgs e)
    {

        MyViewModel.WelcomeTabViewItemsCount = WelcomeTabView.Items.Count;


    }

    private async void NextBtnTwo_Clicked(object sender, EventArgs e)
    {
        
        if (NextBtnTwo.Text == DimmerLanguage.txt_done)
        {
            await MyViewModel.AppSetupPageNextBtnClick(true);
            return;
        }
        await MyViewModel.AppSetupPageNextBtnClick(false);

    }

    private void TryCommand_Clicked(object sender, EventArgs e)
    {

    }


    private void WelcomeTabView_SelectionChanged(object sender, Syncfusion.Maui.Toolkit.TabView.TabSelectionChangedEventArgs e)
    {
        var isEnd = e.NewIndex == (WelcomeTabView.Items.Count - 1);
        if(isEnd)
        {
            NextBtnTwo.Text = DimmerLanguage.txt_done;
        }
    }
}