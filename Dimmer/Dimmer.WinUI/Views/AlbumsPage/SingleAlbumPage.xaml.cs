using CommunityToolkit.Maui.Behaviors;

using Vanara.Extensions;

namespace Dimmer.WinUI.Views.AlbumsPage;

public partial class SingleAlbumPage : ContentPage
{
	public SingleAlbumPage(BaseViewModelWin viewModelWin)
	{
		InitializeComponent();
		BindingContext = viewModelWin;
        MyViewModel = viewModelWin;
    }

    BaseViewModelWin MyViewModel { get; }

    private async void ViewAlbumArtists_Clicked(object sender, EventArgs e)
    {
        var button = (Button)sender;
        var song = button.CommandParameter as SongModelView;

        await MyViewModel.SelectedArtistAndNavtoPage(song);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        MyViewModel.CurrentMAUIPage = null;
        MyViewModel.CurrentMAUIPage = this;

    }

    private void OnArtistButton_TouchGestureCompleted(object? sender, CommunityToolkit.Maui.Core.TouchGestureCompletedEventArgs e)
    {

    }

    private void ViewAlbumArtists_Loaded(object sender, EventArgs e)
    {
        var btn = (Button)sender;
        var tchBehavior = new TouchBehavior()
        {
            HoveredScale = 1.2,
            HoveredAnimationEasing = Easing.BounceOut,
            HoveredAnimationDuration = 450,
            PressedScale = 0.9,
            PressedAnimationEasing = Easing.BounceIn,
            PressedAnimationDuration = 400,
            
        };
        tchBehavior.TouchGestureCompleted += OnArtistButton_TouchGestureCompleted;
        tchBehavior.InteractionStatusChanged += (s, ev) =>
        {

            Debug.WriteLine($"Interaction Status: {ev.TouchInteractionStatus}");

        };
        btn.Behaviors.Add(tchBehavior);
    }

    private void ViewAlbumArtists_Unloaded(object sender, EventArgs e)
    {
        var btn = (Button)sender;
        btn.Behaviors.Clear();

    }
}