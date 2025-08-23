using System.Threading.Tasks;

namespace Dimmer.WinUI.Views.DimmerLiveUI;

public partial class SessionTransferView : ContentPage
{
	public SessionTransferView(SessionTransferVMWin sessionTransferVM)
	{
		InitializeComponent();
		BindingContext = sessionTransferVM;
        SessionTransferVM=sessionTransferVM;
    }

    public SessionTransferVMWin SessionTransferVM { get; }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if(!SessionTransferVM.IsAuthenticated())
        {
            MainView.SelectedIndex = 1;
        }
        else
        {
            MainView.SelectedIndex = 0;
        }
        MainView.EnableSwiping = false;
    }

    private async void Login_Clicked(object sender, EventArgs e)
    {
        SessionTransferVM.LoginViewModel.Username = UsernameEntry.Text;
        SessionTransferVM.LoginViewModel.Password = PasswordEntry.Text;
        SessionTransferVM.LoginViewModel.Email = EmailEntry.Text;
        await SessionTransferVM.LoginViewModel.LoginCommand.ExecuteAsync(null);
    }
}