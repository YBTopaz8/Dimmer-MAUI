﻿namespace Dimmer_MAUI.DataAccess.IServices;
public interface ISongsManagementService
{
    IList<SongsModelView> AllSongs { get; set; }
    Task<bool> AddSongAsync(SongsModel song);
    bool AddSongBatchAsync(IEnumerable<SongsModelView> song);
    Task<bool> AddArtistsBatchAsync(IEnumerable<ArtistModelView> artists);
    //public Task UpdateSongAsync(SongsModel song);
    //public Task DeleteSongAsync(SongsModel song);
    void GetSongs();
    bool UpdateSongDetails(SongsModelView songsModelView);
    void Dispose();

    IList<AlbumModelView> AllAlbums { get; set; }
    void GetAlbums();
    (ObjectId artistID, ObjectId albumID) GetArtistAndAlbumIdFromSongId(ObjectId songId);
    IList<AlbumModelView> GetAlbumsFromArtistOrSongID(ObjectId artistOrSongId, bool fromSong = false);
    IList<ObjectId> GetSongsIDsFromAlbumID(ObjectId albumID);
    IList<ObjectId> GetSongsIDsFromArtistID(ObjectId artistID);
    void UpdateAlbum(AlbumModelView album);
    public int GetSongsCountFromAlbumID(ObjectId albumID);
    //public Task<SongsModel> FindSongsByArtist(string searchText);
    //public Task<SongsModel> FindSongsByAlbum(string searchText);
    //public Task<SongsModel> FindSongsByGenre(string searchText);
    //public Task<SongsModel> FindSongsByYear(string searchText);
    //public Task<SongsModel> FindSongsByDuration(string searchText);
    //public Task<SongsModel> FindSongsByLyrics(string searchText);
    //public Task<SongsModel> FindSongsByPath(string searchText);
    //public Task<SongsModel> FindSongsByRating(string searchText);
    //public Task<SongsModel> FindSongsByDateAdded(string searchText);
    //public Task<SongsModel> FindSongsByLastPlayed(string searchText);
    //public Task<SongsModel> FindSongsByPlayCount(string searchText);
    //public Task<SongsModel> FindSongsBySkipCount(string searchText);


}
