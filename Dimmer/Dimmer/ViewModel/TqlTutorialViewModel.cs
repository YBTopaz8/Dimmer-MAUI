namespace Dimmer.ViewModel;

public partial class TqlTutorialViewModel : ObservableObject
{
    // A reference to the main ViewModel to run the search
    private readonly BaseViewModel _mainViewModel;
    public BaseViewModel MainViewModel => _mainViewModel;
    public ObservableCollection<TqlLesson> Lessons { get; }

    public TqlTutorialViewModel(BaseViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;
       

        Lessons = new ObservableCollection<TqlLesson>();
        LoadLessons();
    }

    [RelayCommand]
    public void TryItNow(string query)
    {
        if (string.IsNullOrEmpty(query))
            return;

        // The magic happens here: we tell the main ViewModel to run the search.
        _mainViewModel.SearchSongForSearchResultHolder(query);
        // The main ViewModel's Rx pipeline will take over from here.
    }

    private void LoadLessons()
    {
        // Populate the list of all TQL features. This is your curriculum.
        Lessons.Add(new TqlLesson
        {
            Category = "Basic Filtering",
            Title = "Search Any Field",
            Explanation = "Just type text to search across all common fields like title, artist, and album.",
            TqlQuery = "Dream Theater"
        });
        Lessons.Add(new TqlLesson
        {
            Category = "Basic Filtering",
            Title = "Search a Specific Field",
            Explanation = "Use a field name followed by a colon (:) to be more specific. Common fields are 'artist', 'album', 'title', 'year', 'genre'.",
            TqlQuery = "artist:Tool"
        });
        Lessons.Add(new TqlLesson
        {
            Category = "Basic Filtering",
            Title = "Phrases with Spaces",
            Explanation = "If your search term has spaces, wrap it in double quotes.",
            TqlQuery = "album:\"A Dramatic Turn of Events\""
        });

        // --- Operators ---
        Lessons.Add(new TqlLesson
        {
            Category = "Operators",
            Title = "Greater/Less Than",
            Explanation = "Use > or < for numeric fields like 'year' or 'plays'.",
            TqlQuery = "year:>2010"
        });
        Lessons.Add(new TqlLesson
        {
            Category = "Operators",
            Title = "Numeric Ranges",
            Explanation = "Use a dash (-) to find songs within a numeric range.",
            TqlQuery = "year:2000-2005"
        });
        Lessons.Add(new TqlLesson
        {
            Category = "Operators",
            Title = "Starts With (^)",
            Explanation = "Use a caret (^) to find text that starts with your term.",
            TqlQuery = "title:^The"
        });
        Lessons.Add(new TqlLesson
        {
            Category = "Operators",
            Title = "Ends With ($)",
            Explanation = "Use a dollar sign ($) to find text that ends with your term.",
            TqlQuery = "title:$s"
        });

        // --- Logical Combiners ---
        Lessons.Add(new TqlLesson
        {
            Category = "Combining Filters",
            Title = "AND (Implicit)",
            Explanation = "Just list two filters to find songs that match both (AND). 'and' keyword is optional.",
            TqlQuery = "artist:Porcupine Tree year:>2005"
        });
        Lessons.Add(new TqlLesson
        {
            Category = "Combining Filters",
            Title = "OR (add)",
            Explanation = "Use 'add' or 'or' to find songs that match either filter.",
            TqlQuery = "artist:Tool add artist:\"A Perfect Circle\""
        });
        Lessons.Add(new TqlLesson
        {
            Category = "Combining Filters",
            Title = "NOT (exclude)",
            Explanation = "Use 'exclude' or 'not' to remove songs that match a filter.",
            TqlQuery = "genre:Rock exclude artist:Nickelback"
        });
        Lessons.Add(new TqlLesson
        {
            Category = "Combining Filters",
            Title = "Grouping with Parentheses",
            Explanation = "Use parentheses () to control the order of operations for complex queries.",
            TqlQuery = "year:>2000 and (artist:Tool or artist:Opeth)"
        });

        // --- Directives ---
        Lessons.Add(new TqlLesson
        {
            Category = "Sorting & Limiting",
            Title = "Sorting",
            Explanation = "Use 'asc' or 'desc' at the end of your query, followed by a field name.",
            TqlQuery = "genre:Progressive Metal desc year"
        });
        Lessons.Add(new TqlLesson
        {
            Category = "Sorting & Limiting",
            Title = "Get First/Last",
            Explanation = "Use 'first' or 'last' to limit the results. Great when combined with sorting.",
            TqlQuery = "artist:Led Zeppelin asc year first 10"
        });
        Lessons.Add(new TqlLesson
        {
            Category = "Sorting & Limiting",
            Title = "Random Shuffle",
            Explanation = "Use 'shuffle' or 'random' to get a random selection from your results.",
            TqlQuery = "fav:true shuffle 25"
        });

        // --- Commands ---
        Lessons.Add(new TqlLesson
        {
            Category = "Commands",
            Title = "Play Results",
            Explanation = "Use '>> play!' to immediately replace your queue with the search results.",
            TqlQuery = "genre:Ambient >> play!"
        });
        Lessons.Add(new TqlLesson
        {
            Category = "Commands",
            Title = "Save as Playlist",
            Explanation = "Use '>> save <name>!' to save the results as a new playlist.",
            TqlQuery = "year:1991 >> save My 1991 Playlist!"
        });
    }
}