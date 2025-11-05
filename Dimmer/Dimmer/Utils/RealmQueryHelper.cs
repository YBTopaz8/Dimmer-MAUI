using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Realms;

namespace Dimmer.Utils;

public static class RealmQueryHelper
{
    // --- SONGS ------------------------------------------------------------
    public static IQueryable<SongModel> SongsPlayedAfter(Realm realm, DateTimeOffset date) =>
    realm.All<SongModel>().Where(s => s.LastPlayed > date);

    public static IQueryable<SongModel> SongsPlayedBefore(Realm realm, DateTimeOffset date) =>
        realm.All<SongModel>().Where(s => s.LastPlayed < date);
    // All songs by an artist (RQL-safe)
    public static IQueryable<SongModel> SongsByArtist(Realm realm, ObjectId artistId) =>
        realm.All<SongModel>().Where(s => s.Artist.Id == artistId);

    // Songs in a specific album
    public static IQueryable<SongModel> SongsInAlbum(Realm realm, ObjectId albumId) =>
        realm.All<SongModel>().Where(s => s.Album.Id == albumId);
    public static IQueryable<SongModel> SongsInPlaylist(Realm realm, ObjectId playlistId) =>
        realm.All<SongModel>().Filter("ANY PlaylistsHavingSong.Id == $0", playlistId);


    // Songs of a given genre
    public static IQueryable<SongModel> SongsByGenre(Realm realm, ObjectId genreId) =>
        realm.All<SongModel>().Where(s => s.Genre.Id == genreId);

    // Songs rated higher than a value
    public static IQueryable<SongModel> SongsWithRatingAbove(Realm realm, int rating) =>
        realm.All<SongModel>().Where(s => s.Rating > rating);

    // Favorite songs
    public static IQueryable<SongModel> FavoriteSongs(Realm realm) =>
        realm.All<SongModel>().Where(s => s.IsFavorite);

    // Songs missing genre or album
    public static IQueryable<SongModel> SongsWithoutGenre(Realm realm) =>
        realm.All<SongModel>().Where(s => s.Genre == null);

    public static IQueryable<SongModel> SongsWithoutAlbum(Realm realm) =>
        realm.All<SongModel>().Where(s => s.Album == null);

    // Songs whose file is missing
    public static IQueryable<SongModel> MissingFiles(Realm realm) =>
        realm.All<SongModel>().Where(s => s.IsFileExists == false);

    // Songs containing text (case-insensitive search)
    public static IQueryable<SongModel> SongsTitleContains(Realm realm, string term) =>
        realm.All<SongModel>().Where(s => QueryMethods.Contains(s.Title, term, StringComparison.OrdinalIgnoreCase));

    public static IQueryable<SongModel> SongsByLanguage(Realm realm, string lang) =>
    realm.All<SongModel>().Where(s => s.Language == lang);

    public static IQueryable<SongModel> SongsFileFormat(Realm realm, string format) =>
        realm.All<SongModel>().Where(s => s.FileFormat == format);

