// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views.WinuiPages.SingleSongPage.SubPage.Stats;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class SongOverviewStatsPage : Page
{
    public SongOverviewStatsPage()
    {
        InitializeComponent();
    }

    protected async override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {

        base.OnNavigatedTo(e);

        //var statsVM = IPlatformApplication.Current!.Services.GetService<StatsViewModelWin>();
        //this.DataContext = statsVM;
        //var param = e.Parameter as SongModelView;

        //if (param != null)
        //{
        //   await  statsVM!.LoadSongStatsAsync(param);
        //}
    }
}
