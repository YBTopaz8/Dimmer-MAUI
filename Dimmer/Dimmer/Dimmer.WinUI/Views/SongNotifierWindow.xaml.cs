using Dimmer.WinUI.ViewModel;

namespace Dimmer.WinUI.Views;

public partial class SongNotifierWindow : Window
{
	public SongNotifierWindow(BaseViewModelWin vm)
	{
		InitializeComponent();
        BindingContext = vm;
        MyViewModel=vm;
    }
    public BaseViewModelWin MyViewModel { get; internal set; }

}