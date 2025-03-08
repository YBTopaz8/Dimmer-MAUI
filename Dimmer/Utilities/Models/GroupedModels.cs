using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer_MAUI.Utilities.Models;

public class GroupedModels
{
}
public partial class SongGroup : ObservableCollection<SongModelView>
{
    public string? AlbumName { get; set; }
    public string AlbumImagePath { get; set; } = string.Empty;
}

public partial class AlbumGroup : ObservableCollection<SongModelView>
{
    public string? AlbumName { get; set; }
    public string? AlbumId { get; set; }
    public int? ReleaseYear { get; set; }
    public string? AlbumImagePath { get; set; }
    public bool IsCurrentlySelected { get; set; }
}

public partial class ArtistGroup : ObservableCollection<SongModelView>
{
    public string? ArtistName { get; set; }
    public string? FirstLetter { get; set; }
    public string? ArtistId { get; set; }
    public string? ArtistImagePath { get; set; }
    public bool IsCurrentlySelected { get; set; }
    public ObservableCollection<SongModelView>? GroupSongs { get; set; }
    // Constructor to initialize the group
    public ArtistGroup(string? artistName, string? artistId, ObservableCollection<SongModelView> songs)
        : base(songs)
    {
        ArtistName = artistName;
        ArtistId = artistId;
        FirstLetter = string.IsNullOrEmpty(artistName) ? "#" : artistName.Substring(0, 1).ToUpper();
        GroupSongs = songs;
    }
    public ArtistGroup() { }
}

public partial class FilterLettersGroup : ObservableObject
{
    public string? Letter { get; set; }
    public bool IsSelected { get; set; }
    public int SongsCount { get; set; }
    public FilterLettersGroup(string? letter, bool isSelected)
    {
        Letter = letter;
        IsSelected = isSelected;
    }
    public FilterLettersGroup() { }
}