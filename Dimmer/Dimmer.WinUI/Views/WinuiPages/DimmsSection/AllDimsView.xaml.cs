using Dimmer.Charts.Services;
using Dimmer.ViewModel.StatsVMs;
using Microsoft.UI.Xaml.Controls.Primitives;
using SelectionChangedEventArgs = Microsoft.UI.Xaml.Controls.SelectionChangedEventArgs;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views.WinuiPages.DimmsSection;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class AllDimsView : Page
{
    public AllDimsView()
    {
        InitializeComponent();
        //MyEventsTableView
    }
    public StatisticsViewModel MyStatsVM;
    public GeneralStatsViewModel MyGeneralStatsVM;
    BaseViewModelWin MyViewModel;

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        MyViewModel = e.Parameter as BaseViewModelWin;
        if (MyViewModel == null)
        {
            throw new InvalidOperationException("Navigation to AllDimsView requires a BaseViewModelWin parameter.");
        }
        this.DataContext = MyViewModel;

        MyStatsVM = IPlatformApplication.Current.Services.GetService<StatisticsViewModel>();
        MyGeneralStatsVM = IPlatformApplication.Current.Services.GetService<GeneralStatsViewModel>();
        MyViewModel.ActivateHistory();
    }

          // ==========================================
          // TAB NAVIGATION
          // ==========================================
    private void StatsNav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem navItem)
        {
            OverviewTab.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            LeaderboardsTab.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            HistoryTab.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;

            switch (navItem.Tag.ToString())
            {
                case "Overview":
                    OverviewTab.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                    break;
                case "Leaderboards":
                    LeaderboardsTab.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                    break;
                case "History":
                    HistoryTab.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                    break;
            }
        }
    }

    // ==========================================
    // DATE FILTERING
    // ==========================================
    private void DateFilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (MyStatsVM == null) return;

        var selectedItem = (ComboBoxItem)DateFilterCombo.SelectedItem;
        if (Enum.TryParse(typeof(DateRangeFilter), selectedItem.Tag.ToString(), out var filterVal))
        {
            // Call your GeneralStatsService to update data streams!
            // E.g., _generalStatsService.SetDateFilter((DateRangeFilter)filterVal);
        }
    }

    // ==========================================
    // CONTEXT MENU (PREMIUM STYLING)
    // ==========================================
    private void MoreBtn_Click(object sender, RoutedEventArgs e)
    {
        var btn = (Button)sender;
        var evt = btn.DataContext as DimmerPlayEventView;
        if (evt == null) return;

        var flyout = new MenuFlyout();

        // Play Now
        flyout.Items.Add(new MenuFlyoutItem { Text = "Play Now", Icon = new FontIcon { Glyph = "\uE768" } });

        // Favorite Toggle
        flyout.Items.Add(new MenuFlyoutItem
        {
            Text = evt.SongViewObject?.IsFavorite == true ? "Unfavorite" : "Favorite",
            Icon = new FontIcon { Glyph = "\uEB51", Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.HotPink) }
        });

        flyout.Items.Add(new MenuFlyoutSeparator());

        // Add Note
        flyout.Items.Add(new MenuFlyoutItem { Text = "Add Note to Song", Icon = new FontIcon { Glyph = "\uF7BB" } });

        // Go to Artist / Album SubMenu
        var navigateSub = new MenuFlyoutSubItem { Text = "Go to...", Icon = new FontIcon { Glyph = "\uE8A0" } };
        navigateSub.Items.Add(new MenuFlyoutItem { Text = "Artist", Icon = new FontIcon { Glyph = "\uE720" } });
        navigateSub.Items.Add(new MenuFlyoutItem { Text = "Album", Icon = new FontIcon { Glyph = "\uE93C" } });
        flyout.Items.Add(navigateSub);

        flyout.Items.Add(new MenuFlyoutSeparator());

        // Delete Event from History
        var deleteItem = new MenuFlyoutItem { Text = "Remove from History", Icon = new FontIcon { Glyph = "\uE74D" } };
        // deleteItem.Click += (s, args) => MyViewModel.DeleteHistoryEvent(evt);
        flyout.Items.Add(deleteItem);

        flyout.ShowAt(btn, new FlyoutShowOptions { Placement = FlyoutPlacementMode.BottomEdgeAlignedRight });
    }

    private void OverviewTab_Loaded(object sender, RoutedEventArgs e)
    {

        MyGeneralStatsVM ??= IPlatformApplication.Current.Services.GetService<GeneralStatsViewModel>();
        OverviewTab.DataContext = MyGeneralStatsVM;
    }

    private void EventTypesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox comboBox) return;
        if (MyEventsTableView is null)
        {
            return;
        }
        // Get the selected string (e.g., "Completed", "Skipped", or "All")
        string? selectedItem = comboBox.SelectedItem switch
        {
            ComboBoxItem cbi => cbi.Content?.ToString(),
            string str => str,
            _ => null
        };

        string propertyName = nameof(DimmerPlayEventView.PlayTypeStr);

        // 1. Remove any existing filter on this column to avoid stacking conflicting filters
        var existingFilter = MyEventsTableView.FilterDescriptions
            .FirstOrDefault(f => f.PropertyName == propertyName);

        if (existingFilter != null)
        {
            MyEventsTableView.FilterDescriptions.Remove(existingFilter);
        }

        // 2. If "All" or null is selected, do not apply a filter (show everything)
        if (string.IsNullOrWhiteSpace(selectedItem) ||
            selectedItem.Equals("All", StringComparison.OrdinalIgnoreCase) ||
            selectedItem.Equals("All Event Types", StringComparison.OrdinalIgnoreCase))
        {
            MyEventsTableView.FilterDescriptions.Clear(); // Clear all filters if "All" is selected
            return;
        }

        // 3. Define the Predicate (safely checks row object OR string value)
        Predicate<object?> predicate = itemOrValue =>
        {
            // Case A: The TableView passes the entire DimmerPlayEventView row object
            if (itemOrValue is DimmerPlayEventView row)
            {
                return string.Equals(row.PlayTypeStr, selectedItem, StringComparison.OrdinalIgnoreCase);
            }

            // Case B: The TableView passes just the property value (string)
            if (itemOrValue is string strVal)
            {
                return string.Equals(strVal, selectedItem, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        };

        // 4. Add the new filter description
        MyEventsTableView.FilterDescriptions.Add(new FilterDescription(propertyName, predicate));
    }
}
