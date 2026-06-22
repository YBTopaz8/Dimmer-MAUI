namespace Dimmer.DimmerSearch;

public partial class SongModelEqualityComparer : IEqualityComparer<SongModel>
{
    public bool Equals(SongModel? x, SongModel? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;

        // Ensure keys are generated
        var keyX = x.TitleDurationKey ?? $"{x.Title?.ToLowerInvariant()}|{x.DurationInSeconds}";
        var keyY = y.TitleDurationKey ?? $"{y.Title?.ToLowerInvariant()}|{y.DurationInSeconds}";

        return keyX == keyY;
    }

    public int GetHashCode(SongModel obj)
    {
        if (obj.TitleDurationKey == null)
            return $"{obj.Title?.ToLowerInvariant()}|{obj.DurationInSeconds}".GetHashCode();

        return obj.TitleDurationKey.GetHashCode();
    }
}