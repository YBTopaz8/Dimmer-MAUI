using Dimmer.Data.Models;

using Microsoft.UI.Xaml;

using System.Text.RegularExpressions;

using Window = Microsoft.UI.Xaml.Window;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views.WinUIPages;
/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class AllSongsWindow : Window
{
    public AllSongsWindow(BaseViewModelWin vm)
    {
        InitializeComponent();


        //MyPageGrid.DataContext=vm;
        MyViewModel= vm;

        //when window is activated, focus on the search box and scroll to currently playing song
        this.Activated += (s, e) =>
        {
            if (e.WindowActivationState == WindowActivationState.Deactivated)
            {
                return;
            }

            
            MyViewModel.CurrentWinUIPage = this;
            var removeCOmmandFromLastSaved = MyViewModel.CurrentTqlQuery;
            removeCOmmandFromLastSaved = Regex.Replace(removeCOmmandFromLastSaved, @">>addto:\d+!", "", RegexOptions.IgnoreCase);

            removeCOmmandFromLastSaved = Regex.Replace(removeCOmmandFromLastSaved, @">>addto:end!", "", RegexOptions.IgnoreCase);

            removeCOmmandFromLastSaved = Regex.Replace(removeCOmmandFromLastSaved, @">>addnext!", "", RegexOptions.IgnoreCase);



            // Focus the search box
            //SearchSongSB.Focus(FocusState.Programmatic);


            //// Scroll to the currently playing song
            //if (MyViewModel.CurrentPlayingSongView != null)
            //{
            //    ScrollToSong(MyViewModel.CurrentPlayingSongView);
            //}
            ContentFrame.Navigate(typeof(AllSongsListPage), MyViewModel);

        };

        // Initialize collections for live updates
        var realm = MyViewModel.RealmFactory.GetRealmInstance();
        _liveArtists = new ObservableCollection<string>(realm.All<ArtistModel>().AsEnumerable().Select(x => x.Name));
        _liveAlbums = new ObservableCollection<string>(realm.All<AlbumModel>().AsEnumerable().Select(x => x.Name));
        _liveGenres = new ObservableCollection<string>(realm.All<GenreModel>().AsEnumerable().Select(x => x.Name));

        this.Closed +=AllSongsWindow_Closed;


    }

    private void AllSongsWindow_Closed(object sender, WindowEventArgs args)
    {
        this.Closed -= AllSongsWindow_Closed;
    }

    public ObservableCollection<string> _liveArtists;
    public ObservableCollection<string> _liveAlbums;
    public ObservableCollection<string> _liveGenres;

    public BaseViewModelWin MyViewModel { get; internal set; }

    
    
  
}