using System.Text.RegularExpressions;

using Microsoft.UI.Xaml;

using Windows.Graphics;



// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DimmerWin : Window
    {
        private BaseViewModelWin baseViewModelWin;
        private AppUtil appUtil;

        public DimmerWin()
        {
            InitializeComponent();
            MyViewModel= IPlatformApplication.Current?.Services.GetService<BaseViewModelWin>();
            if(MyViewModel is not null)
                ContentFrame.Navigate(typeof(AllSongsListPage), MyViewModel);

        }
        public BaseViewModelWin? MyViewModel { get; internal set; }
        private void AllSongsWindow_Closed(object sender, WindowEventArgs args)
        {
            this.Closed -= AllSongsWindow_Closed;
        }
        public void LoadWindowAndPassVM(BaseViewModelWin baseViewModelWin, AppUtil appUtil)
        {
            this.baseViewModelWin = baseViewModelWin;
            this.appUtil = appUtil;
            
        }

        private void Window_Activated(object sender, WindowActivatedEventArgs args)
        {
            //MyPageGrid.DataContext=vm;
            

            //when window is activated, focus on the search box and scroll to currently playing song
            this.Activated += (s, e) =>
            {
                if (e.WindowActivationState == WindowActivationState.Deactivated)
                {
                    return;
                }


                SizeInt32 currentWindowSize = new SizeInt32(1600, 1000);
                PlatUtils.ResizeNativeWindow(this, currentWindowSize);

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

            };

            ContentFrame.Navigate(typeof(AllSongsListPage), MyViewModel);

            this.Closed += AllSongsWindow_Closed;
        }
    }
}
