using DynamicData.Binding;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;

namespace Dimmer.WinUI.Views.WinuiPages.Artist;

public sealed partial class AllArtistsPage : Page
{
    CompositeDisposable compDisp;
    public BaseViewModelWin MyViewModel { get; set; }

    public AllArtistsPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        compDisp?.Dispose();
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        compDisp = new();
        MyViewModel = IPlatformApplication.Current!.Services.GetService<BaseViewModelWin>()!;

        DataContext = MyViewModel;
        MyViewModel.CurrentPageEnum = CurrentPage.AllArtistsPage;
        MyViewModel.SetupArtistPipeline();

        MyViewModel.WhenPropertyChanged(nameof(MyViewModel.IsArtistInitialized), v => MyViewModel.ArtistsCollection)
            .Subscribe(s =>
            {
                var send = this.FilterArtistName;
                if (!send.IsLoaded) return;
                var namesList = MyViewModel.ArtistsCollection.Where(x => x is not null).Select(x => x.Name).Distinct().ToList();
                send.ItemsSource = namesList;

                // Initialize A-Z Grouping
                ApplyAZGrouping();
            }).DisposeWith(compDisp);
    }

    // ==========================================
    // UI ELEVATION: A-Z GROUPING
    // ==========================================
    private void ApplyAZGrouping()
    {

        // Check if your TableView supports GroupDescriptions. 
        // Since you provided GroupDescription, it likely has a GroupDescriptions collection or a GroupBy method.
        // We group by the first character of the Artist's name.
        if (AllArtistsTableView.GroupDescriptions != null && AllArtistsTableView.GroupDescriptions.Count == 0)
        {
            AllArtistsTableView.GroupDescriptions.Add(new GroupDescription(
                propertyName: "Name",
                valueDelegate: obj =>
                {
                    if (obj is ArtistModelView artist && !string.IsNullOrWhiteSpace(artist.Name))
                    {
                        char firstChar = artist.Name.ToUpper()[0];
                        return char.IsLetter(firstChar) ? firstChar.ToString() : "#";
                    }
                    return "?";
                }));
        }
    }

    // ==========================================
    // UI ELEVATION: REAL-TIME FILTERING
    // ==========================================
    private void FilterArtistName_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            string query = sender.Text.ToLower().Trim();

            // Clear previous filters
            AllArtistsTableView.FilterDescriptions?.Clear();

            // Apply new filter if text isn't empty
            if (!string.IsNullOrEmpty(query))
            {
                AllArtistsTableView.FilterDescriptions?.Add(new FilterDescription(
                    propertyName: "Name",
                    predicate: obj =>
                    {
                        if (obj is ArtistModelView artist && !string.IsNullOrWhiteSpace(artist.Name))
                        {
                            return artist.Name.ToLower().Contains(query);
                        }
                        return false;
                    }));
            }
        }
    }

    private void FilterArtistName_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        var selectedValString = args.SelectedItem as string;
        var firstItem = MyViewModel.ArtistsCollection.FirstOrDefault(x => x.Name == selectedValString);

        if (firstItem == null) return;

        // Ensure re-entrancy safety
        DispatcherQueue.TryEnqueue(() =>
        {
            MyViewModel.SetSelectedArtist(firstItem, true);
        });
    }

    // ==========================================
    // SELECTION FIXES
    // ==========================================
    private void AllArtistsTableView_Tapped(object sender, TappedRoutedEventArgs e)
    {
        var OGFrameworkElt = e.OriginalSource as FrameworkElement;
        if (OGFrameworkElt is null) return;

        var artistModelView = OGFrameworkElt.DataContext as ArtistModelView;
        if (artistModelView == null) return;

        // FIXED: Defer execution to prevent the Reentrancy/Layout Crash
        DispatcherQueue.TryEnqueue(() =>
        {
            MyViewModel.SetSelectedArtist(artistModelView, true);
        });
    }

    private void AllArtistsTableView_CellDoubleTapped(object sender, TableViewCellDoubleTappedEventArgs e)
    {
        if (e.Item is ArtistModelView artist)
        {
            MyViewModel.NavigateToArtistPageWithArtistId(artist.Id);
        }
    }

    private void FilterArtistName_Loaded(object sender, RoutedEventArgs e) { }

    private void ContextMenuPlay_Click(object sender, RoutedEventArgs e)
    {

    }

    private void ContextMenuQueue_Click(object sender, RoutedEventArgs e)
    {

    }

    private void ContextMenuPlaylist_Click(object sender, RoutedEventArgs e)
    {

    }

    private void ViewArtistButton_Click(object sender, RoutedEventArgs e)
    {
        if (MyViewModel.SelectedArtist != null)
        {
            MyViewModel.NavigateToArtistPageWithArtistId(MyViewModel.SelectedArtist.Id);
        }
    }
}