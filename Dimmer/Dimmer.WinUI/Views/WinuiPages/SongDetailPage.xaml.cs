using Dimmer.Utilities.StatsUtils;
using Dimmer.ViewModel.StatsVMs;
using Microsoft.UI.Xaml.Documents;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Text.RegularExpressions;
using NavigationEventArgs = Microsoft.UI.Xaml.Navigation.NavigationEventArgs;
using Visibility = Microsoft.UI.Xaml.Visibility;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views.WinuiPages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class SongDetailPage : Page
{
    private SongTransitionAnimation _userPrefAnim = SongTransitionAnimation.Spring;

    private readonly Compositor _compositor;
    public SongModelView DetailedSong { get; set; }
    public SongDetailPage()
    {
        InitializeComponent();
        _compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;
        //DataContext = viewModelWin;
        //MyViewModel = viewModelWin;


    }



    BaseViewModelWin MyViewModel { get; set; }
    public LastFMViewModel MyLastFMViewModel { get; internal set; }
    public SongStatsViewModel? MySongStatsViewModel { get; private set; }
    public CompositeDisposable compDisp { get; set; }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        MyLastFMViewModel = IPlatformApplication.Current!.Services.GetService<LastFMViewModel>()!;
        MySongStatsViewModel = IPlatformApplication.Current.Services.GetService<SongStatsViewModel>();
        //DetailedSong = DetailedSong is null ? MyViewModel.SelectedSong : DetailedSong;

        compDisp = new CompositeDisposable();
        if (e.Parameter is BaseViewModelWin myVm)
        {
            MyViewModel = myVm;
            this.DataContext = MyViewModel;
            DetailedSong = MyViewModel.SelectedSong!;


            MyViewModel.CurrentPageEnum = CurrentPage.SingleSongPage;
        }
        if (e.Parameter is SongDetailNavArgs args)
        {
            var vm = args.ExtraParam is null ? args.ViewModel as BaseViewModelWin : args.ExtraParam as BaseViewModelWin;

            if (vm != null)
            {
                var argSong = args.Song;
                if (argSong == null) return;
                if (string.IsNullOrEmpty(argSong.CoverImagePath))
                {
                    argSong.CoverImagePath = string.Empty;
                } else
                {
                    argSong.CoverImagePath = argSong.CoverImagePath;

                    BgImage.Source = new BitmapImage(new Uri(argSong.CoverImagePath));
                }

                MyViewModel = vm;
                MyViewModel.SelectedSong = argSong;
                DetailedSong = args.Song;
                this.DataContext = MyViewModel;








               
            }
        }

        MyViewModel.SelectedSong = DetailedSong;

        MySongStatsViewModel?.LoadSong(MyViewModel.SelectedSong.Id);

        MyViewModel.CurrentPageEnum = CurrentPage.SingleSongPage;
        await MyViewModel.LoadLyricsFromOnlineOrDBIfNeededAsync(MyViewModel.SelectedSong!);
        await MyLastFMViewModel.LoadSelectedSongLastFMData();


    }

    protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
    {
        if (e.NavigationMode == Microsoft.UI.Xaml.Navigation.NavigationMode.Back)
        {
          

        }
        base.OnNavigatingFrom(e);


    }

    private void PlaySongBtn_Click(object sender, RoutedEventArgs e)
    {

    }

    private void EditSongAudioBtn_Click(object sender, RoutedEventArgs e)
    {

    }

    private void ViewSelector_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {

    }

    private void ViewEditToggle_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        
    }
}

