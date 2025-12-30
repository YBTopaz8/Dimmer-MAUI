using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.DimmerSearch.TQLDoc;

public enum TqlCategory { Basics, Operators, Logic, Sorting, Commands, Natural, Advanced, Examples }

public record TqlHelpItem(
    string Title,
    string Description,
    string ExampleQuery,
    TqlCategory Category
);

// A static repository of your documentation
public static class TqlDocumentation
{
    public static List<TqlHelpItem> AllItems = new()
    {
        // --- BASICS ---
        new("General Search", "Search all text fields at once. Just type text without field prefix.", "Dream Theater", TqlCategory.Basics),
        new("Artist Search", "Find songs by a specific artist using 'artist:' or 'ar:'", "artist:\"Pink Floyd\"", TqlCategory.Basics),
        new("Album Search", "Find songs within a specific album using 'album:' or 'al:'", "album:\"Dark Side\"", TqlCategory.Basics),
        new("Title Search", "Find songs with a specific word in the title using 'title:' or 't:'", "title:Time", TqlCategory.Basics),
        new("Genre Search", "Find songs by genre using 'genre:' or 'g:'", "genre:Rock", TqlCategory.Basics),
        new("Field Aliases", "Many fields have short aliases. t=title, ar=artist, al=album, g=genre", "t:\"Bohemian Rhapsody\" ar:Queen", TqlCategory.Basics),
        new("Quoted Phrases", "Use quotes for phrases with spaces.", "album:\"The Dark Side of the Moon\"", TqlCategory.Basics),

        // --- OPERATORS ---
        new("Greater Than", "Use > for numeric comparisons.", "year:>2010", TqlCategory.Operators),
        new("Less Than", "Use < for numeric comparisons.", "plays:<5", TqlCategory.Operators),
        new("Greater or Equal", "Use >= to include the boundary value.", "rating:>=4", TqlCategory.Operators),
        new("Less or Equal", "Use <= to include the boundary value.", "skips:<=2", TqlCategory.Operators),
        new("Numeric Range", "Use dash (-) for ranges.", "year:2000-2009", TqlCategory.Operators),
        new("Starts With", "Use caret (^) to match text that starts with a term.", "title:^The", TqlCategory.Operators),
        new("Ends With", "Use dollar sign ($) to match text that ends with a term.", "title:$Blues", TqlCategory.Operators),
        new("Fuzzy Search", "Use tilde (~) for approximate matching. Good for typos!", "artist:~Beatels", TqlCategory.Operators),

        // --- LOGIC ---
        new("AND Implicit", "Space between filters means AND (all must match).", "artist:Tool year:>2000", TqlCategory.Logic),
        new("AND Explicit", "Use 'and' keyword explicitly if preferred.", "genre:Rock and year:>2000", TqlCategory.Logic),
        new("OR Operator", "Use 'or' or 'add' to match either filter.", "artist:Tool or artist:\"A Perfect Circle\"", TqlCategory.Logic),
        new("NOT Operator", "Use 'not' or 'exclude' to remove matching songs.", "genre:Rock not artist:Nickelback", TqlCategory.Logic),
        new("Grouping", "Use parentheses () to control order of operations.", "year:>2000 and (artist:Tool or artist:Opeth)", TqlCategory.Logic),
        new("Complex Logic", "Combine multiple operators with grouping.", "(genre:Rock or genre:Metal) and year:1990-1999", TqlCategory.Logic),

        // --- SORTING & LIMITING ---
        new("Sort Ascending", "Use 'asc' followed by field name to sort low to high.", "genre:Jazz asc year", TqlCategory.Sorting),
        new("Sort Descending", "Use 'desc' followed by field name to sort high to low.", "fav:true desc rating", TqlCategory.Sorting),
        new("First N", "Use 'first' to limit to the first N results.", "artist:Queen asc year first 10", TqlCategory.Sorting),
        new("Last N", "Use 'last' to get the last N results.", "any:* desc added last 5", TqlCategory.Sorting),
        new("Random Selection", "Use 'shuffle' or 'random' to get random results.", "fav:true shuffle 25", TqlCategory.Sorting),
        new("Sort by Plays", "Sort by play count to find most/least played.", "any:* desc plays first 20", TqlCategory.Sorting),

        // --- NATURAL LANGUAGE ---
        new("My Favorites", "Natural: 'my fav' or 'my favorites' → fav:true", "my fav", TqlCategory.Natural),
        new("Songs By", "Natural: 'songs by [artist]' → artist:[artist]", "songs by Queen", TqlCategory.Natural),
        new("Music From", "Natural: 'music from [artist]' → artist:[artist]", "music from Pink Floyd", TqlCategory.Natural),
        new("Album Is", "Natural: 'album is [name]' → album:[name]", "album is \"Dark Side of the Moon\"", TqlCategory.Natural),
        new("From Decade", "Natural: 'from the 90s' → year:1990-1999", "songs by Metallica from the 80s", TqlCategory.Natural),
        new("Has Lyrics", "Natural: 'has lyrics' or 'with lyrics' → haslyrics:true", "has lyrics", TqlCategory.Natural),
        new("Time Phrases", "Natural: 'added today', 'played yesterday', etc.", "added this week", TqlCategory.Natural),
        new("Duration Natural", "Natural: 'longer than 5 minutes' → len:>300", "longer than 5 minutes", TqlCategory.Natural),

        // --- ADVANCED FIELDS ---
        new("Favorites", "Find your favorite songs.", "fav:true", TqlCategory.Advanced),
        new("Rating", "Filter by star rating (0-5).", "rating:5", TqlCategory.Advanced),
        new("Play Count", "Filter by number of plays.", "plays:>50", TqlCategory.Advanced),
        new("Skip Count", "Find frequently skipped songs.", "skips:>10", TqlCategory.Advanced),
        new("Synced Lyrics", "Songs with synchronized lyrics.", "synced:true", TqlCategory.Advanced),
        new("Has Any Lyrics", "Songs with any kind of lyrics.", "singable:true", TqlCategory.Advanced),
        new("Duration", "Filter by song length in seconds.", "len:>360", TqlCategory.Advanced),
        new("BPM/Tempo", "Filter by beats per minute.", "bpm:>140", TqlCategory.Advanced),
        new("Bitrate", "Filter by audio quality (kbps).", "bit:>=320", TqlCategory.Advanced),
        new("Track Number", "Find specific track positions.", "track:1", TqlCategory.Advanced),
        new("File Format", "Filter by audio format.", "type:flac", TqlCategory.Advanced),
        new("Composer", "Search by composer.", "comp:Mozart", TqlCategory.Advanced),
        new("Lyrics Text", "Search within lyrics content.", "lyrics:love", TqlCategory.Advanced),
        new("Notes Search", "Search your personal notes.", "note:workout", TqlCategory.Advanced),
        new("Date Added", "Filter by when added to library.", "added:today", TqlCategory.Advanced),
        new("Last Played", "Filter by last play time.", "played:yesterday", TqlCategory.Advanced),
        new("Global Rank", "Filter by overall song ranking.", "rglo:<=100", TqlCategory.Advanced),

        // --- COMMANDS ---
        new("Play Now", "Use '>> play!' to start playing results immediately.", "genre:Ambient shuffle 20 >> play!", TqlCategory.Commands),
        new("Save Playlist", "Use '>> save [name]!' to create a playlist.", "year:1991 >> save Best of 1991!", TqlCategory.Commands),
        new("Add to Queue", "Use '>> addnext' to add to queue without clearing.", "fav:true shuffle 5 >> addnext", TqlCategory.Commands),

        // --- REAL-WORLD EXAMPLES ---
        new("Workout Mix", "High-energy, highly-rated songs for exercise.", "bpm:>130 rating:>=4 shuffle 50", TqlCategory.Examples),
        new("Focus Music", "Long instrumental tracks without lyrics.", "len:>300 singable:false shuffle 30", TqlCategory.Examples),
        new("90s Nostalgia", "Your favorite 90s songs by play count.", "year:1990-1999 fav:true desc plays", TqlCategory.Examples),
        new("Hidden Gems", "Great songs you rarely play.", "rating:>=4 plays:<5 shuffle 20", TqlCategory.Examples),
        new("Album Order", "Full album in track order.", "album:\"Dark Side of the Moon\" asc track", TqlCategory.Examples),
        new("Road Trip", "Upbeat multi-genre favorites.", "(genre:Rock or genre:Pop) rating:>=4 shuffle 100", TqlCategory.Examples),
    };
}