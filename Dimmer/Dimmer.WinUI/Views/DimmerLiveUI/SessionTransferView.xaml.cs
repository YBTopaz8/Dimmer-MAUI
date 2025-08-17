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
}