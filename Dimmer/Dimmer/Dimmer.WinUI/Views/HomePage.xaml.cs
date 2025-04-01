

namespace Dimmer.WinUI.Views;

public partial class HomePage : ContentPage
{
	public HomePage(HomeViewModel vm)
	{
		InitializeComponent();

        BindingContext = vm;	

		List<string> list = new List<string>();
        list.Add("Item 1");
        list.Add("Item 2");
        list.Add("Item 1");
        list.Add("Item 2");
        list.Add("Item 1");
        list.Add("Item 2");
        list.Add("Item 1");
        list.Add("Item 2");
        list.Add("Item 1");
        list.Add("Item 2");


    }
}