namespace Dimmer.WinUI.Views.SingleSongPages;

public partial class SingleSongPage : ContentPage
{
    public SingleSongPage(BaseViewModelWin vm)
    {
        InitializeComponent();
        BindingContext = vm;
        MyViewModel = vm;
    }

    public BaseViewModelWin MyViewModel { get; }

    protected async override void OnAppearing()
    {
        base.OnAppearing();
       
            await MyViewModel.LoadSongLastFMData();
            await MyViewModel.LoadSongLastFMMoreData();

        
    }
}