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


    // --- Authentication ---
    // Note: The library seems to only support direct password auth, not token auth.
    // If you need token auth, a different library or direct HTTP calls might be needed.
    // For now, we'll stick to what this library provides.
    Task<bool> LoginAsync(string username, string password);
    void Logout();

    // --- Core Features ---
    Task ScrobbleAsync(SongModelView song);
    Task UpdateNowPlayingAsync(SongModelView song);

    // --- Data Retrieval ---
    Task<Artist> GetArtistInfoAsync(string artistName);
    Task<Album> GetAlbumInfoAsync(string artistName, string albumName);
    Task<Track> GetTrackInfoAsync(string artistName, string trackName);
    Task<List<Artist>> GetTopArtistsChartAsync(int limit = 20);
}

