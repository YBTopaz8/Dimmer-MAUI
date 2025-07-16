using Hqub.Lastfm;
using Hqub.Lastfm.Cache;
using Hqub.Lastfm.Entities;

using Microsoft.Extensions.Options;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.LastFM;

public class LastfmService : ILastfmService
{
    private readonly LastfmClient _client;
    private readonly LastfmSettings _settings;

    private readonly BehaviorSubject<bool> _isAuthenticatedSubject;
    public IObservable<bool> IsAuthenticatedChanged => _isAuthenticatedSubject;

    // We must store the username manually, as the Session object does not.
    private string? _username;

    public bool IsAuthenticated => _client.Session.Authenticated;
    public string? AuthenticatedUser => IsAuthenticated ? _username : null;
    public string? AuthenticatedUserSessionToken => _client.Session.SessionKey;

    public LastfmService(IOptions<LastfmSettings> settingsOptions)
    {
        _settings = settingsOptions.Value;
        if (string.IsNullOrEmpty(_settings.ApiKey)) // Only ApiKey is strictly needed for public requests
        {
            throw new InvalidOperationException("Last.fm API Key must be configured in appsettings.json");
        }

        var cacheLocation = Path.Combine(FileSystem.AppDataDirectory, "lastfm_cache");
        Directory.CreateDirectory(cacheLocation);

        _client = new LastfmClient(_settings.ApiKey, _settings.ApiSecret)
        {
            Cache = new FileRequestCache(cacheLocation)
        };

        _isAuthenticatedSubject = new BehaviorSubject<bool>(false);

        LoadSession();
    }

    #region Authentication

    public async Task<bool> LoginAsync(string username, string password)
    {
        try
        {
            // The AuthenticateAsync method takes user/pass and handles the session internally.
            await _client.AuthenticateAsync(username, password);

            if (_client.Session.Authenticated)
            {
                // Store the username since we succeeded
                _username = username;
                SaveSession();
                _isAuthenticatedSubject.OnNext(true);
                return true;
            }
        }
        catch (Exception ex)
        {
            // Log the exception (e.g., invalid credentials)
        }

        // Ensure state is clean on failure
        Logout();
        return false;
    }

    public void Logout()
    {
        // To log out, we clear our stored credentials and the client's session key.
        _client.Session.SessionKey = null;
        _username = null;
        ClearSession();
        _isAuthenticatedSubject.OnNext(false);
    }

    private void SaveSession()
    {
        Preferences.Set("LastfmSessionKey", _client.Session.SessionKey);
        Preferences.Set("LastfmUsername", _username);
    }

    private void LoadSession()
    {
        var sessionKey = Preferences.Get("LastfmSessionKey", string.Empty);
        var username = Preferences.Get("LastfmUsername", string.Empty);

        if (!string.IsNullOrEmpty(sessionKey) && !string.IsNullOrEmpty(username))
        {
            _client.Session.SessionKey = sessionKey;
            _username = username;
            _isAuthenticatedSubject.OnNext(true);
        }
    }

    private void ClearSession()
    {
        Preferences.Remove("LastfmSessionKey");
        Preferences.Remove("LastfmUsername");
    }

    #endregion

    #region Core Features

    public async Task ScrobbleAsync(SongModelView song)
    {
        if (!IsAuthenticated || song == null)
            return;

        // CORRECTED: Use parameterless constructor and set properties.
        var scrobble = new Scrobble
        {
            Artist = song.OtherArtistsName,
            Track = song.Title,
            Date = DateTime.UtcNow, // Timestamp is required for a scrobble
            Album = song.AlbumName,
            AlbumArtist = song.OtherArtistsName
        };

        try
        {
            // The API takes a single scrobble or an IEnumerable<Scrobble>
            var response = await _client.Track.ScrobbleAsync(new[] { scrobble });
            // Log response.Accepted/Ignored if needed
        }
        catch (Exception ex)
        {
            // Log error
        }
    }
    #region Recommended Web Authentication

    /// <summary>
    /// Step 1: Gets a URL for the user to visit to authorize the app.
    /// The temporary token is managed internally by the LastfmClient instance.
    /// </summary>
    public async Task<string> GetAuthenticationUrlAsync()
    {
        return await _client.GetWebAuthenticationUrlAsync();
    }

    /// <summary>
    /// Step 2: After the user authorizes on the website, this completes the authentication.
    /// </summary>
    public async Task<bool> CompleteAuthenticationAsync()
    {
        try
        {
            // This calls the second method from your fork. It uses the internally stored token.
            await _client.AuthenticateViaWebAsync();

            if (_client.Session.Authenticated)
            {
                // CRITICAL STEP: After getting the session key, we must find out the username.
                // The most reliable way is to make a quick call to user.getInfo.
                var userInfo = await _client.User.GetInfoAsync(); // Gets info for the NOW authenticated user.
                _username = userInfo.Name;

                // Now we save the session key AND the username.
                SaveSession();
                _isAuthenticatedSubject.OnNext(true);
                return true;
            }
        }
        catch (Exception ex)
        {
            // Log the error
        }
        return false;
    }

    #endregion
    public async Task UpdateNowPlayingAsync(SongModelView song)
    {
        if (!IsAuthenticated || song == null)
            return;

        try
        {
            await _client.Track.UpdateNowPlayingAsync(song.OtherArtistsName, song.Title, album: song.AlbumName);
        }
        catch (Exception ex)
        {
            // Log error
        }
    }

    #endregion

    #region Data Retrieval

    public async Task<Artist?> GetArtistInfoAsync(string artistName)
    {
        try
        { return await _client.Artist.GetInfoAsync(artistName); }
        catch { return null; }
    }

    public async Task<Album?> GetAlbumInfoAsync(string artistName, string albumName)
    {
        try
        { return await _client.Album.GetInfoAsync(artistName, albumName); }
        catch { return null; }
    }

    public async Task<Track?> GetTrackInfoAsync(string artistName, string trackName)
    {
        try
        { return await _client.Track.GetInfoAsync(artistName, trackName); }
        catch { return null; }
    }

    // CORRECTED: The method returns a List<Artist>, not a Chart object.
    public async Task<List<Artist>> GetTopArtistsChartAsync(int limit = 20)
    {
        try
        {
            // The method returns a PagedResponse<Artist>, we need the Items.
            var pagedResponse = await _client.Chart.GetTopArtistsAsync(1, limit);
            return pagedResponse.Items.ToList();
        }
        catch
        {
            return new List<Artist>();
        }
    }

    #endregion
}