    public static IQueryable<SongModel> SongsTitleStartsWith(Realm realm, string prefix) =>
        realm.All<SongModel>().Where(s => s.Title.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    public static IQueryable<ArtistModel> ArtistsDiscoveredBefore(Realm realm, DateTimeOffset date) =>
    realm.All<ArtistModel>().Where(a => a.DiscoveryDate < date);

    public static IQueryable<SongModel> SongsRatedBelow(Realm realm, int rating) =>
        realm.All<SongModel>().Where(s => s.Rating < rating && s.Rating > 0);

    public static IQueryable<SongModel> SongsWithAtLeastPlays(Realm realm, int count) =>
    realm.All<SongModel>().Where(s => s.PlayCount >= count);

    public static IQueryable<SongModel> SongsSkippedMoreThan(Realm realm, int skips) =>
        realm.All<SongModel>().Where(s => s.SkipCount > skips);

    public static IQueryable<SongModel> HighlyPlayedFavorites(Realm realm, int minPlays) =>
        realm.All<SongModel>().Where(s => s.IsFavorite && s.PlayCount > minPlays);

    // --- ARTISTS ----------------------------------------------------------
    public static IQueryable<AlbumModel> AlbumsWithCompletionAbove(Realm realm, double threshold) =>
    realm.All<AlbumModel>().Where(a => a.CompletionPercentage > threshold);

    public static IQueryable<ArtistModel> ArtistsWithEddingtonAbove(Realm realm, double value) =>
        realm.All<ArtistModel>().Where(a => a.EddingtonNumber > value);

    // Artists linked to a specific song (works via Filter + backlink)
    public static IQueryable<ArtistModel> ArtistsLinkedToSong(Realm realm, ObjectId songId) =>
        realm.All<ArtistModel>().Filter("ANY Songs.Id == $0", songId);

    // Artists linked to a specific album (via each song’s artist link)
    public static IQueryable<ArtistModel> ArtistsLinkedToAlbum(Realm realm, ObjectId albumId) =>
        realm.All<ArtistModel>().Filter("ANY Songs.Album.Id == $0", albumId);

    // Artists with no songs
    public static IQueryable<ArtistModel> ArtistsWithoutSongs(Realm realm) =>
        realm.All<ArtistModel>().Where(a => a.Songs.Count() == 0);

    // Artists discovered recently
    public static IQueryable<ArtistModel> ArtistsDiscoveredAfter(Realm realm, DateTimeOffset date) =>
        realm.All<ArtistModel>().Where(a => a.DiscoveryDate > date);

    // Artists whose name starts with
    public static IQueryable<ArtistModel> ArtistsNameStartsWith(Realm realm, string prefix) =>
        realm.All<ArtistModel>().Where(a => a.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

    // --- ALBUMS -----------------------------------------------------------

    // Albums by a given artist
    public static IQueryable<AlbumModel> AlbumsByArtist(Realm realm, ObjectId artistId) =>
        realm.All<AlbumModel>().Filter("ANY ArtistIds.Id == $0", artistId);

    // Albums missing artwork
    public static IQueryable<AlbumModel> AlbumsWithoutCover(Realm realm) =>
        realm.All<AlbumModel>().Where(a => a.ImagePath == "musicalbum.png" || a.ImagePath == null);

    // Albums released in a year
    public static IQueryable<AlbumModel> AlbumsByYear(Realm realm, int year) =>
        realm.All<AlbumModel>().Where(a => a.ReleaseYear == year);

    public static IQueryable<AlbumModel> AlbumsOfGenre(Realm realm, ObjectId genreId) =>
    realm.All<AlbumModel>().Filter("ANY SongsInAlbum.Genre.Id == $0", genreId);

    public static IQueryable<ArtistModel> ArtistsOfGenre(Realm realm, ObjectId genreId) =>
        realm.All<ArtistModel>().Filter("ANY Songs.Genre.Id == $0", genreId);


    // --- GENRES -----------------------------------------------------------

    // All songs within a genre
    public static IQueryable<GenreModel> GenresByNameContains(Realm realm, string namePart) =>
        realm.All<GenreModel>().Where(g => QueryMethods.Contains(g.Name, namePart, StringComparison.OrdinalIgnoreCase));

    // Genres with no songs
    public static IQueryable<GenreModel> GenresWithoutSongs(Realm realm) =>
        realm.All<GenreModel>().Where(g => g.Songs.Count() == 0);

    // --- PLAYLISTS --------------------------------------------------------

    // Manual playlists only
    public static IQueryable<PlaylistModel> ManualPlaylists(Realm realm) =>
        realm.All<PlaylistModel>().Where(p => p.IsSmartPlaylist == false);

    // Smart playlists only
    public static IQueryable<PlaylistModel> SmartPlaylists(Realm realm) =>
        realm.All<PlaylistModel>().Where(p => p.IsSmartPlaylist);

    // Playlists created after a certain date
    public static IQueryable<PlaylistModel> PlaylistsCreatedAfter(Realm realm, DateTimeOffset date) =>
        realm.All<PlaylistModel>().Where(p => p.DateCreated > date);

    // --- LINKED / MIXED QUERIES ------------------------------------------

    // Songs that share the same artist as a given song
    public static IQueryable<SongModel> SongsBySameArtist(Realm realm, ObjectId songId)
    {

        
        var artistIds = realm.All<SongModel>()
                             .Filter("Id == $0", songId)
                             .Select(s => (QueryArgument)s.Artist.Id)
                             .ToArray();
        return realm.All<SongModel>().Filter("Artist.Id IN $0)", artistIds);
    }

    // Songs by artists in a specific album (Album → Songs → Artists → Songs)
    public static IQueryable<SongModel> SongsLinkedToAlbumArtists(Realm realm, ObjectId albumId)
    {
        var artistIds = realm.All<ArtistModel>()
                             .Filter("ANY Songs.Album.Id == $0", albumId)
                             .Select(a => (QueryArgument)a.Id)
                             .ToArray();

        return realm.All<SongModel>().Filter("Artist.Id IN $0", artistIds);
    }

    // Songs by album’s artists (instant via backlinks)
    public static IQueryable<SongModel> SongsByAlbumArtists(Realm realm, ObjectId albumId)
    {
        var res = realm.All<ArtistModel>().Filter("ANY Songs.Album.Id == $0", albumId)
                                   .Select(a => (QueryArgument)a.Id)
                                   .ToArray();
        return realm.All<SongModel>().Filter("Artist.Id IN { $values }", res);
    }
}
