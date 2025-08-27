namespace Dimmer.Views;

public partial class DetailSongPage : ContentPage
{
    DetailFormViewModel ViewModel => BindingContext as DetailFormViewModel;

    public SongModelView song => (SongModelView)ViewModel.Item;
    bool isDeleting;
    public DetailSongPage()
    {
        InitializeComponent();

    }

    private void DeleteTBItem_Clicked(object sender, EventArgs e)
    {

    }
}