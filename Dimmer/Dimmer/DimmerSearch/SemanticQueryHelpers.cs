using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace Dimmer.DimmerSearch;

public static class SemanticQueryHelpers
{
    // This is a thread-safe dictionary that will act as our high-speed cache.
    // It stores the super-fast functions we are about to create.
    // The key will be a string like "SongModel.Title"
    // The value will be a compiled delegate like Func<SongModel, object>
    private static readonly ConcurrentDictionary<string, Delegate> _accessorCache = new();

    /// <summary>
    /// This is the core of our performance optimization.
    /// It takes a type (like SongModel) and a property name (like "Title" or "Genre.Name")
    /// and returns a pre-compiled, lightning-fast function to get that property's value.
    /// The slow work of finding the property is only done ONCE. After that, it's retrieved
    /// from the cache.
    /// </summary>
    /// <typeparam name="T">The type of the object to get the property from (e.g., SongModel).</typeparam>
    /// <typeparam name="TResult">The expected return type of the property (we use 'object' for flexibility).</typeparam>
    /// <param name="propertyName">The name of the property, supporting nesting like "Genre.Name".</param>
    /// <returns>A compiled function that gets the property value.</returns>
    // Step 1: Modify GetAccessor to build the chain and collect the parts.
    private static readonly object _compilationLock = new();
    private static Func<T, TResult> GetAccessor<T, TResult>(string propertyName)
    {
        string cacheKey = $"{typeof(T).FullName}.{propertyName}";
        if (_accessorCache.TryGetValue(cacheKey, out Delegate? cachedAccessor))
        {
            return (Func<T, TResult>)cachedAccessor;
        }
        lock (_compilationLock)
        {
            var parameter = Expression.Parameter(typeof(T), "model");

        // --- REFACTORED PART ---
        var propertyAccessors = new List<Expression>();
        Expression currentExpression = parameter;
        propertyAccessors.Add(currentExpression); // Add the root object itself (e.g., song)

        foreach (var member in propertyName.Split('.'))
        {
            var propertyInfo = currentExpression.Type.GetProperty(member)
                ?? throw new ArgumentException($"Property '{member}' not found on type '{currentExpression.Type.Name}'");

            currentExpression = Expression.Property(currentExpression, propertyInfo);
            propertyAccessors.Add(currentExpression);
        }

        // --- END REFACTORED PART ---

        // Pass the collected parts to the helper to add null checks.
        var finalBody = AddNullChecks<TResult>(propertyAccessors);

        var lambda = Expression.Lambda<Func<T, TResult>>(finalBody, parameter);
        Func<T, TResult> compiledLambda = lambda.Compile();
        _accessorCache[cacheKey] = compiledLambda;
        return compiledLambda;
        }
    }
    // Step 2: Modify AddNullChecks to simply assemble the parts it's given.
    private static Expression AddNullChecks<TResult>(List<Expression> propertyAccessors)
    {
        // The actual final value (e.g., song.Genre.Name)
        var finalPropertyAccess = propertyAccessors.Last();

        // The parts to check for null (e.g., song, song.Genre)
        // We don't need to check the final property itself, just the path to it.
        var chainPartsToTest = propertyAccessors.Take(propertyAccessors.Count - 1);

        var nullConditions = new List<Expression>();
        foreach (var part in chainPartsToTest)
        {
            // Only add a null check if the type can be null (a class or Nullable<T>)
            if (part.Type.IsClass || Nullable.GetUnderlyingType(part.Type) != null)
            {
                var nullCheck = Expression.Equal(part, Expression.Constant(null, part.Type));
                nullConditions.Add(nullCheck);
            }
        }

        if (nullConditions.Count == 0)
        {
            return Expression.Convert(finalPropertyAccess, typeof(TResult));
        }

        Expression combinedNullCheck = nullConditions[0];
        for (int i = 1; i < nullConditions.Count; i++)
        {
            combinedNullCheck = Expression.OrElse(combinedNullCheck, nullConditions[i]);
        }

        return Expression.Condition(
            combinedNullCheck,
            Expression.Default(typeof(TResult)),
            Expression.Convert(finalPropertyAccess, typeof(TResult))
        );
    }

    public static DateTimeOffset? GetDateProp(SongModel song, string propertyName)
    {
        // Get the super-fast accessor for this property.
        var accessor = GetAccessor<SongModel, object>(propertyName);

        // Run the fast function.
        var value = accessor(song);

        // Perform the conversion logic on the result.
        if (value is DateTimeOffset dto)
            return dto;
        if (value is DateTime dt)
            return new DateTimeOffset(dt);

        return null;
    }
    public static string GetStringProp(SongModel song, string name)
    {
        return name switch
        {
            nameof(SongModel.Title) => song.Title ?? "",
            nameof(SongModel.OtherArtistsName) => song.OtherArtistsName ?? "",
            nameof(SongModel.AlbumName) => song.AlbumName ?? "",
            nameof(SongModel.GenreName) => song.GenreName ?? "",
            nameof(SongModel.FilePath) => song.FilePath ?? "",
            _ => GetAccessor<SongModel, object>(name)(song) as string ?? ""
        };
    }

    public static double GetNumericProp(SongModel song, string name)
    {
        return name switch
        {
            nameof(SongModel.DurationInSeconds) => song.DurationInSeconds,
            nameof(SongModel.ReleaseYear) => song.ReleaseYear is null ? 0:(double)song.ReleaseYear,
            nameof(SongModel.PlayCount) => song.PlayCount,
            nameof(SongModel.PlayCompletedCount) => song.PlayCompletedCount,
            nameof(SongModel.SkipCount) => song.SkipCount,
            nameof(SongModel.Rating) => song.Rating,
            nameof(SongModel.BitRate) => song.BitRate is null ? 0 : (double)song.BitRate,
            _ => Convert.ToDouble(GetAccessor<SongModel, object>(name)(song) ?? 0)
        };
    }

    public static bool GetBoolProp(SongModel song, string name)
    {
        return name switch
        {
            nameof(SongModel.IsFavorite) => song.IsFavorite,
            nameof(SongModel.HasLyrics) => song.HasLyrics,
            nameof(SongModel.HasSyncedLyrics) => song.HasSyncedLyrics,
            _ => Convert.ToBoolean(GetAccessor<SongModel, object>(name)(song) ?? false)
        };
    }

    // This is used for sorting. It also benefits from the fast accessor.
    public static IComparable? GetComparableProp(SongModel song, string name)
    {
        var accessor = GetAccessor<SongModel, object>(name);
        var propValue = accessor(song);

        // If the property is null, return a default value to avoid crashes during sorting.
        if (propValue == null)
            return string.Empty;

        // Return the value if it's naturally comparable (like a number, string, or date).
        if (propValue is IComparable comparable)
        {
            return comparable;
        }

        // As a last resort, convert it to a string for comparison.
        return propValue.ToString();
    }

    // The Levenshtein Distance function doesn't rely on reflection,
    // so it can remain as it was. It's a pure string algorithm.
    public static int LevenshteinDistance(string s, string t)
    {
        if (string.IsNullOrEmpty(s))
            return string.IsNullOrEmpty(t) ? 0 : t.Length;
        if (string.IsNullOrEmpty(t))
            return s.Length;

        int n = s.Length;
        int m = t.Length;
        int[,] d = new int[n + 1, m + 1];

        for (int i = 0; i <= n; d[i, 0] = i++)
            ;
        for (int j = 0; j <= m; d[0, j] = j++)
            ;

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }
        return d[n, m];
    }
}