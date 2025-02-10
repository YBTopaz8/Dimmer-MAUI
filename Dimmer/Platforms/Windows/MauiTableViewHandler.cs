// File: Platforms/Windows/TableViewImplementation.cs
using Microsoft.Maui.Controls;
using WinUI.TableView;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using System.ComponentModel;
using Microsoft.Maui.Platform;
using WinUIGrid = Microsoft.UI.Xaml.Controls.Grid;
using WinUITableview = WinUI.TableView.TableView;
using Microsoft.Maui.Handlers;
using ListViewSelectionMode = Microsoft.UI.Xaml.Controls.ListViewSelectionMode;
using Microsoft.UI.Xaml.Media.Animation;
using DataTemplate = Microsoft.UI.Xaml.DataTemplate;
using static Dimmer_MAUI.Platforms.Windows.PlatSpecificUtils;
using Microsoft.UI.Xaml.Input;

namespace Dimmer_MAUI.Platforms.Windows;

public class TableViewImplementation : WinUIGrid, INotifyPropertyChanged
{
    private WinUITableview _tableView;
    private bool _autoGenerateColumns;

    public TableViewImplementation()
    {
        _tableView = new WinUITableview();

        AutoGenerateColumns = true;
        ItemsSource = null;
        Children.Add(_tableView);

        try
        {
            this.Loaded += TableViewImplementation_Loaded;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error initializing TableView: {ex.Message}");
        }
    }

