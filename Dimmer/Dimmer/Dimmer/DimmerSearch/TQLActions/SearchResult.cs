using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.DimmerSearch.TQLActions;

/// <summary>
/// A container for the results of a query, including the list of songs
/// and an optional song to be given special focus by the UI.
/// </summary>
public class SearchResult
{
    public static readonly SearchResult Empty = new();

    public IReadOnlyList<SongModelView> DisplayList { get; }
    public SongModelView? FocusedSong { get; }
    public bool IsFocusActionAvailable => FocusedSong is not null;

    public SearchResult(IReadOnlyList<SongModelView>? displayList = null, SongModelView? focusedSong = null)
    {
        DisplayList = displayList ?? Array.Empty<SongModelView>();
        FocusedSong = focusedSong;
    }
}