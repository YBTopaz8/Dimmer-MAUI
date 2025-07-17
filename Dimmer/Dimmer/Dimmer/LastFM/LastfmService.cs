using Dimmer.Interfaces.Services.Interfaces;
using Dimmer.Utilities.Events;
using Dimmer.Utilities.StatsUtils;

using Hqub.Lastfm;
using Hqub.Lastfm.Cache;
using Hqub.Lastfm.Entities;

using Microsoft.Extensions.Options;

using ReactiveUI;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.LastFM;

public class LastfmService : ILastfmService
{
    private readonly IRealmFactory _realmFactory;
    private readonly LastfmClient _client;
    private readonly LastfmSettings _settings; 
    private readonly IDimmerAudioService _audioService; 
    private readonly ILogger<LastfmService> _logger;
    private readonly CompositeDisposable _disposables = new(); // To manage subscriptions

    private readonly IRepository<SongModel> _songRepo; 
    private readonly IRepository<DimmerPlayEvent> _playEventRepo; 

    private readonly BehaviorSubject<bool> _isAuthenticatedSubject;
    public IObservable<bool> IsAuthenticatedChanged => _isAuthenticatedSubject;

    // We must store the username manually, as the Session object does not.
    private string? _username;

    public bool IsAuthenticated => _client.Session.Authenticated;
    public string? AuthenticatedUser => IsAuthenticated ? _username : null;
    public string? AuthenticatedUserSessionToken => _client.Session.SessionKey;

    public LastfmService(IOptions<LastfmSettings> settingsOptions, IDimmerAudioService audioService, ILogger<LastfmService> logger,
        IRealmFactory realmFactory,
        IRepository<SongModel> songRepo,
        IRepository<DimmerPlayEvent> playEventRepo)
    {
        _realmFactory = realmFactory;
        _audioService = audioService;
        _songRepo = songRepo;
        _playEventRepo = playEventRepo;
        this._logger=logger;
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

        Observable.FromEventPattern<PlaybackEventArgs>(
             h => audioService.PlaybackStateChanged += h,
             h => audioService.PlaybackStateChanged -= h)
         .Select(evt => evt.EventArgs)
         .ObserveOn(RxApp.MainThreadScheduler)
         .Subscribe(HandlePlaybackStateChange, ex => _logger.LogError(ex, "Error in PlaybackStateChanged subscription"));
        Observable.FromEventPattern<PlaybackEventArgs>(
                h => audioService.PlayEnded += h,
                h => audioService.PlayEnded -= h)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(async _ => await OnPlaybackEnded(), ex => _logger.LogError(ex, "Error in PlayEnded subscription"));
    }

    private async Task OnPlaybackEnded()
    {
        if (_songToScrobble != null && IsAuthenticated)
        {
            await ScrobbleAsync(_songToScrobble);
            _songToScrobble = null; // Clear the state.
        }
    }

    private void HandlePlaybackStateChange(PlaybackEventArgs args)
    {
        PlayType? state = StatesMapper.Map(args.EventType); // Assuming you have a way to get the enum state

        switch (state)
        {
            case PlayType.Play:
                // This case might be handled by PlayEnded -> NextTrack -> StartAudioForSongAtIndex
                // which then calls InitializeAsync and Play. The audio service might raise
                // a 'Playing' state change at that point. We can simply log it.
                OnPlaybackStarted(args);
                break;

            case PlayType.Resume:
                OnPlaybackResumed(args);
                break;

            case PlayType.Pause:
                OnPlaybackPaused(args);
                break;
        } 
    }

    private async void OnPlaybackPaused(PlaybackEventArgs args)
    {
    }

    private async void OnPlaybackResumed(PlaybackEventArgs args)
    {


        await UpdateNowPlayingAsync(args.MediaSong);
    }

    private async void OnPlaybackStarted(PlaybackEventArgs args)
    {
        if (_songToScrobble != null && IsAuthenticated)
        {
            await ScrobbleAsync(_songToScrobble);
            _songToScrobble = null; // Clear the state.
            await UpdateNowPlayingAsync(args.MediaSong);
        }
    }

    private SongModelView? _songToScrobble; // Internal state



