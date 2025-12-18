namespace Dimmer.DimmerSearch.TQL;

public static class TqlConverter
{
    // A cache to avoid repeated lookups for the same header.
    private static readonly Dictionary<string, string?> _headerToAliasCache = new();

    /// <summary>
    /// Converts a "Header\nValue" string from the TableView into a valid TQL clause.
    /// Example: "Artist\nKanye West" -> "artist:\"Kanye West\""
    /// </summary>
    /// <param name="tableViewContent">The string content from the table view.</param>
    /// <returns>A TQL clause string, or an empty string if conversion fails.</returns>
    public static string ConvertTableViewContentToTql(string tableViewContent)
    {
        if (string.IsNullOrWhiteSpace(tableViewContent))
            return string.Empty;

        var parts = tableViewContent.Split(new[] { '\n' }, 2);
        if (parts.Length != 2)
            return string.Empty; // Invalid format

        var header = parts[0].Trim();
        var value = parts[1].Trim();

        var fieldAlias = FindFieldAliasFromHeader(header);
        if (fieldAlias is null)
        {
            // If we can't map the header, we can't create a specific clause.
            // You could fall back to a general "any" search if you wanted.
            return string.Empty;
        }

        // TQL values with spaces must be quoted.
        if (value.Contains(' ') && !value.StartsWith("\""))
        {
            // Also escape any existing quotes inside the value string
            value = $"\"{value.Replace("\"", "\\\"")}\"";
        }

        return $"{fieldAlias}:{value}";
    }

    /// <summary>
    /// Finds the most appropriate TQL field alias based on a user-facing column header.
    /// It's robust against differences in spacing and casing.
    /// </summary>
    private static string? FindFieldAliasFromHeader(string header)
    {
        if (_headerToAliasCache.TryGetValue(header, out var cachedAlias))
        {
            return cachedAlias;
        }

        string normalizedHeader = header.Replace(" ", "").ToLowerInvariant();

        foreach (var fieldDef in FieldRegistry.AllFields)
        {
            // Check the primary name first
            if (fieldDef.PrimaryName.Replace(" ", "").Equals(normalizedHeader, StringComparison.OrdinalIgnoreCase))
            {
                _headerToAliasCache[header] = fieldDef.Aliases.First(); // Cache and return the primary alias
                return fieldDef.Aliases.First();
            }

            // Then check all aliases
            foreach (var alias in fieldDef.Aliases)
            {
                if (alias.Equals(normalizedHeader, StringComparison.OrdinalIgnoreCase))
                {
                    _headerToAliasCache[header] = alias; // Cache and return the matched alias
                    return alias;
                }
            }
        }

        // Special case for your "Artist" column, which maps to OtherArtistsName
        if (normalizedHeader == "artist")
        {
            var artistField = FieldRegistry.FieldsByAlias["artist"];
            _headerToAliasCache[header] = artistField.Aliases.First();
            return artistField.Aliases.First();
        }


        _headerToAliasCache[header] = null; // Cache the failure
        return null;
    }
}