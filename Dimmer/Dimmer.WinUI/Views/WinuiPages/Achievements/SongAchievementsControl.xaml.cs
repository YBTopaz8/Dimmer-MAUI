using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views.WinuiPages.Achievements;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class SongAchievementsControl : UserControl
{
    public SongAchievementsViewModel ViewModel;
    public SongAchievementsControl()
    {
        InitializeComponent();

        ViewModel = IPlatformApplication.Current!.Services.GetService<SongAchievementsViewModel>()!;
        DataContext = ViewModel;
    }

    public void FinishLoadAll(BaseViewModelWin parentVM, SongModelView songParam)
    {
        parentVM = IPlatformApplication.Current!.Services.GetService<BaseViewModelWin>()!;
        var realm = parentVM.RealmFactory.GetRealmInstance();
        var findSong = realm.Find<SongModel>(songParam.Id);
        if (findSong is null) return;
        ViewModel.Initialize(findSong);
    }
}
