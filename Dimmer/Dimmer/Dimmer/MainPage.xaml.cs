namespace Dimmer;

public partial class MainPage : ContentPage
{
    int count;

    public MainPage(BaseAppFlow baseAppFlow)
    {
        InitializeComponent();
        baseAppFlow.Initialize();
    }

    private void OnCounterClicked(object sender, EventArgs e)
    {
        count++;

        if (count == 1)
            CounterBtn.Text = $"Clicked {count} time";
        else
            CounterBtn.Text = $"Clicked {count} times";

        SemanticScreenReader.Announce(CounterBtn.Text);
    }
}
