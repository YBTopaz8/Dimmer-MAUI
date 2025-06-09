namespace Dimmer.Views;

public partial class SingleSongPage : ContentPage
{
    public SingleSongPage(BaseViewModelAnd vm)
    {
        InitializeComponent();
        BindingContext = vm;
        MyViewModel = vm;
    }
    public BaseViewModelAnd MyViewModel { get; internal set; }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        MyViewModel.BaseVM.LoadStatsForSong(MyViewModel.BaseVM.SelectedSongForContext!);
    }
}