    // --- PRIVATE EVENT HANDLER & LOGIC ---


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
            await Shell.Current.DisplayAlert("Error", $"Failed to scrobble on Last.fm. {ex.Message}", "OK");

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
         var isUpdated=   await _client.Track.UpdateNowPlayingAsync(song.OtherArtistsName, song.Title, album: song.AlbumName);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Failed to update now playing on Last.fm. {ex.Message}", "OK");
            // Log error
        }
    }

    #endregion

    #region Data Retrieval

    public async Task<Artist> GetArtistInfoAsync(string artistName)
    {
        try
        { 
            return await _client.Artist.GetInfoAsync(artistName); 
        }
        catch 
        { 
            return new Artist(); 
        }
    }

    public async Task<Album> GetAlbumInfoAsync(string artistName, string albumName)
    {
        try
        { 
            return await _client.Album.GetInfoAsync(artistName, albumName); 
        }
        catch 
        { 
            return new Album(); 
        }
    }

    public async Task<Track> GetTrackInfoAsync(string artistName, string trackName)
    {
        try
        { 
          
            return await _client.Track.GetInfoAsync(artistName, trackName); 
        }
        catch 
        { 
            return new Track(); 
        }
    }


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
    public async Task<List<Track>> GetUserRecentTracksAsync(string username, int limit = 20)
    {
        if (string.IsNullOrEmpty(username))
            return new List<Track>();
        try
        {
            var pagedResponse = await _client.User.GetRecentTracksAsync(username, page: 1, limit: limit);
            return pagedResponse.Items.ToList();
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Failed to get recent tracks for user {User}", username); return new List<Track>(); }
    }

    public async Task<bool> LoveTrackAsync(SongModelView song)
    {
        if (!IsAuthenticated || song is null)
            return false;
        try
        {
            await _client.Track.LoveAsync(song.OtherArtistsName, song.Title);
            
            return true;
        }
        catch (Exception ex)
        {
            
            return false;
        }
    }

    public async Task<bool> UnloveTrackAsync(SongModelView song)
    {
        if (!IsAuthenticated || song is null)
            return false;
        try
        {
            await _client.Track.UnloveAsync(song.OtherArtistsName, song.Title);
            
            return true;
        }
        catch (Exception ex)
        {
            
            return false;
        }
    }

    public async Task<bool> EnrichSongMetadataAsync(ObjectId songId)
    {
        var realm = _realmFactory.GetRealmInstance();
        var song = realm.Find<SongModel>(songId);

        if (song is null || string.IsNullOrEmpty(song.ArtistName))
            return false;

        try
        {
            var trackInfo = await GetTrackInfoAsync(song.ArtistName, song.Title);
            if (trackInfo is null)
                return false;

            Album? albumInfo = trackInfo.Album != null ? await GetAlbumInfoAsync(trackInfo.Artist.Name, trackInfo.Album.Name) : null;

            bool hasChanges = false;
            realm.Write(() =>
            {
                var liveSong = realm.Find<SongModel>(songId);
                if (liveSong is null)
                    return;

                // Update Genre
                if (string.IsNullOrEmpty(liveSong.Genre?.Name) && trackInfo.Tags.Any())
                {
                    // Find or create genre
                    var topTag = trackInfo.Tags.First().Name;
                    var genre = realm.All<GenreModel>().FirstOrDefault(g => g.Name == topTag)
                                ?? new GenreModel { Name = topTag };
                    liveSong.Genre = genre;
                    hasChanges = true;
                }


                // You can add more enrichment here: Cover art URL, song duration, etc.
            });

            if (hasChanges)
                _logger.LogInformation("Enriched metadata for: {Title}", song.Title);
            return hasChanges;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enrich metadata for song ID {SongId}", songId);
            return false;
        }
    }


    public async Task<int> PullLastfmHistoryToLocalAsync(DateTimeOffset since)
    {
        if (!IsAuthenticated || _username is null)
            return 0;
        _logger.LogInformation("Starting to pull Last.fm history since {Date}", since);

        var recentTracks = new List<Track>();
        try
        {
            // Fetch multiple pages if needed, but start with one.
            var pagedResponse = await _client.User.GetRecentTracksAsync(_username, from: since.DateTime, page: 1, limit: 200);
            recentTracks.AddRange(pagedResponse.Items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch recent tracks from Last.fm.");
            return 0;
        }

        var newEvents = new List<DimmerPlayEvent>();
        foreach (var track in recentTracks.Where(t => t.Date.HasValue))
        {
            var song = _songRepo.Query(s => s.Title == track.Name && s.ArtistName == track.Artist.Name).FirstOrDefault();

            // Only add a play event if we know about the song locally.
            // A more advanced version could create stub song entries.
            if (song != null)
            {
                newEvents.Add(new DimmerPlayEvent
                {
                    SongId = song.Id,
                    SongName = song.Title,
                    PlayType = 3, // Completed
                    PlayTypeStr = "Completed",
                    EventDate = track.Date.Value,
                    DatePlayed = track.Date.Value,
                    WasPlayCompleted = true,
                });
            }
        }

        if (newEvents.Count > 0)
        {
            foreach (var item in newEvents)
            {
                _playEventRepo.Create(item);
            }
            _logger.LogInformation("Pulled and saved {Count} new play events from Last.fm.", newEvents.Count);
        }

        return newEvents.Count;
    }

}