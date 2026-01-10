using Dimmer.DimmerSearch.TQL.RealmSection;
using Realms;

namespace Dimmer.ViewsAndPages.NativeViews.SectionHeader;

/// <summary>
/// Computes section headers based on the current sort mode/query plan.
/// </summary>
internal static class SectionGroupingHelper
{
    /// <summary>
    /// Computes sections from songs based on the query plan's sort descriptions or shuffle state.
    /// </summary>
    public static List<SectionHeaderModel> ComputeSections(
        IList<SongModelView> songs, 
        RealmQueryPlan? queryPlan)
    {
        var sections = new List<SectionHeaderModel>();

        if (songs == null || songs.Count == 0)
        {
            return sections;
        }

        // Handle shuffle mode - single static header
        if (queryPlan?.Shuffle != null)
        {
            sections.Add(new SectionHeaderModel
            {
                Title = "Shuffle",
                AdapterPosition = 0,
                SongStartIndex = 0,
                SongCount = songs.Count,
                Type = SectionType.Shuffle
            });
            return sections;
        }

        // Get the primary sort field
        var primarySort = queryPlan?.SortDescriptions?.FirstOrDefault();
        if (primarySort == null)
        {
            // No sort specified - create a single "All Songs" section
            sections.Add(new SectionHeaderModel
            {
                Title = "All Songs",
                AdapterPosition = 0,
                SongStartIndex = 0,
                SongCount = songs.Count,
                Type = SectionType.None
            });
            return sections;
        }

        // Determine section type based on field name
        var sectionType = DetermineSectionType(primarySort.PropertyName);

        if (sectionType == SectionType.DateBased)
        {
            sections = ComputeDateSections(songs, primarySort.PropertyName, primarySort.Direction);
        }
        else if (sectionType == SectionType.Alphabetical)
        {
            sections = ComputeAlphabeticalSections(songs, primarySort.PropertyName);
        }
        else
        {
            // Fallback for other fields
            sections.Add(new SectionHeaderModel
            {
                Title = $"Sorted by {primarySort.PropertyName}",
                AdapterPosition = 0,
                SongStartIndex = 0,
                SongCount = songs.Count,
                Type = SectionType.None
            });
        }

        return sections;
    }

    private static SectionType DetermineSectionType(string fieldName)
    {
        // Date-based fields
        if (fieldName.Contains("Date", StringComparison.OrdinalIgnoreCase) ||
            fieldName.Contains("played", StringComparison.OrdinalIgnoreCase) ||
            fieldName.Contains("added", StringComparison.OrdinalIgnoreCase) ||
            fieldName.Contains("modified", StringComparison.OrdinalIgnoreCase))
        {
            return SectionType.DateBased;
        }

        // Text fields that should be alphabetically grouped
        if (fieldName.Equals("Title", StringComparison.OrdinalIgnoreCase) ||
            fieldName.Equals("ArtistName", StringComparison.OrdinalIgnoreCase) ||
            fieldName.Equals("AlbumName", StringComparison.OrdinalIgnoreCase))
        {
            return SectionType.Alphabetical;
        }

        return SectionType.None;
    }

    private static List<SectionHeaderModel> ComputeDateSections(
        IList<SongModelView> songs, 
        string fieldName, 
        SortDirection direction)
    {
        var sections = new List<SectionHeaderModel>();
        string? currentSection = null;
        int sectionStartIndex = 0;
        int songsInSection = 0;

        for (int i = 0; i < songs.Count; i++)
        {
            var song = songs[i];
            var dateValue = GetDateValueFromSong(song, fieldName);
            var sectionKey = FormatDateSection(dateValue, direction);

            if (currentSection != sectionKey)
            {
                // Save previous section if any
                if (currentSection != null)
                {
                    sections.Add(new SectionHeaderModel
                    {
                        Title = currentSection,
                        AdapterPosition = sectionStartIndex,
                        SongStartIndex = sectionStartIndex,
                        SongCount = songsInSection,
                        Type = SectionType.DateBased
                    });
                }

                // Start new section
                currentSection = sectionKey;
                sectionStartIndex = i;
                songsInSection = 1;
            }
            else
            {
                songsInSection++;
            }
        }

        // Add last section
        if (currentSection != null)
        {
            sections.Add(new SectionHeaderModel
            {
                Title = currentSection,
                AdapterPosition = sectionStartIndex,
                SongStartIndex = sectionStartIndex,
                SongCount = songsInSection,
                Type = SectionType.DateBased
            });
        }

        return sections;
    }

    private static List<SectionHeaderModel> ComputeAlphabeticalSections(
        IList<SongModelView> songs, 
        string fieldName)
    {
        var sections = new List<SectionHeaderModel>();
        string? currentLetter = null;
        int sectionStartIndex = 0;
        int songsInSection = 0;

        for (int i = 0; i < songs.Count; i++)
        {
            var song = songs[i];
            var textValue = GetTextValueFromSong(song, fieldName);
            var letter = GetFirstLetter(textValue);

            if (currentLetter != letter)
            {
                // Save previous section if any
                if (currentLetter != null)
                {
                    sections.Add(new SectionHeaderModel
                    {
                        Title = currentLetter,
                        AdapterPosition = sectionStartIndex,
                        SongStartIndex = sectionStartIndex,
                        SongCount = songsInSection,
                        Type = SectionType.Alphabetical
                    });
                }

                // Start new section
                currentLetter = letter;
                sectionStartIndex = i;
                songsInSection = 1;
            }
            else
            {
                songsInSection++;
            }
        }

        // Add last section
        if (currentLetter != null)
        {
            sections.Add(new SectionHeaderModel
            {
                Title = currentLetter,
                AdapterPosition = sectionStartIndex,
                SongStartIndex = sectionStartIndex,
                SongCount = songsInSection,
                Type = SectionType.Alphabetical
            });
        }

        return sections;
    }

    private static DateTimeOffset? GetDateValueFromSong(SongModelView song, string fieldName)
    {
        return fieldName.ToLower() switch
        {
            "datecreated" => song.DateCreated,
            "lastplayed" => song.LastPlayed,
            _ => song.DateCreated
        };
    }

    private static string FormatDateSection(DateTimeOffset? date, SortDirection direction)
    {
        if (!date.HasValue)
        {
            return "Unknown Date";
        }

        // Format as YYYY-MM for monthly grouping
        return date.Value.ToString("yyyy-MM");
    }

    private static string GetTextValueFromSong(SongModelView song, string fieldName)
    {
        return fieldName.ToLower() switch
        {
            "title" => song.Title ?? "",
            "artistname" => song.ArtistName ?? "",
            "albumname" => song.AlbumName ?? "",
            _ => song.Title ?? ""
        };
    }

    private static string GetFirstLetter(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "#";
        }

        var firstChar = char.ToUpper(text[0]);
        
        // If it's not a letter, group under '#'
        if (!char.IsLetter(firstChar))
        {
            return "#";
        }

        return firstChar.ToString();
    }
}
