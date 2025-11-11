using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using Windows.Foundation;
using Windows.Foundation.Collections;



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

            this.Closed += AllSongsWindow_Closed;

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
    }
}
