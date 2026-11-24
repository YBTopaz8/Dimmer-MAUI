using MongoDB.Bson;

namespace Dimmer.DimmerSearch;

public static class FacetEngine
{
    public static SearchFacets? GenerateFacets(IEnumerable<SongModelView>? vmResults)
    {
        if (vmResults is null)
            return null;

        var songs = vmResults as IList<SongModelView> ?? vmResults.ToList();

        // 1. Group Artists (Alias: "ar")
        var artists = songs
            .Where(s => !string.IsNullOrWhiteSpace(s.OtherArtistsName))
            .GroupBy(s => s.OtherArtistsName, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var artistId = g
                    .Select(s =>
                    {
                        if (s.Artist is not null && s.Artist.Id != ObjectId.Empty)
                            return s.Artist.Id;

                        return s.ArtistToSong?
                            .FirstOrDefault(a => a is not null && a.Id != ObjectId.Empty)?.Id
                            ?? ObjectId.Empty;
                    })
                    .FirstOrDefault(id => id != ObjectId.Empty);

                return new FacetItem(
                    Value: g.Key,
                    objectId: artistId,
                    Count: g.Count(),
                    FieldAlias: "ar");
            })
            .OrderByDescending(f => f.Count)
            .Take(10)
            .ToList();

        // 2. Group Albums (Alias: "al")
        var albums = songs
            .Where(s => !string.IsNullOrWhiteSpace(s.AlbumName))
            .GroupBy(s => s.AlbumName, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var albumId = g
                    .Select(s => s.Album?.Id ?? ObjectId.Empty)
                    .FirstOrDefault(id => id != ObjectId.Empty);
                return new FacetItem(
                    Value: g.Key,
                    objectId: albumId,
                    Count: g.Count(),
                    FieldAlias: "al");
            })
            .OrderByDescending(f => f.Count)
            .Take(10)
            .ToList();

        // 3. Group Genres (Alias: "genre")
        var genres = songs
            .Where(s => !string.IsNullOrWhiteSpace(s.GenreName))
            .GroupBy(s => s.GenreName, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var genreId = g
                    .Select(s => s.Genre?.Id ?? ObjectId.Empty)
                    .FirstOrDefault(id => id != ObjectId.Empty);

                return new FacetItem(
                    Value: g.Key,
                    objectId: genreId,
                    Count: g.Count(),
                    FieldAlias: "genre");
            })
            .OrderByDescending(f => f.Count)
            .Take(10)
            .ToList();

        return new SearchFacets(artists, albums, genres);
    }
}