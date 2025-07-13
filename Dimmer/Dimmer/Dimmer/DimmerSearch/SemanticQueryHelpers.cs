using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace Dimmer.DimmerSearch;

public static class SemanticQueryHelpers
{
    // This is a thread-safe dictionary that will act as our high-speed cache.
    // It stores the super-fast functions we are about to create.
    // The key will be a string like "SongModelView.Title"
    // The value will be a compiled delegate like Func<SongModelView, object>
    private static readonly ConcurrentDictionary<string, Delegate> _accessorCache = new();

    /// <summary>
    /// This is the core of our performance optimization.
    /// It takes a type (like SongModelView) and a property name (like "Title" or "Genre.Name")
    /// and returns a pre-compiled, lightning-fast function to get that property's value.
    /// The slow work of finding the property is only done ONCE. After that, it's retrieved
    /// from the cache.
    /// </summary>
    /// <typeparam name="T">The type of the object to get the property from (e.g., SongModelView).</typeparam>
    /// <typeparam name="TResult">The expected return type of the property (we use 'object' for flexibility).</typeparam>
    /// <param name="propertyName">The name of the property, supporting nesting like "Genre.Name".</param>
    /// <returns>A compiled function that gets the property value.</returns>
    private static Func<T, TResult> GetAccessor<T, TResult>(string propertyName)
    {
        // 1. Create a unique key for our cache.
        string cacheKey = $"{typeof(T).FullName}.{propertyName}";

        // 2. Try to get the pre-compiled function from our cache. If it exists, we're done!
        if (_accessorCache.TryGetValue(cacheKey, out Delegate? cachedAccessor))
        {
            return (Func<T, TResult>)cachedAccessor;
        }

        // 3. If it's not in the cache, we must build it. This is the "slow" part that only runs once per property.

        // Create a parameter for our function. This is equivalent to `song => ...` where `song` is the parameter.
        ParameterExpression parameter = Expression.Parameter(typeof(T), "model");
        Expression body = parameter;

        // Handle nested properties. For a name like "Genre.Name", this loop will first
        // create `song.Genre` and then create `song.Genre.Name`.
        foreach (var member in propertyName.Split('.'))
        {
            // We need to handle nulls gracefully. `Expression.Property` would crash if Genre is null.
            // So we build a null-check.
            PropertyInfo propertyInfo = body.Type.GetProperty(member) ?? throw new ArgumentException($"Property '{member}' not found on type '{body.Type.Name}'");
            body = Expression.Property(body, propertyInfo);
        }

        // The final property might not be the exact TResult type (e.g., it might be an 'int' but we want 'object').
        // We create a conversion to make sure the function signature is correct.
        var bodyAsObject = Expression.Convert(body, typeof(TResult));

        // This builds a "conditional" expression. It's the C# equivalent of:
        // song => (song == null || song.Genre == null) ? default(TResult) : (TResult)song.Genre.Name
        // This prevents NullReferenceExceptions if an intermediate property is null.
        Expression finalBody = AddNullChecks<T, TResult>(parameter, propertyName);

        // 4. Compile the complete expression tree into a real, executable .NET function.
        var lambda = Expression.Lambda<Func<T, TResult>>(finalBody, parameter);
        Func<T, TResult> compiledLambda = lambda.Compile();

        // 5. Store the brand new function in our cache for next time.
        _accessorCache[cacheKey] = compiledLambda;

        return compiledLambda;
    }

    /// <summary>
    /// A helper method to recursively build null-checks into an expression tree.
    /// This ensures that accessing a nested property like "Genre.Name" doesn't crash if "Genre" is null.
    /// </summary>
    private static Expression AddNullChecks<T, TResult>(ParameterExpression parameter, string propertyName)
    {
        Expression body = parameter;
        Expression fullChain = parameter;
        var nullConditions = new List<Expression>();

        foreach (var member in propertyName.Split('.'))
        {
            var propertyInfo = fullChain.Type.GetProperty(member) ?? throw new ArgumentException($"Property '{member}' not found on type '{fullChain.Type.Name}'");

            // For every property in the chain (except the last), add a null check.
            // e.g., for "Genre.Name", we add a check for "Genre != null".
            if (propertyInfo.PropertyType.IsClass || Nullable.GetUnderlyingType(propertyInfo.PropertyType) != null)
            {
                var nullCheck = Expression.Equal(fullChain, Expression.Constant(null, fullChain.Type));
                nullConditions.Add(nullCheck);
            }

            fullChain = Expression.Property(fullChain, propertyInfo);
        }

        if (nullConditions.Count == 0)
        {
            return Expression.Convert(fullChain, typeof(TResult));
        }

        // Combine all null checks with an "Or" condition.
        // e.g., (song == null || song.Genre == null)
        Expression combinedNullCheck = nullConditions[0];
        for (int i = 1; i < nullConditions.Count; i++)
        {
            combinedNullCheck = Expression.OrElse(combinedNullCheck, nullConditions[i]);
        }

        // The final expression:
        // if (song == null || song.Genre == null) return default; else return (TResult)song.Genre.Name;
        return Expression.Condition(
            combinedNullCheck,
            Expression.Default(typeof(TResult)),
            Expression.Convert(fullChain, typeof(TResult))
        );
    }

    // --- PUBLIC HELPER METHODS ---
    // These are the methods your `AsPredicate()` functions will call.
    // They use the high-speed accessor system internally.

    public static string GetStringProp(SongModelView song, string name)
    {
        // Get the super-fast accessor for this property (e.g., "Title").
        var accessor = GetAccessor<SongModelView, object>(name);
        // Run the fast function and safely convert the result to a string.
        return accessor(song) as string ?? "";
    }
    public static DateTimeOffset? GetDateProp(SongModelView song, string propertyName)
    {
        var propInfo = typeof(SongModelView).GetProperty(propertyName);
        if (propInfo == null)
            return null;

        var value = propInfo.GetValue(song);
        if (value is DateTimeOffset dto)
            return dto;
        if (value is DateTime dt)
            return new DateTimeOffset(dt);

        return null;
    }
    public static double GetNumericProp(SongModelView song, string name)
    {
        var accessor = GetAccessor<SongModelView, object>(name);
        // Run the fast function and safely convert to double. '0' is the default if null.
        return Convert.ToDouble(accessor(song) ?? 0);
    }

    public static bool GetBoolProp(SongModelView song, string name)
    {
        var accessor = GetAccessor<SongModelView, object>(name);
        // Run the fast function and safely convert to bool. 'false' is the default if null.
        return Convert.ToBoolean(accessor(song) ?? false);
    }

    // This is used for sorting. It also benefits from the fast accessor.
    public static IComparable? GetComparableProp(SongModelView song, string name)
    {
        var accessor = GetAccessor<SongModelView, object>(name);
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