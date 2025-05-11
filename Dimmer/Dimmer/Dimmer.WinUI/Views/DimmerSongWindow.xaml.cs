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

    
}