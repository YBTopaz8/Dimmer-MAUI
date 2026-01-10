namespace Dimmer.ViewsAndPages.NativeViews.SectionHeader;

/// <summary>
/// Wrapper class that can represent either a section header or a song.
/// Used in the adapter to support multiple view types.
/// </summary>
public class ListItem
{
    public enum ItemType
    {
        Header,
        Song
    }

    public ItemType Type { get; set; }
    public SectionHeaderModel? Header { get; set; }
    public SongModelView? Song { get; set; }

    public static ListItem CreateHeader(SectionHeaderModel header)
    {
        return new ListItem
        {
            Type = ItemType.Header,
            Header = header
        };
    }

    public static ListItem CreateSong(SongModelView song)
    {
        return new ListItem
        {
            Type = ItemType.Song,
            Song = song
        };
    }
}
