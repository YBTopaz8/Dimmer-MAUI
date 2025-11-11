using Dimmer.Interfaces;

namespace Dimmer.DimmerSearch.TQL;

public class AutocompleteEngine
{
    // These collections are populated by your ViewModel using DynamicData
    private readonly ObservableCollection<string> _distinctArtists;
    private readonly ObservableCollection<string> _distinctAlbums;
    private readonly ObservableCollection<string> _distinctGenres;

    // The "Free Search" sources (all data)
    private readonly ObservableCollection<string> _masterArtists;
    private readonly ObservableCollection<string> _masterAlbums;
    private readonly ObservableCollection<string> _masterGenres;

    public AutocompleteEngine(
        IRepository<ArtistModel> artistRepo,
        IRepository<AlbumModel> albumRepo,
        IRepository<GenreModel> genreRepo)
    {

        var art= artistRepo.GetAll().Select(x => x.Name).ToObservableCollection();
       
        var albums = albumRepo.GetAll().Select(x => x.Name).ToObservableCollection();
        
        
        var genres = genreRepo.GetAll().Select(x => x.Name).ToObservableCollection();
        
    }

    public AutocompleteEngine(
        ObservableCollection<string> masterArtists,
        ObservableCollection<string> masterAlbums,
        ObservableCollection<string> masterGenres,
        ObservableCollection<string> liveArtists,
        ObservableCollection<string> liveAlbums,
        ObservableCollection<string> liveGenres)
    {
        _masterArtists = masterArtists;
        _masterAlbums = masterAlbums;
        _masterGenres = masterGenres;
       
    }
    public static ObservableCollection<string> GetSuggestions(

    // The "Firm Search" sources (live, filtered data)
    ObservableCollection<string> _liveArtists,
    ObservableCollection<string> _liveAlbums,
    ObservableCollection<string> _liveGenres, string queryText, int cursorPosition, bool isFirmSearch=false)
    {
        try
        {
            if (cursorPosition == 0 || string.IsNullOrWhiteSpace(queryText))
            {
                // Suggest common fields to start with
                return FieldRegistry.AllFields.Select(f => f.PrimaryName + ":").Take(5).ToObservableCollection();
            }

            // Find the start of the word the cursor is in
            int wordStart = queryText.LastIndexOf(' ', cursorPosition - 1) + 1;
            string currentWord = queryText[wordStart..cursorPosition];

            // Case 1: The user is typing a field name (e.g., "art|")
            if (!currentWord.Contains(':'))
            {
                return FieldRegistry.FieldsByAlias.Keys
                    .Where(alias => alias.StartsWith(currentWord, StringComparison.OrdinalIgnoreCase))
                    .Select(alias => alias + ":")
                    .Distinct()
                    .OrderBy(s => s.Length)
                    .ToObservableCollection();
            }

            // Case 2: The user has typed a field and is typing a value (e.g., "artist:que|")
            var parts = currentWord.Split(new[] { ':' }, 2);
            string fieldAlias = parts[0];
            string valuePrefix = parts.Length > 1 ? parts[1] : "";

            if (!FieldRegistry.FieldsByAlias.TryGetValue(fieldAlias, out var fieldDef))
            {
                return new ObservableCollection<string>(); // Invalid field, no suggestions
            }


            if (fieldDef.Type == FieldType.Date)
            {
                var dateSuggestions = new List<string>
    {
        "today", "yesterday", "this week", "last week",
        "morning", "afternoon", "evening", "night",
        "never", "ago(\"\")"
    };
                return dateSuggestions
                    .Where(s => s.StartsWith(valuePrefix, StringComparison.OrdinalIgnoreCase))
                    .ToObservableCollection();
            }

            // Determine which data source to use for suggestions
            var artistSource = _liveArtists;
            var albumSource = _liveAlbums;
            var genreSource = _liveGenres;

            // Provide suggestions based on the field's type
            switch (fieldDef.PrimaryName)
            {
                case "OtherArtistsName":
                    return artistSource
                        .Where(a => a != null && a.StartsWith(valuePrefix, StringComparison.OrdinalIgnoreCase))
                        .Select(a => a.Contains(' ') ? $"\"{a}\"" : a) // Quote if it has spaces
                        .Take(10).ToObservableCollection();

                case "AlbumName":

                    return albumSource
                        .Where(a => a != null && a.StartsWith(valuePrefix, StringComparison.OrdinalIgnoreCase))
                        .Select(a => a.Contains(' ') ? $"\"{a}\"" : a)
                        .Take(10).ToObservableCollection();

                case "GenreName":
                    return genreSource
                       .Where(g => g != null && g.StartsWith(valuePrefix, StringComparison.OrdinalIgnoreCase))
                       .Take(10).ToObservableCollection();

                case "ReleaseYear":
                    return new ObservableCollection<string> { ">", "<", ">=", "<=", "-" };

                case "IsFavorite":
                case "HasLyrics":
                case "HasSyncedLyrics":
                    return new List<string> { "true", "false" }
                        .Where(s => s.StartsWith(valuePrefix, StringComparison.OrdinalIgnoreCase))
                        .ToObservableCollection();

                default:
                    return new ObservableCollection<string>(); // No suggestions for generic text fields
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            return new ObservableCollection<string>(); // Return empty collection on error
        }
    }
}