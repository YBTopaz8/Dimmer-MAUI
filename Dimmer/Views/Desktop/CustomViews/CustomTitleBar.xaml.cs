namespace Dimmer_MAUI.Views.Desktop.CustomViews;

public partial class CustomTitleBar : Window
{
	public CustomTitleBar(HomePageVM vM)
	{
		InitializeComponent();
        VM = vM;
        this.BindingContext = vM;
    }

    public HomePageVM VM { get; }
}