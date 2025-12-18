using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.DimmerSearch.TQLDoc;

public enum TqlCategory { Basics, Dates, numeric, Logic, Commands, Fun }

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
        new("Artist Search", "Find songs by a specific artist.", "artist:\"Pink Floyd\"", TqlCategory.Basics),
        new("Album Search", "Find songs within a specific album.", "album:\"Dark Side\"", TqlCategory.Basics),
        new("Title Search", "Find songs with a specific word in the title.", "title:Time", TqlCategory.Basics),
        new("General Search", "Search all text fields at once.", "any:\"Wish you were here\"", TqlCategory.Basics),
        new("Fuzzy Search", "Finds approximate matches (good for typos).", "artist~Led Zepplin", TqlCategory.Basics),

        // --- NUMERIC ---
        new("High Rating", "Songs rated 4 stars or higher.", "rating:>=4", TqlCategory.numeric),
        new("Duration", "Songs longer than 5 minutes.", "len:>5m", TqlCategory.numeric),
        new("Bitrate", "High quality audio files (320kbps).", "bit:320", TqlCategory.numeric),
        new("Release Year", "Songs from the 90s.", "year:1990-1999", TqlCategory.numeric),

        // --- DATES ---
        new("Recently Added", "Songs added in the last week.", "added:ago(\"1w\")", TqlCategory.Dates),
        new("Recently Played", "Songs played today.", "played:today", TqlCategory.Dates),
        new("Time of Day", "Songs usually played in the evening.", "played:evening", TqlCategory.Dates),
        new("Date Range", "Added between two specific dates.", "added:between(\"2023-01-01\", \"2023-12-31\")", TqlCategory.Dates),

        // --- LOGIC ---
        new("Favorites", "Your favorite songs.", "fav:true", TqlCategory.Logic),
        new("Lyrics", "Songs that have synced lyrics.", "synced:true", TqlCategory.Logic),
        new("Combination", "Rock songs from the 80s.", "genre:Rock AND year:1980-1989", TqlCategory.Logic),
        new("Exclusion", "Metallica songs, but not from the Black Album.", "artist:Metallica NOT album:\"Black Album\"", TqlCategory.Logic),

        // --- COMMANDS ---
        new("Shuffle", "Pick 50 random songs.", "matchall shuffle 50", TqlCategory.Commands),
        new("Save Playlist", "Save current results as a playlist.", "artist:Tool >> save MyToolPlaylist", TqlCategory.Commands),
        new("Queue Next", "Add results to the top of the queue.", "title:Hello >> addnext", TqlCategory.Commands),
        
        // --- FUN ---
        new("Random Chance", "A 50% chance to include any song.", "chance(50)", TqlCategory.Fun),
    };
}