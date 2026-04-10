namespace Dimmer.Views.Artist;

public partial class AllArtistsPage : ContentPage
{
	public AllArtistsPage(BaseViewModelAnd myViewModel)
	{
		InitializeComponent();
		MyViewModel = myViewModel;
		BindingContext = myViewModel;
	}
    public BaseViewModelAnd MyViewModel { get; }

    protected override void OnAppearing()
    {
        base.OnAppearing();

		foreach (var art in MyViewModel.ArtistsCollection)
		{
			Debug.WriteLine(art.Name);
			Debug.WriteLine(art.ImagePath);
			Debug.WriteLine(art.SongsByArtist.Count);
			Debug.WriteLine(art.AlbumsByArtist.Count);

		}
    }
}