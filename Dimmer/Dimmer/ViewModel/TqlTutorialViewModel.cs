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
        // --- GETTING STARTED ---
        Lessons.Add(new TqlLesson
        {
            Category = "Getting Started",
            Title = "Search Any Field",
            Explanation = "Just type text to search across all common fields like title, artist, and album. This is the simplest way to find music.",
            TqlQuery = "Dream Theater"
        });
        Lessons.Add(new TqlLesson
        {
            Category = "Getting Started",
            Title = "Search a Specific Field",
            Explanation = "Use a field name followed by a colon (:) to be more specific. Common fields are 'artist' (or 'ar'), 'album' (or 'al'), 'title' (or 't'), 'year', 'genre' (or 'g').",
            TqlQuery = "artist:Tool"
        });
        Lessons.Add(new TqlLesson
        {
            Category = "Getting Started",
            Title = "Phrases with Spaces",
            Explanation = "If your search term has spaces, wrap it in double quotes to search for the exact phrase.",
            TqlQuery = "album:\"A Dramatic Turn of Events\""
        });
        Lessons.Add(new TqlLesson
        {
            Category = "Getting Started",
            Title = "Field Aliases",
            Explanation = "Many fields have short aliases. For example: 't' for title, 'ar' for artist, 'al' for album, 'g' for genre.",
            TqlQuery = "t:\"Bohemian Rhapsody\" ar:Queen"
        });

        // --- OPERATORS ---
        Lessons.Add(new TqlLesson
        {
            Category = "Operators",
            Title = "Greater Than (>)",
            Explanation = "Use > for numeric fields like 'year', 'plays', 'rating', 'bit' (bitrate). Find songs released after 2010.",
            TqlQuery = "year:>2010"
        });
        Lessons.Add(new TqlLesson
        {
            Category = "Operators",
            Title = "Less Than (<)",
            Explanation = "Use < for numeric comparisons. Find songs with less than 5 plays.",
            TqlQuery = "plays:<5"
        });
        Lessons.Add(new TqlLesson
        {
            Category = "Operators",
            Title = "Greater or Equal (>=)",
            Explanation = "Use >= to include the boundary value. Find songs rated 4 stars or higher.",
            TqlQuery = "rating:>=4"
        });
        Lessons.Add(new TqlLesson
        {
            Category = "Operators",
            Title = "Numeric Ranges",
            Explanation = "Use a dash (-) to find songs within a numeric range. Find music from the 2000s.",
            TqlQuery = "year:2000-2009"
        });
        Lessons.Add(new TqlLesson
        {
            Category = "Operators",
            Title = "Starts With (^)",
            Explanation = "Use a caret (^) to find text that starts with your term. Find all titles starting with 'The'.",
            TqlQuery = "title:^The"
        });
        Lessons.Add(new TqlLesson
        {
            Category = "Operators",
            Title = "Ends With ($)",
            Explanation = "Use a dollar sign ($) to find text that ends with your term. Find titles ending with 'Blues'.",
            TqlQuery = "title:$Blues"
        });
        Lessons.Add(new TqlLesson
        {
            Category = "Operators",
            Title = "Fuzzy Search (~)",
            Explanation = "Use tilde (~) for approximate matching. Great for typos! Will find 'Beatles' even if you type 'Beatels'.",
            TqlQuery = "artist:~Beatels"
        });

        // --- LOGICAL COMBINERS ---
        Lessons.Add(new TqlLesson
        {
            Category = "Combining Filters",
            Title = "AND (Implicit)",
            Explanation = "Just list multiple filters separated by spaces to find songs that match ALL conditions. The 'and' keyword is optional.",
            TqlQuery = "artist:\"Porcupine Tree\" year:>2005"
        });
        Lessons.Add(new TqlLesson
        {
            Category = "Combining Filters",
            Title = "AND (Explicit)",
            Explanation = "You can use the 'and' keyword explicitly if you prefer. Both work the same way.",
            TqlQuery = "genre:Rock and year:>2000"
        });
        Lessons.Add(new TqlLesson
        {
            Category = "Combining Filters",
            Title = "OR (add)",
            Explanation = "Use 'add' or 'or' to find songs that match EITHER filter. Find songs by Tool OR A Perfect Circle.",
            TqlQuery = "artist:Tool add artist:\"A Perfect Circle\""
        });
        Lessons.Add(new TqlLesson
        {
            Category = "Combining Filters",
            Title = "OR Alternative",
            Explanation = "The 'or' keyword works the same as 'add'. Find Rock or Metal songs.",
            TqlQuery = "genre:Rock or genre:Metal"
        });
        Lessons.Add(new TqlLesson
        {
            Category = "Combining Filters",
            Title = "NOT (exclude)",
            Explanation = "Use 'exclude' or 'not' to remove songs that match a filter. Find Rock songs but exclude Nickelback.",
            TqlQuery = "genre:Rock exclude artist:Nickelback"
        });
        Lessons.Add(new TqlLesson
        {
            Category = "Combining Filters",
            Title = "NOT Alternative",
            Explanation = "The 'not' keyword works the same as 'exclude'. Find favorites that aren't by Drake.",
            TqlQuery = "fav:true not artist:Drake"
        });
        Lessons.Add(new TqlLesson
        {
            Category = "Combining Filters",
            Title = "Grouping with Parentheses",
            Explanation = "Use parentheses () to control the order of operations for complex queries. Find modern songs by Tool or Opeth.",
            TqlQuery = "year:>2000 and (artist:Tool or artist:Opeth)"
        });
        Lessons.Add(new TqlLesson
        {
            Category = "Combining Filters",
            Title = "Complex Grouping",
            Explanation = "Combine multiple conditions with parentheses. Find Rock or Metal from the 90s, excluding live albums.",
            TqlQuery = "(genre:Rock or genre:Metal) and year:1990-1999 exclude album:*Live*"
        });

        // --- SORTING & LIMITING ---
        Lessons.Add(new TqlLesson
        {
            Category = "Sorting & Limiting",
            Title = "Sort Ascending",
            Explanation = "Use 'asc' followed by a field name to sort results in ascending order. Sort by year, oldest first.",
            TqlQuery = "genre:Jazz asc year"
        });
        Lessons.Add(new TqlLesson
        {
            Category = "Sorting & Limiting",
            Title = "Sort Descending",
            Explanation = "Use 'desc' followed by a field name to sort results in descending order. Sort by rating, highest first.",
            TqlQuery = "fav:true desc rating"
        });
        Lessons.Add(new TqlLesson
        {
            Category = "Sorting & Limiting",
            Title = "Get First N Results",
            Explanation = "Use 'first' followed by a number to limit results to the first N songs. Great when combined with sorting!",
            TqlQuery = "artist:\"Led Zeppelin\" asc year first 10"
        });
        Lessons.Add(new TqlLesson
        {
            Category = "Sorting & Limiting",
            Title = "Get Last N Results",
            Explanation = "Use 'last' followed by a number to get the last N songs from results. Find the 5 most recent songs added.",
            TqlQuery = "any:* desc added last 5"
        });
        Lessons.Add(new TqlLesson
        {
            Category = "Sorting & Limiting",
            Title = "Random Shuffle",
            Explanation = "Use 'shuffle' or 'random' followed by a number to get a random selection from your results. Perfect for discovery!",
            TqlQuery = "fav:true shuffle 25"
        });
        Lessons.Add(new TqlLesson
        {
            Category = "Sorting & Limiting",
            Title = "Sort by Play Count",
            Explanation = "Sort by how many times you've played songs. Find your most played tracks.",
            TqlQuery = "any:* desc plays first 20"
        });

        // --- NATURAL LANGUAGE ---
        Lessons.Add(new TqlLesson
        {
            Category = "Natural Language",
            Title = "My Favorites",
            Explanation = "Natural language: 'my fav' or 'my favorites' automatically translates to fav:true. Find all your favorite songs!",
            TqlQuery = "my fav"
        });
        Lessons.Add(new TqlLesson
        {
            Category = "Natural Language",
            Title = "Songs by Artist",
            Explanation = "Natural language: 'songs by [artist]' translates to artist:[artist]. More intuitive than field syntax!",
            TqlQuery = "songs by Queen"
        });
        Lessons.Add(new TqlLesson
        {
            Category = "Natural Language",
            Title = "Music from Artist",
            Explanation = "Natural language: 'music from [artist]' also works for finding artist songs.",
            TqlQuery = "music from Pink Floyd"
        });
        Lessons.Add(new TqlLesson
        {
            Category = "Natural Language",
            Title = "Album Queries",
            Explanation = "Natural language: 'album is [name]' or 'in album [name]' for album searches.",
            TqlQuery = "album is \"Dark Side of the Moon\""
        });
        Lessons.Add(new TqlLesson
        {
            Category = "Natural Language",
            Title = "From the Decade",
            Explanation = "Natural language: 'from the 90s' or 'in the 80s' automatically converts to year ranges.",
            TqlQuery = "songs by Metallica from the 80s"
        });
        Lessons.Add(new TqlLesson
        {
            Category = "Natural Language",
            Title = "Has Lyrics",
            Explanation = "Natural language: 'has lyrics' or 'with lyrics' finds songs that have lyrics available.",
            TqlQuery = "has lyrics"
        });
        Lessons.Add(new TqlLesson
        {
            Category = "Natural Language",
            Title = "Recently Added",
            Explanation = "Natural language: 'added today', 'added yesterday', 'added this week' for time-based queries.",
            TqlQuery = "added this week"
        });
        Lessons.Add(new TqlLesson
        {
            Category = "Natural Language",
            Title = "Longer Than",
            Explanation = "Natural language: 'longer than 5 minutes' finds songs by duration.",
            TqlQuery = "longer than 5 minutes"
        });

        // --- BOOLEAN FIELDS ---
        Lessons.Add(new TqlLesson
        {
            Category = "Boolean Fields",
            Title = "Favorites",
            Explanation = "Use fav:true to find your favorite songs. You can also use the alias 'love:true'.",
            TqlQuery = "fav:true"
        });
        Lessons.Add(new TqlLesson
        {
            Category = "Boolean Fields",
            Title = "Not Favorites",
            Explanation = "Use fav:false to find songs that aren't marked as favorites.",
            TqlQuery = "fav:false rating:>=4"
        });
        Lessons.Add(new TqlLesson
        {
            Category = "Boolean Fields",
            Title = "Has Synced Lyrics",
            Explanation = "Use synced:true or haslyrics:true to find songs with synchronized lyrics.",
            TqlQuery = "synced:true"
        });
        Lessons.Add(new TqlLesson
        {
            Category = "Boolean Fields",
            Title = "Has Any Lyrics",
            Explanation = "Use singable:true to find songs that have any kind of lyrics (synced or plain).",
            TqlQuery = "singable:true genre:Pop"
        });

        // --- NUMERIC FIELDS ---
        Lessons.Add(new TqlLesson
        {
            Category = "Numeric Fields",
            Title = "Rating Filter",
            Explanation = "Filter by rating (0-5 stars). Find your 5-star songs!",
            TqlQuery = "rating:5"
        });
        Lessons.Add(new TqlLesson
        {
            Category = "Numeric Fields",
            Title = "Play Count",
            Explanation = "Use 'plays' to filter by how many times you've played a song. Find hidden gems with few plays.",
            TqlQuery = "plays:<3 rating:>=4"
        });
        Lessons.Add(new TqlLesson
        {
            Category = "Numeric Fields",
            Title = "Skip Count",
            Explanation = "Use 'skips' to find songs you frequently skip. Maybe it's time to remove them?",
            TqlQuery = "skips:>10"
        });
        Lessons.Add(new TqlLesson
        {
            Category = "Numeric Fields",
            Title = "Track Number",
            Explanation = "Use 'track' to find specific track positions. Find all album openers!",
            TqlQuery = "track:1"
        });
        Lessons.Add(new TqlLesson
        {
            Category = "Numeric Fields",
            Title = "BPM (Beats Per Minute)",
            Explanation = "Use 'bpm' or 'beats' to filter by tempo. Find high-energy workout music!",
            TqlQuery = "bpm:>140"
        });
        Lessons.Add(new TqlLesson
        {
            Category = "Numeric Fields",
            Title = "Bitrate Quality",
            Explanation = "Use 'bit' to filter by audio quality. Find your high-quality 320kbps tracks.",
            TqlQuery = "bit:>=320"
        });
        Lessons.Add(new TqlLesson
        {
            Category = "Numeric Fields",
            Title = "File Size",
            Explanation = "Use 'size' to filter by file size in bytes. Find large files that might be lossless.",
            TqlQuery = "size:>10000000"
        });
        Lessons.Add(new TqlLesson
        {
            Category = "Numeric Fields",
            Title = "Global Rank",
            Explanation = "Use 'rglo' to filter by your global song ranking. Find your top 100 songs!",
            TqlQuery = "rglo:<=100"
        });

        // --- DURATION QUERIES ---
        Lessons.Add(new TqlLesson
        {
            Category = "Duration",
            Title = "Short Songs",
            Explanation = "Use 'len', 'length', 'duration', or 'dur' for song duration in seconds. Find songs under 3 minutes (180 seconds).",
            TqlQuery = "len:<180"
        });
        Lessons.Add(new TqlLesson
        {
            Category = "Duration",
            Title = "Long Songs",
            Explanation = "Find epic songs over 6 minutes long (360 seconds). Perfect for prog rock!",
            TqlQuery = "len:>360 genre:Progressive"
        });
        Lessons.Add(new TqlLesson
        {
            Category = "Duration",
            Title = "Duration Range",
            Explanation = "Find songs within a specific duration range. Songs between 3-5 minutes.",
            TqlQuery = "dur:180-300"
        });

        // --- DATE & TIME ---
        Lessons.Add(new TqlLesson
        {
            Category = "Date & Time",
            Title = "Recently Added",
            Explanation = "Use 'added' to filter by when songs were added to your library. This works with natural language too!",
            TqlQuery = "added:today"
        });
        Lessons.Add(new TqlLesson
        {
            Category = "Date & Time",
            Title = "Recently Played",
            Explanation = "Use 'played' to filter by when you last played songs. Find songs played yesterday.",
            TqlQuery = "played:yesterday"
        });
        Lessons.Add(new TqlLesson
        {
            Category = "Date & Time",
            Title = "This Week",
            Explanation = "Find songs added or played this week. Great for discovering recent additions!",
            TqlQuery = "added:\"this week\""
        });

        // --- ADVANCED FIELDS ---
        Lessons.Add(new TqlLesson
        {
            Category = "Advanced",
            Title = "Search in Lyrics",
            Explanation = "Use 'lyrics' to search within the actual lyrics text of songs. Find songs mentioning 'love'.",
            TqlQuery = "lyrics:love"
        });
        Lessons.Add(new TqlLesson
        {
            Category = "Advanced",
            Title = "Search in Notes",
            Explanation = "Use 'note', 'notes', or 'comment' to search your personal notes or playlist comments.",
            TqlQuery = "note:workout"
        });
        Lessons.Add(new TqlLesson
        {
            Category = "Advanced",
            Title = "File Format",
            Explanation = "Use 'type' or 'format' to filter by audio format. Find all your FLAC files!",
            TqlQuery = "type:flac"
        });
        Lessons.Add(new TqlLesson
        {
            Category = "Advanced",
            Title = "Composer Search",
            Explanation = "Use 'comp' or 'composer' to find songs by a specific composer. Great for classical music!",
            TqlQuery = "comp:Mozart"
        });
        Lessons.Add(new TqlLesson
        {
            Category = "Advanced",
            Title = "Rank in Artist",
            Explanation = "Use 'rar' to filter by ranking within an artist's songs. Find top 5 songs from each artist.",
            TqlQuery = "rar:<=5"
        });

        // --- COMMANDS ---
        Lessons.Add(new TqlLesson
        {
            Category = "Commands",
            Title = "Play Results",
            Explanation = "Use '>> play!' to immediately replace your queue with the search results and start playing.",
            TqlQuery = "genre:Ambient shuffle 20 >> play!"
        });
        Lessons.Add(new TqlLesson
        {
            Category = "Commands",
            Title = "Save as Playlist",
            Explanation = "Use '>> save [name]!' to save the search results as a new playlist. Great for preserving complex queries!",
            TqlQuery = "year:1991 desc rating >> save Best of 1991!"
        });
        Lessons.Add(new TqlLesson
        {
            Category = "Commands",
            Title = "Add to Queue",
            Explanation = "Use '>> addnext' to add results to the top of your current queue without clearing it.",
            TqlQuery = "fav:true shuffle 5 >> addnext"
        });

        // --- REAL-WORLD EXAMPLES ---
        Lessons.Add(new TqlLesson
        {
            Category = "Examples",
            Title = "Workout Playlist",
            Explanation = "High-energy songs with fast tempo, highly rated. Perfect for the gym!",
            TqlQuery = "bpm:>130 rating:>=4 shuffle 50"
        });
        Lessons.Add(new TqlLesson
        {
            Category = "Examples",
            Title = "Focus Music",
            Explanation = "Long, instrumental tracks without lyrics. Great for studying or working.",
            TqlQuery = "len:>300 singable:false shuffle 30"
        });
        Lessons.Add(new TqlLesson
        {
            Category = "Examples",
            Title = "Nostalgic 90s",
            Explanation = "Your favorite songs from the 90s, sorted by play count. Trip down memory lane!",
            TqlQuery = "year:1990-1999 fav:true desc plays"
        });
        Lessons.Add(new TqlLesson
        {
            Category = "Examples",
            Title = "Hidden Gems",
            Explanation = "Highly rated songs you rarely play. Rediscover forgotten favorites!",
            TqlQuery = "rating:>=4 plays:<5 shuffle 20"
        });
        Lessons.Add(new TqlLesson
        {
            Category = "Examples",
            Title = "Album Deep Dive",
            Explanation = "All songs from a specific album, in track order. Listen to albums as intended!",
            TqlQuery = "album:\"Dark Side of the Moon\" asc track"
        });
        Lessons.Add(new TqlLesson
        {
            Category = "Examples",
            Title = "Road Trip Mix",
            Explanation = "Upbeat favorites from multiple genres, shuffled. Perfect for long drives!",
            TqlQuery = "(genre:Rock or genre:Pop or genre:Electronic) rating:>=4 shuffle 100"
        });
    }
}