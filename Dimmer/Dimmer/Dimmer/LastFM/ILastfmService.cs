using Hqub.Lastfm.Entities;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.LastFM;
public interface ILastfmService
{
    // --- State Management ---
    bool IsAuthenticated { get; }
    IObservable<bool> IsAuthenticatedChanged { get; }
    string? AuthenticatedUser { get; }
    string? AuthenticatedUserSessionToken { get; }
    Task<string> GetAuthenticationUrlAsync(); // Returns only the URL
    Task<bool> CompleteAuthenticationAsync();


    // --- Data Retrieval ---
    Task<Track?> GetTrackInfoAsync(string artistName, string trackName);
    Task<Album?> GetAlbumInfoAsync(string artistName, string albumName);
    Task<Artist?> GetArtistInfoAsync(string artistName);
    Task<List<Artist>> GetTopArtistsChartAsync(int limit = 20);
    Task<List<Track>> GetUserRecentTracksAsync(string username, int limit = 20);

    Task<bool> LoveTrackAsync(SongModelView song);
    Task<bool> UnloveTrackAsync(SongModelView song);
    // --- Authentication ---
    // Note: The library seems to only support direct password auth, not token auth.
    // If you need token auth, a different library or direct HTTP calls might be needed.
    // For now, we'll stick to what this library provides.
    Task<bool> LoginAsync(string username, string password);
    void Logout();

    // --- Core Features ---
    Task ScrobbleAsync(SongModelView song);
    Task UpdateNowPlayingAsync(SongModelView song);

    /// <summary>
    /// Fetches metadata for a single song from Last.fm and updates the local database.
    /// </summary>
    /// <param name="songId">The ObjectId of the song in your Realm database.</param>
    /// <returns>True if any data was updated.</returns>
    Task<bool> EnrichSongMetadataAsync(ObjectId songId);



    /// <summary>
    /// Fetches your Last.fm play history and imports any missing songs into your app.
    /// </summary>
    /// <param name="since">Only import scrobbles that happened after this date.</param>
    /// <returns>The number of new play events added to your database.</returns>
    Task<int> PullLastfmHistoryToLocalAsync(DateTimeOffset since);
}

