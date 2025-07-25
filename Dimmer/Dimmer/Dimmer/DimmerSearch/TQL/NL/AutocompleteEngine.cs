﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.DimmerSearch.AbstractQueryTree.NL;

public class AutocompleteEngine
{
    // These collections are populated by your ViewModel using DynamicData
    private readonly ReadOnlyObservableCollection<string> _distinctArtists;
    private readonly ReadOnlyObservableCollection<string> _distinctAlbums;
    private readonly ReadOnlyObservableCollection<string> _distinctGenres;

    public AutocompleteEngine(
        ReadOnlyObservableCollection<string> artists,
        ReadOnlyObservableCollection<string> albums,
        ReadOnlyObservableCollection<string> genres)
    {
        _distinctArtists = artists;
        _distinctAlbums = albums;
        _distinctGenres = genres;
    }

    public ObservableCollection<string> GetSuggestions(string queryText, int cursorPosition)
    {
        if (cursorPosition == 0 || string.IsNullOrWhiteSpace(queryText))
        {
            // Suggest common fields to start with
            return FieldRegistry.AllFields.Select(f => f.PrimaryName + ":").Take(5).ToObservableCollection();
        }

        // Find the start of the word the cursor is in
        int wordStart = queryText.LastIndexOf(' ', cursorPosition - 1) + 1;
        string currentWord = queryText.Substring(wordStart, cursorPosition - wordStart);

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

        // Provide suggestions based on the field's type
        switch (fieldDef.PrimaryName)
        {
            case "OtherArtistsName":
                return _distinctArtists
                    .Where(a => a != null && a.StartsWith(valuePrefix, StringComparison.OrdinalIgnoreCase))
                    .Select(a => a.Contains(' ') ? $"\"{a}\"" : a) // Quote if it has spaces
                    .Take(10).ToObservableCollection();

            case "AlbumName":
                return _distinctAlbums
                    .Where(a => a != null && a.StartsWith(valuePrefix, StringComparison.OrdinalIgnoreCase))
                    .Select(a => a.Contains(' ') ? $"\"{a}\"" : a)
                    .Take(10).ToObservableCollection();

            case "GenreName":
                return _distinctGenres
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
}