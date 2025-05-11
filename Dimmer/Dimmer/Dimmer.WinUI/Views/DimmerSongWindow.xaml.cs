using System.Threading.Tasks;

namespace Dimmer.WinUI.Views;

public partial class DimmerSongWindow : Window
{
    private readonly BaseViewModelWin ViewModel;

    public DimmerSongWindow(BaseViewModelWin vm)
	{
		InitializeComponent();
		
		BindingContext=vm;
        this.ViewModel=vm;
    }

    private async void ViewConverationGesture_Tapped(object sender, TappedEventArgs e)
    {
        await ViewModel.OpenSpecificChatConversationCommand.ExecuteAsync((e.Parameter as string));
    }

    protected async override void OnCreated()
    {
        base.OnCreated();

        await ViewModel.LoadOnlineData();
    }

    private void ShareProfileBtn_Clicked(object sender, EventArgs e)
    {

        
    }
}