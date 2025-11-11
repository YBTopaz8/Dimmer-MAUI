namespace Dimmer.Data.Models;
public partial class PlaybackStateInfo : IEquatable<PlaybackStateInfo>
{
    public SongModel? Songdb;

    public DimmerUtilityEnum State { get; set; }
    public object? ExtraParameter { get; set; }
    public SongModelView? SongView { get; set; }

    public double? ContextSongPositionSeconds { get; set; } = 0.0;
   

    public PlaybackStateInfo(DimmerUtilityEnum state, object? extParam, SongModelView? song, SongModel? songdb)
    {
        SongView    =song;
        State = state;
        ExtraParameter = extParam;
        this.Songdb=songdb;
    }
    public bool Equals(PlaybackStateInfo? other)
    {
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;

        // Compare relevant properties
        bool statesEqual = State == other.State;
        bool extrasEqual = true; // Default to true

        // Careful with ExtraParameter comparison:
        // If ExtraParameter is a list, you might need to compare contents (e.g., SequenceEqual)
        // or decide if its reference equality is enough.
        if (ExtraParameter is IReadOnlyList<SongModel> thisList && other.ExtraParameter is IReadOnlyList<SongModel> otherList)
        {
            extrasEqual = thisList.SequenceEqual(otherList); // Example: content equality for song lists
        }
        else if(Songdb != other.Songdb || (SongView != other.SongView))
        {
            return false;
        }
        else
        {
            extrasEqual = Equals(ExtraParameter, other.ExtraParameter); // Default object.Equals or reference
        }
        return statesEqual && extrasEqual;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as PlaybackStateInfo);
    }

    public override int GetHashCode()
    {
        // Important to override GetHashCode if you override Equals
        unchecked
        {
            int hash = 17;
            hash = hash * 23 + State.GetHashCode();
            // Be careful with ExtraParameter in GetHashCode if it's mutable or complex
            hash = hash * 23 + (ExtraParameter?.GetHashCode() ?? 0);
            return hash;
        }
    }
}