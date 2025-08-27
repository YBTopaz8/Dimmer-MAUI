namespace Dimmer.WinUI.Views.DimmerLiveUI;

public partial class SocialView : ContentPage
{
	public SocialView(SocialViewModelWin vm)
	{
		InitializeComponent();
        BindingContext = vm;
        MyViewModel=vm;
    }


    public SocialViewModelWin MyViewModel { get; }
}