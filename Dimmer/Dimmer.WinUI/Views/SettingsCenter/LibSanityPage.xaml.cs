namespace Dimmer.WinUI.Views.SettingsCenter;

public partial class LibSanityPage : ContentPage
{
	public LibSanityPage(BaseViewModelWin vm)
    {
        InitializeComponent();
        BindingContext = vm;
        MyViewModel = vm;

    }
    public BaseViewModelWin MyViewModel { get; internal set; }
}