using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;

using Hqub.Lastfm.Entities;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using Windows.Foundation;
using Windows.Foundation.Collections;

using NavigationEventArgs = Microsoft.UI.Xaml.Navigation.NavigationEventArgs;
using SelectionChangedEventArgs = Microsoft.UI.Xaml.Controls.SelectionChangedEventArgs;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views.WinuiPages.LastFMSection;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class LastFmPage : Page
{
   
public ObservableCollection<Track> RecentTracks { get; } = new();
    public ObservableCollection<Track> TopTracks { get; } = new();
    public ObservableCollection<Track> LovedTracks { get; } = new();

    public BaseViewModelWin MyViewModel { get; private set; }

    public LastFmPage()
    {
        this.InitializeComponent();
    }

    protected async override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        // Resolve VM
        MyViewModel = IPlatformApplication.Current!.Services.GetService<BaseViewModelWin>()!;


        await LoadUserData();
         // Initial Load
    }

    private void Pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var pivot = sender as Pivot;
        if (pivot != null)
            switch (pivot.SelectedIndex)
            {
                //case 0: LoadRecentTracks(); break;
                //case 1: LoadTopTracks(); break;
                //case 2: LoadLovedTracks(); break;
            }
    }

    private async Task LoadUserData()
    {
        // 1. Get Data from VM
        var user = MyViewModel.CurrentUserLocal?.LastFMAccountInfo; // Assuming this property exists on your VM parity

        if (user != null)
        {
            UserNameTxt.Text = user.Name;
            TotalScrobblesTxt.Text = $"{user.Playcount:N0} Scrobbles";

            // If user.Image is a string URL:
            if (!string.IsNullOrEmpty(user.Image.Url))
            {
                UserAvatarImg.Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(user.Image.Url));
            }
            await MyViewModel.LoadUserLastFMDataAsync();
        }
        else
        {
            UserNameTxt.Text = "Not Connected";
            TotalScrobblesTxt.Text = "Log in via Settings";
        }
    }

    private void SongTitle_Click(object sender, RoutedEventArgs e)
    {
        var SongTitlebutton = sender as Button;
        var trackModel = SongTitlebutton?.DataContext as Track;
        var songModelView = trackModel.IsOnPresentDevice ? MyViewModel.SearchResults.FirstOrDefault(song => song.Id.ToString() == trackModel?.OnDeviceObjectId)
            :null;
        if (songModelView == null) return;
            ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("ForwardConnectedAnimation", SongTitlebutton);

        var visualImage = ElementCompositionPreview.GetElementVisual(SongTitlebutton);

        var supNavTransInfo = new SuppressNavigationTransitionInfo();
        Type songDetailType = typeof(SongDetailPage);
        var navParams = new SongDetailNavArgs
        {
            Song = songModelView,
            ViewModel = MyViewModel,
        };

        FrameNavigationOptions navigationOptions = new FrameNavigationOptions
        {
            TransitionInfoOverride = supNavTransInfo,
            IsNavigationStackEnabled = true

        };

        Frame?.NavigateToType(songDetailType, navParams, navigationOptions);

    }
}