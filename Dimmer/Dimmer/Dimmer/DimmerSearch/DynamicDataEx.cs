using DynamicData;

namespace Dimmer.DimmerSearch;
public static class DynamicDataEx
{
    // This signature is specific and correct.
    public static IObservable<IChangeSet<TObject, TKey>> Limit<TObject, TKey>(
        this IObservable<IChangeSet<TObject, TKey>> source,
        IObservable<LimiterClause?> limiter)
        where TObject : notnull
        where TKey : notnull
    {
        return limiter
            .Select(limit =>
            {
                if (limit is null)
                    return source;

                return limit.Type switch
                {
                    LimiterType.First or LimiterType.Random =>
                        source.Take(limit.Count == int.MaxValue ? 10_000 : limit.Count),
                    LimiterType.Last =>
                        source.TakeLast(limit.Count),
                    _ => source
                };
            })
            .Switch();
    }
}