namespace Dimmer.DimmerSearch.TQL;

/// <summary>
/// Provides ICU-style collation for accent-insensitive and case-insensitive string comparison.
/// This enables keyboard-agnostic matching where "doree" matches "dorée".
/// </summary>
public static class CollationHelper
{
    private static readonly CompareInfo CompareInfo = CultureInfo.InvariantCulture.CompareInfo;
    private const CompareOptions CollationOptions = CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreCase;

    /// <summary>
    /// Checks if the source string contains the query string using accent-insensitive and case-insensitive comparison.
    /// </summary>
    /// <param name="source">The string to search in.</param>
    /// <param name="query">The string to search for.</param>
    /// <returns>True if source contains query (ignoring accents and case), false otherwise.</returns>
    public static bool Contains(string source, string query)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(query))
            return false;

        return CompareInfo.IndexOf(source, query, CollationOptions) >= 0;
    }

    /// <summary>
    /// Checks if the source string starts with the query string using accent-insensitive and case-insensitive comparison.
    /// </summary>
    /// <param name="source">The string to check.</param>
    /// <param name="query">The prefix to check for.</param>
    /// <returns>True if source starts with query (ignoring accents and case), false otherwise.</returns>
    public static bool StartsWith(string source, string query)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(query))
            return false;

        return CompareInfo.IsPrefix(source, query, CollationOptions);
    }

    /// <summary>
    /// Checks if the source string ends with the query string using accent-insensitive and case-insensitive comparison.
    /// </summary>
    /// <param name="source">The string to check.</param>
    /// <param name="query">The suffix to check for.</param>
    /// <returns>True if source ends with query (ignoring accents and case), false otherwise.</returns>
    public static bool EndsWith(string source, string query)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(query))
            return false;

        return CompareInfo.IsSuffix(source, query, CollationOptions);
    }

    /// <summary>
    /// Checks if the source string equals the query string using accent-insensitive and case-insensitive comparison.
    /// </summary>
    /// <param name="source">The first string to compare.</param>
    /// <param name="query">The second string to compare.</param>
    /// <returns>True if source equals query (ignoring accents and case), false otherwise.</returns>
    public static bool Equals(string source, string query)
    {
        if (source == null || query == null)
            return source == query; // Both null or one is null

        return CompareInfo.Compare(source, query, CollationOptions) == 0;
    }

    /// <summary>
    /// Gets the index of the first occurrence of query in source using accent-insensitive and case-insensitive comparison.
    /// </summary>
    /// <param name="source">The string to search in.</param>
    /// <param name="query">The string to search for.</param>
    /// <returns>The zero-based index of the first occurrence of query in source, or -1 if not found.</returns>
    public static int IndexOf(string source, string query)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(query))
            return -1;

        return CompareInfo.IndexOf(source, query, CollationOptions);
    }
}
