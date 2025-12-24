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
        _compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;
    }

    private readonly Compositor _compositor;
    protected async override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        // Resolve VM
        MyViewModel = IPlatformApplication.Current!.Services.GetService<BaseViewModelWin>()!;


        await LoadUserData();


       
    }

    protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
    {
        
        base.OnNavigatingFrom(e);

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

    LastFMUserView? user;
    private async Task LoadUserData()
    {
        // 1. Get Data from VM
        user = MyViewModel.CurrentUserLocal?.LastFMAccountInfo; // Assuming this property exists on your VM parity
        
        if (user != null)
        {
            UserNameTxt.Text = user.Name;
            TotalScrobblesTxt.Text = $"{user.Playcount:N0} Scrobbles";
            scrobblingSince.Text = $"Scrobbling since {user.Registered:dd MMM yyyy}";
            // If user.Image is a string URL:
            if (!string.IsNullOrEmpty(user.Image.Url))
            {
                UserAvatarImg.ProfilePicture
                    = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(user.Image.Url));
                UserAvatarImg.DisplayName = user.Name;
            }
            await MyViewModel.LoadUserLastFMDataAsync(user);

        }
        else
        {
            UserNameTxt.Text = "Not Connected";
            TotalScrobblesTxt.Text = "Log in via Settings";
        }
    }

    Microsoft.UI.Xaml.Controls.Button? SongTitlebutton;
    private void SongTitle_Click(object sender, RoutedEventArgs e)
    {
        SongTitlebutton = sender as Button;
        trackModel = SongTitlebutton?.DataContext as Track;
        MyViewModel.SelectedTrack = trackModel;
        var songModelView = trackModel.IsOnPresentDevice ? MyViewModel.SearchResults.FirstOrDefault(song => song.Id.ToString() == trackModel?.OnDeviceObjectId)
            :null;

        AnimationHelper.Prepare(AnimationHelper.Key_ListToDetail, SongTitlebutton);

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
    Track? trackModel = null;
    private async void RecentTracksList_Loaded(object sender, RoutedEventArgs e)
    {
        if (MyViewModel.SelectedTrack != null)
        {
            trackModel = MyViewModel.SelectedTrack;
            // CLEAN: Handles scrolling, updating layout, finding the image, and starting animation
           await AnimationHelper.TryStartListReturn(
                RecentTracksList,
                trackModel,
                "coverArtImage",
                AnimationHelper.Key_DetailToList
            );

            trackModel = null;
        }
    }
}