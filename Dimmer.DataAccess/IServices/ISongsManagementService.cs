

namespace Dimmer.DataAccess.IServices;
public interface ISongsManagementService
{
    IList<SongsModelView> AllSongs { get; set; }
    Task<bool> AddSongAsync(SongsModel song);
    Task<bool> AddSongBatchAsync(IEnumerable<SongsModelView> song);
    Task<bool> AddArtistsBatchAsync(IEnumerable<ArtistModelView> artists);
    //public Task UpdateSongAsync(SongsModel song);
    //public Task DeleteSongAsync(SongsModel song);
    void GetSongs();
    Task<SongsModel> FindSongsByTitleAsync(string searchText);

    Task<bool> UpdateSongDetailsAsync(SongsModelView songsModelView);
    void Dispose();
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
