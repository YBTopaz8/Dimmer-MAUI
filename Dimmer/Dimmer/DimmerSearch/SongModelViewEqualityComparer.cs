    using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.DimmerSearch;

public partial class SongModelViewEqualityComparer : IEqualityComparer<SongModelView>
{
    public bool Equals(SongModelView? x, SongModelView? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;

        // Ensure keys are generated
        var keyX = x.TitleDurationKey ?? $"{x.Title?.ToLowerInvariant()}|{x.DurationInSeconds}";
        var keyY = y.TitleDurationKey ?? $"{y.Title?.ToLowerInvariant()}|{y.DurationInSeconds}";

        return keyX == keyY;
    }

    public int GetHashCode(SongModelView obj)
    {
        if (obj.TitleDurationKey == null)
            return $"{obj.Title?.ToLowerInvariant()}|{obj.DurationInSeconds}".GetHashCode();

        return obj.TitleDurationKey.GetHashCode();
    }
}