namespace Dimmer.WinUI.Views;

public partial class OnlinePageManagement : ContentPage
{
    BaseViewModelWin ViewModel;
    public OnlinePageManagement(BaseViewModelWin vm)
	{
		InitializeComponent();
        ViewModel = vm;
        BindingContext = ViewModel;

    }


}