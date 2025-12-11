using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

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

    public BaseViewModelWin ViewModel { get; private set; }

    public LastFmPage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        // Resolve VM
        ViewModel = IPlatformApplication.Current!.Services.GetService<BaseViewModelWin>()!;

        // Bind Sources
        RecentTracksList.ItemsSource = RecentTracks;
        TopTracksList.ItemsSource = TopTracks;
        LovedTracksList.ItemsSource = LovedTracks;

        LoadUserData();
        LoadRecentTracks(); // Initial Load
    }

    private void Pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var pivot = sender as Pivot;
        switch (pivot.SelectedIndex)
        {
            case 0: LoadRecentTracks(); break;
            case 1: LoadTopTracks(); break;
            case 2: LoadLovedTracks(); break;
        }
    }

    private void LoadUserData()
    {
        // 1. Get Data from VM
        var user = ViewModel.CurrentUserLocal?.LastFMAccountInfo; // Assuming this property exists on your VM parity

        if (user != null)
        {
            UserNameTxt.Text = user.Name;
            TotalScrobblesTxt.Text = $"{user.Playcount:N0} Scrobbles";

            // If user.Image is a string URL:
            if (!string.IsNullOrEmpty(user.Image.Url))
            {
                UserAvatarImg.Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(user.Image.Url));
            }
        }
        else
        {
            UserNameTxt.Text = "Not Connected";
            TotalScrobblesTxt.Text = "Log in via Settings";
        }
    }

    private async void LoadRecentTracks()
    {
        // Simulate Fetching Data (Replace with: await ViewModel.LastFmService.GetRecentTracks())
        // Parity with Android Logic
        RecentTracks.Clear();

        // Dummy Data for Preview Parity
        var dummyTracks = new List<Track>
        {
            new Track
            {
                Name = "Starlight",
                Artist = new Artist { Name = "Muse" },
                Duration = 240000,
                UserLoved = true,
            },
            new Track
            {
                Name = "Time",
                Artist = new Artist { Name = "Pink Floyd" },
                Duration = 420000,
                UserLoved = false,
                NowPlaying = true,
            }
        };

        foreach (var t in dummyTracks) RecentTracks.Add(t);
    }

    private void LoadTopTracks()
    {
        if (TopTracks.Count > 0) return; // Don't reload if populated
        TopTracks.Clear();
        // Add Dummy Top Tracks...
    }

    private void LoadLovedTracks()
    {
        if (LovedTracks.Count > 0) return;
        LovedTracks.Clear();
        // Add Dummy Loved Tracks...
    }
}