    private void TableViewImplementation_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateTableView();
    }

    public object? ItemsSource
    {
        get => _tableView.ItemsSource;
        set
        {
            _tableView.ItemsSource = (System.Collections.IList?)value;
            OnPropertyChanged(nameof(ItemsSource));
            UpdateTableView();
        }
    }

    public bool AutoGenerateColumns
    {
        get => _autoGenerateColumns;
        set
        {
            _autoGenerateColumns = value;
            _tableView.AutoGenerateColumns = value;
            OnPropertyChanged(nameof(AutoGenerateColumns));
            UpdateTableView();
        }
    }
  
    // --- Exposed native properties ---
    public bool CanDragItems
    {
        get => _tableView.CanDragItems;
        set { _tableView.CanDragItems = value; OnPropertyChanged(nameof(CanDragItems)); }
    }

    public bool CanReorderItems
    {
        get => _tableView.CanReorderItems;
        set { _tableView.CanReorderItems = value; OnPropertyChanged(nameof(CanReorderItems)); }
    }

    public double DataFetchSize
    {
        get => _tableView.DataFetchSize;
        set { _tableView.DataFetchSize = value; OnPropertyChanged(nameof(DataFetchSize)); }
    }

    public object Footer
    {
        get => _tableView.Footer;
        set { _tableView.Footer = value; OnPropertyChanged(nameof(Footer)); }
    }

    public DataTemplate FooterTemplate
    {
        get => _tableView.FooterTemplate;
        set { _tableView.FooterTemplate = value; OnPropertyChanged(nameof(FooterTemplate)); }
    }

    public TransitionCollection FooterTransitions
    {
        get => _tableView.FooterTransitions;
        set { _tableView.FooterTransitions = value; OnPropertyChanged(nameof(FooterTransitions)); }
    }

    public object Header
    {
        get => _tableView.Header;
        set { _tableView.Header = value; OnPropertyChanged(nameof(Header)); }
    }

    public DataTemplate HeaderTemplate
    {
        get => _tableView.HeaderTemplate;
        set { _tableView.HeaderTemplate = value; OnPropertyChanged(nameof(HeaderTemplate)); }
    }

    public TransitionCollection HeaderTransitions
    {
        get => _tableView.HeaderTransitions;
        set { _tableView.HeaderTransitions = value; OnPropertyChanged(nameof(HeaderTransitions)); }
    }

    public double IncrementalLoadingThreshold
    {
        get => _tableView.IncrementalLoadingThreshold;
        set { _tableView.IncrementalLoadingThreshold = value; OnPropertyChanged(nameof(IncrementalLoadingThreshold)); }
    }

    public IncrementalLoadingTrigger IncrementalLoadingTrigger
    {
        get => _tableView.IncrementalLoadingTrigger;
        set { _tableView.IncrementalLoadingTrigger = value; OnPropertyChanged(nameof(IncrementalLoadingTrigger)); }
    }

    public bool IsActiveView
    {
        get => _tableView.IsActiveView;
        set { _tableView.IsActiveView = value; OnPropertyChanged(nameof(IsActiveView)); }
    }

    public bool IsItemClickEnabled
    {
        get => _tableView.IsItemClickEnabled;
        set { _tableView.IsItemClickEnabled = value; OnPropertyChanged(nameof(IsItemClickEnabled)); }
    }

    public bool IsMultiSelectCheckBoxEnabled
    {
        get => _tableView.IsMultiSelectCheckBoxEnabled;
        set { _tableView.IsMultiSelectCheckBoxEnabled = value; OnPropertyChanged(nameof(IsMultiSelectCheckBoxEnabled)); }
    }

    public bool IsSwipeEnabled
    {
        get => _tableView.IsSwipeEnabled;
        set { _tableView.IsSwipeEnabled = value; OnPropertyChanged(nameof(IsSwipeEnabled)); }
    }

    public bool IsZoomedInView
    {
        get => _tableView.IsZoomedInView;
        set { _tableView.IsZoomedInView = value; OnPropertyChanged(nameof(IsZoomedInView)); }
    }

    public ListViewReorderMode ReorderMode
    {
        get => _tableView.ReorderMode;
        set { _tableView.ReorderMode = value; OnPropertyChanged(nameof(ReorderMode)); }
    }

    public ListViewSelectionMode SelectionMode
    {
        get => _tableView.SelectionMode;
        set { _tableView.SelectionMode = value; OnPropertyChanged(nameof(SelectionMode)); }
    }


    public bool ShowsScrollingPlaceholders
    {
        get => _tableView.ShowsScrollingPlaceholders;
        set { _tableView.ShowsScrollingPlaceholders = value; OnPropertyChanged(nameof(ShowsScrollingPlaceholders)); }
    }

    public bool SingleSelectionFollowsFocus
    {
        get => _tableView.SingleSelectionFollowsFocus;
        set { _tableView.SingleSelectionFollowsFocus = value; OnPropertyChanged(nameof(SingleSelectionFollowsFocus)); }
    }


    private void UpdateTableView()
    {
        if (_tableView != null)
        {
            if (ItemsSource != null)
                _tableView.ItemsSource = (System.Collections.IList?)ItemsSource;
            _tableView.Columns.Clear();
            TableViewColumnsCollection cols = new();
            
            _tableView.AutoGenerateColumns = AutoGenerateColumns;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

// Default mappers for MyTableViewHandler
public static class MyTableViewHandlerMapper
{
    public static IPropertyMapper<MyTableView, MyTableViewHandler> Mapper =
    new PropertyMapper<MyTableView, MyTableViewHandler>(ViewHandler.ViewMapper)
    {
        [nameof(MyTableView.ItemsSource)] = MapItemsSource,
        [nameof(MyTableView.AutoGenerateColumns)] = MapAutoGenerateColumns,
        [nameof(MyTableView.CanDragItems)] = MapCanDragItems,
        [nameof(MyTableView.CanReorderItems)] = MapCanReorderItems,
        [nameof(MyTableView.DataFetchSize)] = MapDataFetchSize,
        [nameof(MyTableView.Footer)] = MapFooter,
        [nameof(MyTableView.FooterTemplate)] = MapFooterTemplate,
        [nameof(MyTableView.FooterTransitions)] = MapFooterTransitions,
        [nameof(MyTableView.Header)] = MapHeader,
        [nameof(MyTableView.HeaderTemplate)] = MapHeaderTemplate,
        [nameof(MyTableView.HeaderTransitions)] = MapHeaderTransitions,
        [nameof(MyTableView.IncrementalLoadingThreshold)] = MapIncrementalLoadingThreshold,
        [nameof(MyTableView.IncrementalLoadingTrigger)] = MapIncrementalLoadingTrigger,
        [nameof(MyTableView.IsActiveView)] = MapIsActiveView,
        [nameof(MyTableView.IsItemClickEnabled)] = MapIsItemClickEnabled,
        [nameof(MyTableView.IsMultiSelectCheckBoxEnabled)] = MapIsMultiSelectCheckBoxEnabled,
        [nameof(MyTableView.IsSwipeEnabled)] = MapIsSwipeEnabled,
        [nameof(MyTableView.IsZoomedInView)] = MapIsZoomedInView,
        [nameof(MyTableView.ReorderMode)] = MapReorderMode,
        [nameof(MyTableView.SelectionMode)] = MapSelectionMode,
        [nameof(MyTableView.ShowsScrollingPlaceholders)] = MapShowsScrollingPlaceholders,
        [nameof(MyTableView.HeaderRowHeight)] = MapHeaderRowHeight,
        [nameof(MyTableView.RowHeight)] = MapRowHeight,
        [nameof(MyTableView.RowMaxHeight)] = MapRowMaxHeight,
        [nameof(MyTableView.ShowExportOptions)] = MapShowExportOptions,
        [nameof(MyTableView.SingleSelectionFollowsFocus)] = MapSingleSelectionFollowsFocus,
        // Additional native property mappings:
        [nameof(MyTableView.HeaderRowHeight)] = MapHeaderRowHeight,
        [nameof(MyTableView.RowHeight)] = MapRowHeight,
        [nameof(MyTableView.RowMaxHeight)] = MapRowMaxHeight,
        [nameof(MyTableView.ShowExportOptions)] = MapShowExportOptions,
        [nameof(MyTableView.IsReadOnly)] = MapIsReadOnly,
        [nameof(MyTableView.CornerButtonMode)] = MapCornerButtonMode,
        [nameof(MyTableView.CanResizeColumns)] = MapCanResizeColumns,
        [nameof(MyTableView.CanSortColumns)] = MapCanSortColumns,
        [nameof(MyTableView.CanFilterColumns)] = MapCanFilterColumns,
        [nameof(MyTableView.MinColumnWidth)] = MapMinColumnWidth,
        [nameof(MyTableView.MaxColumnWidth)] = MapMaxColumnWidth,
        [nameof(MyTableView.SelectionUnit)] = MapSelectionUnit,
        //[nameof(MyTableView.HeaderGridLinesVisibility)] = MapHeaderGridLinesVisibility,
        //[nameof(MyTableView.GridLinesVisibility)] = MapGridLinesVisibility,
        //[nameof(MyTableView.HorizontalGridLinesStrokeThickness)] = MapHorizontalGridLinesStrokeThickness,
        //[nameof(MyTableView.VerticalGridLinesStrokeThickness)] = MapVerticalGridLinesStrokeThickness,
        //[nameof(MyTableView.HorizontalGridLinesStroke)] = MapHorizontalGridLinesStroke,
        //[nameof(MyTableView.VerticalGridLinesStroke)] = MapVerticalGridLinesStroke,
        //[nameof(MyTableView.AlternateRowForeground)] = MapAlternateRowForeground,
        //[nameof(MyTableView.AlternateRowBackground)] = MapAlternateRowBackground,
        [nameof(MyTableView.RowContextFlyout)] = MapRowContextFlyout,
        [nameof(MyTableView.CellContextFlyout)] = MapCellContextFlyout,
        [nameof(MyTableView.ColumnHeaderStyle)] = MapColumnHeaderStyle,
        [nameof(MyTableView.CellStyle)] = MapCellStyle

    };

    public static CommandMapper<MyTableView, MyTableViewHandler> CommandMapper =
        new CommandMapper<MyTableView, MyTableViewHandler>(ViewHandler.ViewCommandMapper);


    static void MapHeaderRowHeight(MyTableViewHandler handler, MyTableView view)
    {
        var native = GetNativeTable(handler);
        if (native != null)
            native.SetValue(WinUITableview.HeaderRowHeightProperty, view.HeaderRowHeight);
    }

    static void MapRowHeight(MyTableViewHandler handler, MyTableView view)
    {
        var native = GetNativeTable(handler);
        if (native != null)
            native.SetValue(WinUITableview.RowHeightProperty, view.RowHeight);
    }

    static void MapRowMaxHeight(MyTableViewHandler handler, MyTableView view)
    {
        var native = GetNativeTable(handler);
        if (native != null)
            native.SetValue(WinUITableview.RowMaxHeightProperty, view.RowMaxHeight);
    }

    static void MapShowExportOptions(MyTableViewHandler handler, MyTableView view)
    {
        var native = GetNativeTable(handler);
        if (native != null)
            native.SetValue(WinUITableview.ShowExportOptionsProperty, view.ShowExportOptions);
    }

    static TableViewImplementation GetNativeTable(MyTableViewHandler handler)
    {
        if (handler.PlatformView is Microsoft.UI.Xaml.Controls.Grid grid)
            return grid.Children.FirstOrDefault() as TableViewImplementation;
        return null;
    }

    static void MapItemsSource(MyTableViewHandler handler, MyTableView view)
    {
        var native = GetNativeTable(handler);
        if (native != null)
            native.ItemsSource = view.ItemsSource;
    }
    static void MapAutoGenerateColumns(MyTableViewHandler handler, MyTableView view)
    {
        var native = GetNativeTable(handler);
        if (native != null)
            native.AutoGenerateColumns = view.AutoGenerateColumns;
    }
    static void MapCanDragItems(MyTableViewHandler handler, MyTableView view)
    {
        var native = GetNativeTable(handler);
        if (native != null)
            native.CanDragItems = view.CanDragItems;
    }
    static void MapCanReorderItems(MyTableViewHandler handler, MyTableView view)
    {
        var native = GetNativeTable(handler);
        if (native != null)
            native.CanReorderItems = view.CanReorderItems;
    }
    static void MapDataFetchSize(MyTableViewHandler handler, MyTableView view)
    {
        var native = GetNativeTable(handler);
        if (native != null)
            native.DataFetchSize = view.DataFetchSize;
    }
    static void MapFooter(MyTableViewHandler handler, MyTableView view)
    {
        var native = GetNativeTable(handler);
        if (native != null)
            native.Footer = view.Footer;
    }
    static void MapFooterTemplate(MyTableViewHandler handler, MyTableView view)
    {
        var native = GetNativeTable(handler);
        if (native != null)
            native.FooterTemplate = DataTemplateConversion.ConvertToWindowsDataTemplate(view.FooterTemplate);
    }
    static void MapFooterTransitions(MyTableViewHandler handler, MyTableView view)
    {
        var native = GetNativeTable(handler);
        if (native != null)
            native.FooterTransitions = view.FooterTransitions;
    }
    static void MapHeader(MyTableViewHandler handler, MyTableView view)
    {
        var native = GetNativeTable(handler);
        if (native != null)
            native.Header = view.Header;
    }
    static void MapHeaderTemplate(MyTableViewHandler handler, MyTableView view)
    {
        var native = GetNativeTable(handler);
        if (native != null)
            native.HeaderTemplate = DataTemplateConversion.ConvertToWindowsDataTemplate(view.HeaderTemplate);
    }
    static void MapHeaderTransitions(MyTableViewHandler handler, MyTableView view)
    {
        var native = GetNativeTable(handler);
        if (native != null)
            native.HeaderTransitions = view.HeaderTransitions;
    }
    static void MapIncrementalLoadingThreshold(MyTableViewHandler handler, MyTableView view)
    {
        var native = GetNativeTable(handler);
        if (native != null)
            native.IncrementalLoadingThreshold = view.IncrementalLoadingThreshold;
    }
    static void MapIncrementalLoadingTrigger(MyTableViewHandler handler, MyTableView view)
    {
        var native = GetNativeTable(handler);
        if (native != null)
            native.IncrementalLoadingTrigger = view.IncrementalLoadingTrigger;
    }
    static void MapIsActiveView(MyTableViewHandler handler, MyTableView view)
    {
        var native = GetNativeTable(handler);
        if (native != null)
            native.IsActiveView = view.IsActiveView;
    }
    static void MapIsItemClickEnabled(MyTableViewHandler handler, MyTableView view)
    {
        var native = GetNativeTable(handler);
        if (native != null)
            native.IsItemClickEnabled = view.IsItemClickEnabled;
    }
    static void MapIsMultiSelectCheckBoxEnabled(MyTableViewHandler handler, MyTableView view)
    {
        var native = GetNativeTable(handler);
        if (native != null)
            native.IsMultiSelectCheckBoxEnabled = view.IsMultiSelectCheckBoxEnabled;
    }
    static void MapIsSwipeEnabled(MyTableViewHandler handler, MyTableView view)
    {
        var native = GetNativeTable(handler);
        if (native != null)
            native.IsSwipeEnabled = view.IsSwipeEnabled;
    }
    static void MapIsZoomedInView(MyTableViewHandler handler, MyTableView view)
    {
        var native = GetNativeTable(handler);
        if (native != null)
            native.IsZoomedInView = view.IsZoomedInView;
    }
    static void MapReorderMode(MyTableViewHandler handler, MyTableView view)
    {
        var native = GetNativeTable(handler);
        if (native != null)
            native.ReorderMode = view.ReorderMode;
    }
    static void MapSelectionMode(MyTableViewHandler handler, MyTableView view)
    {
        var native = GetNativeTable(handler);
        if (native != null)
            native.SelectionMode = view.SelectionMode;
    }
   
    static void MapShowsScrollingPlaceholders(MyTableViewHandler handler, MyTableView view)
    {
        var native = GetNativeTable(handler);
        if (native != null)
            native.ShowsScrollingPlaceholders = view.ShowsScrollingPlaceholders;
    }
    static void MapSingleSelectionFollowsFocus(MyTableViewHandler handler, MyTableView view)
    {
        var native = GetNativeTable(handler);
        if (native != null)
            native.SingleSelectionFollowsFocus = view.SingleSelectionFollowsFocus;
    }


    //static void MapHeaderTransitions(MyTableViewHandler handler, MyTableView view)
    //{
    //    var native = GetNativeTable(handler);
    //    if (native != null)
    //        native.HeaderTransitions = view.HeaderTransitions;
    //}
    //static void MapIncrementalLoadingThreshold(MyTableViewHandler handler, MyTableView view)
    //{
    //    var native = GetNativeTable(handler);
    //    if (native != null)
    //        native.IncrementalLoadingThreshold = view.IncrementalLoadingThreshold;
    //}
    //static void MapIncrementalLoadingTrigger(MyTableViewHandler handler, MyTableView view)
    //{
    //    var native = GetNativeTable(handler);
    //    if (native != null)
    //        native.IncrementalLoadingTrigger = view.IncrementalLoadingTrigger;
    //}
    //static void MapIsActiveView(MyTableViewHandler handler, MyTableView view)
    //{
    //    var native = GetNativeTable(handler);
    //    if (native != null)
    //        native.IsActiveView = view.IsActiveView;
    //}
    //static void MapIsItemClickEnabled(MyTableViewHandler handler, MyTableView view)
    //{
    //    var native = GetNativeTable(handler);
    //    if (native != null)
    //        native.IsItemClickEnabled = view.IsItemClickEnabled;
    //}
    //static void MapIsMultiSelectCheckBoxEnabled(MyTableViewHandler handler, MyTableView view)
    //{
    //    var native = GetNativeTable(handler);
    //    if (native != null)
    //        native.IsMultiSelectCheckBoxEnabled = view.IsMultiSelectCheckBoxEnabled;
    //}
    //static void MapIsSwipeEnabled(MyTableViewHandler handler, MyTableView view)
    //{
    //    var native = GetNativeTable(handler);
    //    if (native != null)
    //        native.IsSwipeEnabled = view.IsSwipeEnabled;
    //}
    //static void MapIsZoomedInView(MyTableViewHandler handler, MyTableView view)
    //{
    //    var native = GetNativeTable(handler);
    //    if (native != null)
    //        native.IsZoomedInView = view.IsZoomedInView;
    //}
    //static void MapReorderMode(MyTableViewHandler handler, MyTableView view)
    //{
    //    var native = GetNativeTable(handler);
    //    if (native != null)
    //        native.ReorderMode = view.ReorderMode;
    //}
    //static void MapSelectionMode(MyTableViewHandler handler, MyTableView view)
    //{
    //    var native = GetNativeTable(handler);
    //    if (native != null)
    //        native.SelectionMode = view.SelectionMode;
    //}
    //static void MapShowsScrollingPlaceholders(MyTableViewHandler handler, MyTableView view)
    //{
    //    var native = GetNativeTable(handler);
    //    if (native != null)
    //        native.ShowsScrollingPlaceholders = view.ShowsScrollingPlaceholders;
    //}
    //static void MapHeaderRowHeight(MyTableViewHandler handler, MyTableView view)
    //{
    //    var native = GetNativeTable(handler);
    //    if (native != null)
    //        native.Header .SetValue(WinUITableview.HeaderRowHeightProperty, view.HeaderRowHeight);
    //}
    //static void MapRowHeight(MyTableViewHandler handler, MyTableView view)
    //{
    //    var native = GetNativeTable(handler);
    //    if (native != null)
    //        native.SetValue(WinUITableview.RowHeightProperty, view.RowHeight);
    //}
    //static void MapRowMaxHeight(MyTableViewHandler handler, MyTableView view)
    //{
    //    var native = GetNativeTable(handler);
    //    if (native != null)
    //        native..SetValue(TableView.RowMaxHeightProperty, view.RowMaxHeight);
    //}
    //static void MapShowExportOptions(MyTableViewHandler handler, MyTableView view)
    //{
    //    var native = GetNativeTable(handler);
    //    if (native != null)
    //        native.SetValue(WinUITableview.ShowExportOptionsProperty, view.ShowExportOptions);
    //}
    static void MapIsReadOnly(MyTableViewHandler handler, MyTableView view)
    {
        var native = GetNativeTable(handler);
        if (native != null)
            native.SetValue(WinUITableview.IsReadOnlyProperty, view.IsReadOnly);
    }
    static void MapCornerButtonMode(MyTableViewHandler handler, MyTableView view)
    {
        var native = GetNativeTable(handler);
        if (native != null)
            native.SetValue(WinUITableview.ShowOptionsButtonProperty, view.CornerButtonMode);
    }
    static void MapCanResizeColumns(MyTableViewHandler handler, MyTableView view)
    {
        var native = GetNativeTable(handler);
        if (native != null)
            native.SetValue(WinUITableview.CanResizeColumnsProperty, view.CanResizeColumns);
    }
    static void MapCanSortColumns(MyTableViewHandler handler, MyTableView view)
    {
        var native = GetNativeTable(handler);
        if (native != null)
            native.SetValue(WinUITableview.CanSortColumnsProperty, view.CanSortColumns);
    }
    static void MapCanFilterColumns(MyTableViewHandler handler, MyTableView view)
    {
        var native = GetNativeTable(handler);
        if (native != null)
            native.SetValue(WinUITableview.CanFilterColumnsProperty, view.CanFilterColumns);
    }
    static void MapMinColumnWidth(MyTableViewHandler handler, MyTableView view)
    {
        var native = GetNativeTable(handler);
        if (native != null)
            native.SetValue(WinUITableview.MinColumnWidthProperty, view.MinColumnWidth);
    }
    static void MapMaxColumnWidth(MyTableViewHandler handler, MyTableView view)
    {
        var native = GetNativeTable(handler);
        if (native != null)
            native.SetValue(WinUITableview.MaxColumnWidthProperty, view.MaxColumnWidth);
    }
    static void MapSelectionUnit(MyTableViewHandler handler, MyTableView view)
    {
        var native = GetNativeTable(handler);
        if (native != null)
            native.SetValue(WinUITableview.SelectionUnitProperty, view.SelectionUnit);
    }
    
    static void MapRowContextFlyout(MyTableViewHandler handler, MyTableView view)
    {
        var native = GetNativeTable(handler);
        if (native != null)
            native.SetValue(WinUITableview.ContextFlyoutProperty, view.RowContextFlyout);
    }
    static void MapCellContextFlyout(MyTableViewHandler handler, MyTableView view)
    {
        var native = GetNativeTable(handler);
        if (native != null)
            native.SetValue(WinUITableview.ContextFlyoutProperty, view.CellContextFlyout);
    }
    static void MapColumnHeaderStyle(MyTableViewHandler handler, MyTableView view)
    {
        var native = GetNativeTable(handler);
        if (native != null)
            native.SetValue(WinUITableview.StyleProperty, view.ColumnHeaderStyle);
    }
    static void MapCellStyle(MyTableViewHandler handler, MyTableView view)
    {
        var native = GetNativeTable(handler);
        if (native != null)
            native.SetValue(WinUITableview.StyleProperty, view.CellStyle);
    }
}

public class MyTableViewHandler : ViewHandler<MyTableView, WinUIGrid>
{
    // Parameterless constructor required for reflection/DI.
    public MyTableViewHandler()
        : base(MyTableViewHandlerMapper.Mapper, MyTableViewHandlerMapper.CommandMapper)
    {
    }

    protected override WinUIGrid CreatePlatformView() =>
        new WinUIGrid();

    protected override void ConnectHandler(WinUIGrid platformView)
    {
        base.ConnectHandler(platformView);
        if (VirtualView != null)
        {
            var nativeTable = new TableViewImplementation
            {
                ItemsSource = VirtualView.ItemsSource,
                AutoGenerateColumns = VirtualView.AutoGenerateColumns,
                CanDragItems = VirtualView.CanDragItems,
                CanReorderItems = VirtualView.CanReorderItems,
                DataFetchSize = VirtualView.DataFetchSize,
                Footer = VirtualView.Footer,
                //FooterTemplate = VirtualView.FooterTemplate,
                FooterTransitions = VirtualView.FooterTransitions,
                Header = VirtualView.Header,
                //HeaderTemplate = VirtualView.HeaderTemplate,
                HeaderTransitions = VirtualView.HeaderTransitions,
                IncrementalLoadingThreshold = VirtualView.IncrementalLoadingThreshold,
                IncrementalLoadingTrigger = VirtualView.IncrementalLoadingTrigger,
                IsActiveView = VirtualView.IsActiveView,
                IsItemClickEnabled = VirtualView.IsItemClickEnabled,
                IsMultiSelectCheckBoxEnabled = VirtualView.IsMultiSelectCheckBoxEnabled,
                IsSwipeEnabled = VirtualView.IsSwipeEnabled,
                IsZoomedInView = VirtualView.IsZoomedInView,
                ReorderMode = VirtualView.ReorderMode,
                SelectionMode = VirtualView.SelectionMode,
    
                ShowsScrollingPlaceholders = VirtualView.ShowsScrollingPlaceholders,
                SingleSelectionFollowsFocus = VirtualView.SingleSelectionFollowsFocus
            };

            // Subscribe to native events:
            nativeTable.Loaded += NativeTable_Loaded;
            nativeTable.Unloaded += NativeTable_Unloaded;
            nativeTable.DoubleTapped += NativeTable_DoubleTapped;
            nativeTable.PointerEntered += NativeTable_PointerEntered;
            nativeTable.PointerExited += NativeTable_PointerExited;
            nativeTable.PointerMoved += NativeTable_PointerMoved;
            nativeTable.PointerPressed += NativeTable_PointerPressed;
            nativeTable.PointerReleased += NativeTable_PointerReleased;
            nativeTable.PointerWheelChanged += NativeTable_PointerWheelChanged;
            nativeTable.RightTapped += NativeTable_RightTapped;
            nativeTable.Tapped += NativeTable_Tapped;
            nativeTable.CharacterReceived += NativeTable_CharacterReceived;
            nativeTable.ContextRequested += NativeTable_ContextRequested;
            nativeTable.DataContextChanged += NativeTable_DataContextChanged;

            platformView.Children.Add(nativeTable);
        }
    }

    protected override void DisconnectHandler(WinUIGrid platformView)
    {
        if (platformView.Children.FirstOrDefault() is TableViewImplementation nativeTable)
        {
            nativeTable.Loaded -= (s, e) => VirtualView?.RaiseLoaded(e);
            nativeTable.Unloaded -= (s, e) => VirtualView?.RaiseUnloaded(e);
            nativeTable.DoubleTapped -= (s, e) => VirtualView?.RaiseDoubleTapped(e);
            nativeTable.PointerEntered -= (s, e) => VirtualView?.RaisePointerEntered(e);
            nativeTable.PointerExited -= (s, e) => VirtualView?.RaisePointerExited(e);
            nativeTable.PointerMoved -= (s, e) => VirtualView?.RaisePointerMoved(e);
            nativeTable.PointerPressed -= (s, e) => VirtualView?.RaisePointerPressed(e);
            nativeTable.PointerReleased -= (s, e) => VirtualView?.RaisePointerReleased(e);
            nativeTable.PointerWheelChanged -= (s, e) => VirtualView?.RaisePointerWheelChanged(e);
            nativeTable.RightTapped -= (s, e) => VirtualView?.RaiseRightTapped(e);
            nativeTable.Tapped -= (s, e) => VirtualView?.RaiseTapped(e);
            nativeTable.CharacterReceived -= (s, e) => VirtualView?.RaiseCharacterReceived(e);
            nativeTable.ContextRequested -= (s, e) => VirtualView?.RaiseContextRequested(e);
            nativeTable.DataContextChanged -= (s, e) => VirtualView?.RaiseDataContextChanged(e);
            nativeTable.Holding -= (s, e) => VirtualView?.RaiseHolding(e);
            nativeTable.KeyDown -= (s, e) => VirtualView?.RaiseKeyDown(e);
            nativeTable.KeyUp -= (s, e) => VirtualView?.RaiseKeyUp(e);
            
            //nativeTable.ManipulationCompleted -= (s, e) => VirtualView?.RaiseManipulationCompleted(e);
            //nativeTable.ManipulationDelta -= (s, e) => VirtualView?.RaiseManipulationDelta(e);
            //nativeTable.ManipulationInertiaStarting -= (s, e) => VirtualView?.RaiseManipulationInertiaStarting(e);
            //nativeTable.ManipulationStarted -= (s, e) => VirtualView?.RaiseManipulationStarted(e);
            //nativeTable.ManipulationStarting -= (s, e) => VirtualView?.RaiseManipulationStarting(e);
            //nativeTable.PointerCanceled -= (s, e) => VirtualView?.RaisePointerCanceled(e);
            //nativeTable.PointerCaptureLost -= (s, e) => VirtualView?.RaisePointerCaptureLost(e);
        }
        base.DisconnectHandler(platformView);
    }

    // Event forwarding methods:
    private void NativeTable_Loaded(object sender, RoutedEventArgs e) => VirtualView?.RaiseLoaded(e);

    private void NativeTable_Unloaded(object sender, RoutedEventArgs e) => VirtualView?.RaiseUnloaded(e);

    private void NativeTable_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        VirtualView?.RaiseDoubleTapped(e);
    }

    private void NativeTable_PointerEntered(object sender, PointerRoutedEventArgs e) => VirtualView?.RaisePointerEntered(e);
    

    private void NativeTable_PointerExited(object sender, PointerRoutedEventArgs e) => VirtualView?.RaisePointerExited(e);

    private void NativeTable_PointerMoved(object sender, PointerRoutedEventArgs e) => VirtualView?.RaisePointerMoved(e);

    private void NativeTable_PointerPressed(object sender, PointerRoutedEventArgs e) => VirtualView?.RaisePointerPressed(e);

    private void NativeTable_PointerReleased(object sender, PointerRoutedEventArgs e) => VirtualView?.RaisePointerReleased(e);

    private void NativeTable_PointerWheelChanged(object sender, PointerRoutedEventArgs e) => VirtualView?.RaisePointerWheelChanged(e);

    private void NativeTable_RightTapped(object sender, RightTappedRoutedEventArgs e) => VirtualView?.RaiseRightTapped(e);

    private void NativeTable_Tapped(object sender, TappedRoutedEventArgs e) => VirtualView?.RaiseTapped(e);

    private void NativeTable_CharacterReceived(object sender, CharacterReceivedRoutedEventArgs e) => VirtualView?.RaiseCharacterReceived(e);

    private void NativeTable_ContextRequested(object sender, ContextRequestedEventArgs e) => VirtualView?.RaiseContextRequested(e);

    private void NativeTable_DataContextChanged(object sender, DataContextChangedEventArgs e) => VirtualView?.RaiseDataContextChanged(e);

}
