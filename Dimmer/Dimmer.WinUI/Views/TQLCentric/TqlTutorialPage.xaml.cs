
using Dimmer.WinUI.Views.WinUIPages;

namespace Dimmer.WinUI.Views.TQLCentric;

public partial class TqlTutorialPage : ContentPage
{
	public TqlTutorialPage(Dimmer.ViewModel.TqlTutorialViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
		MyViewModelView = vm;
    
    }

    public TqlTutorialViewModel MyViewModelView { get; }

    private void Button_Clicked(object sender, EventArgs e)
    {
        var button = sender as Button;
        if (button != null && button.CommandParameter is string query)
        {
            MyViewModelView.TryItNow(query);
        }
    }
}