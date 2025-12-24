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

namespace Dimmer.WinUI.Views.WinuiPages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class LibraryStatsPage : Page
{
    public LibraryStatsPage()
    {
        InitializeComponent();
    }
    StatsViewModelWin ViewModel;
    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        ViewModel ??= IPlatformApplication.Current!.Services.GetService<StatsViewModelWin>()!;
        this.DataContext = ViewModel;
    }

    private void BackBtnClick(object sender, RoutedEventArgs e)
    {
        if(Frame.CanGoBack)
        {
            Frame.GoBack();
        }
    }
    public string FormatDouble(double val) => val.ToString("F1");
    public string FormatDate(DateTimeOffset? d) => d?.ToString("MMM dd, yyyy") ?? "-";
    public string FormatNumber(int val) => val.ToString("N0"); // 1,234
    public string FormatDurationShort(int sec) => TimeSpan.FromSeconds(sec).ToString(@"mm\:ss");
    public string FormatDuration(int sec)
    {
        var t = TimeSpan.FromSeconds(sec);
        if (t.TotalHours >= 1) return $"{(int)t.TotalHours}h {t.Minutes}m";
        return $"{t.Minutes}m {t.Seconds}s";
    }
    public string FormatDurationDouble(double sec)
    {
        var t = TimeSpan.FromSeconds(sec);
        if (t.TotalHours >= 1) return $"{(int)t.TotalHours}h {t.Minutes}m";
        return $"{t.Minutes}m {t.Seconds}s";
    }
    // Calculates percentage for text display
    public string CalcPercent(int part, int total)
    {
        if (total == 0) return "0%";
        return $"{(double)part / total:P1}";
    }
}