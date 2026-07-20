

    namespace Dimmer.DimmerSearch;

public static class FacetEngine
{
    public static SearchFacets GenerateFacets(IReadOnlyList<SongModel> results)
    {
        // 1. Group Artists (Alias: "ar")
        var artists = results
            .Where(s => !string.IsNullOrWhiteSpace(s.OtherArtistsName))
            .GroupBy(s => s.OtherArtistsName)
            .Select(g => new FacetItem(g.Key, g.Count(), "ar"))
            .OrderByDescending(f => f.Count)
            .Take(10) // Show top 10
            .ToList();

        // 2. Group Albums (Alias: "al")
        var albums = results
            .Where(s => !string.IsNullOrWhiteSpace(s.AlbumName))
            .GroupBy(s => s.AlbumName)
            .Select(g => new FacetItem(g.Key, g.Count(), "al"))
            .OrderByDescending(f => f.Count)
            .Take(10)
            .ToList();

        // 3. Group Genres (Alias: "genre")
        var genres = results
            .Where(s => !string.IsNullOrWhiteSpace(s.GenreName))
            .GroupBy(s => s.GenreName)
            .Select(g => new FacetItem(g.Key, g.Count(), "genre"))
            .OrderByDescending(f => f.Count)
            .Take(10)
            .ToList();

        return new SearchFacets(artists, albums, genres);
    }
}

public record FacetItem(string Value, int Count, string FieldAlias);

public record SearchFacets(
    IReadOnlyList<FacetItem> Artists,
    IReadOnlyList<FacetItem> Albums,
    IReadOnlyList<FacetItem> Genres
);
