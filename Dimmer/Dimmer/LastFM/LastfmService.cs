using Dimmer.Interfaces.Services.Interfaces;
using Dimmer.Utilities.Events;

using Hqub.Lastfm;
using Hqub.Lastfm.Cache;
using Hqub.Lastfm.Entities;

using Microsoft.Extensions.Options;

using ReactiveUI;

using System.Reactive.Disposables;

using Track = Hqub.Lastfm.Entities.Track;
namespace Dimmer.LastFM;

public class LastfmService : ILastfmService
{
    private readonly IRealmFactory _realmFactory;
    private readonly LastfmClient _client;
    private readonly LastfmSettings _settings; 
    private readonly IDimmerAudioService audioService; 
    private readonly ILogger<LastfmService> _logger;
    private readonly CompositeDisposable _disposables = new(); // To manage subscriptions

    private readonly IRepository<SongModel> _songRepo; 
    private readonly IRepository<DimmerPlayEvent> _playEventRepo; 

    private readonly BehaviorSubject<bool> _isAuthenticatedSubject;
    public IObservable<bool> IsAuthenticatedChanged => _isAuthenticatedSubject;

    // We must store the username manually, as the Session object does not.
    private string? _username;

    public bool IsAuthenticated => _client.Session.Authenticated;
    /// <summary>
    /// Gets the username of the authenticated user.
    /// </summary>
    public string? AuthenticatedUser => ((ILastfmService)this).IsAuthenticated ? _username : null;
    /// <summary>
    ///     
    /// Gets the session token for the authenticated user.
    /// </summary>
    public string? AuthenticatedUserSessionToken => _client.Session.SessionKey;

    public LastfmService(IOptions<LastfmSettings> settingsOptions, IDimmerAudioService _audioService, ILogger<LastfmService> logger,
        IRealmFactory realmFactory,
        IRepository<SongModel> songRepo,
        IRepository<DimmerPlayEvent> playEventRepo)
    {
        _realmFactory = realmFactory;
        this.audioService = _audioService;
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

       
    }

    public void Start()
    {
        if (_disposables.Count > 0)
            return; // Prevent multiple subscriptions

        _logger.LogInformation("Last.fm Service starting listeners...");

        // --- 1. Handle "Now Playing" and setting the scrobble candidate ---
        // We use PlaybackStateChanged because it fires for Play, Resume, etc.
        Observable.FromEventPattern<PlaybackEventArgs>(
            h => audioService.PlaybackStateChanged += h,
            h => audioService.PlaybackStateChanged -= h)
        .Select(evt => evt.EventArgs)
        .Where(_ => ((ILastfmService)this).IsAuthenticated)
        .ObserveOn(RxApp.TaskpoolScheduler)
        .Subscribe(HandlePlaybackStateChange, ex => _logger.LogError(ex, "Error in Last.fm PlaybackStateChanged subscription."))
        .DisposeWith(_disposables);

        Observable.FromEventPattern<PlaybackEventArgs>(
                    h => audioService.PlayEnded += h,
                    h => audioService.PlayEnded -= h)
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(async _ => await OnPlaybackEnded(), ex => _logger.LogError(ex, "Error in PlayEnded subscription"));

    }

    private async Task OnPlaybackEnded()
    {
        // This event is for when the *entire queue* finishes.
        // We need to scrobble the very last track.
        if (_songToScrobble != null)
        {
            await ((ILastfmService)this).ScrobbleAsync(_songToScrobble);
        }
        // The session is over, clear the candidate.
        _songToScrobble = null;
    }

    private async void HandlePlaybackStateChange(PlaybackEventArgs args)
    {
        var currentSong = args.MediaSong;

        switch (args.EventType)
        {
            case DimmerPlaybackState.Playing:
                if (currentSong is null)
                    return;
                _songToScrobble = currentSong;
               
                //if (_songToScrobble != null)
                //{
                //    await ((ILastfmService)this).ScrobbleAsync(_songToScrobble);
                //}

                await ((ILastfmService)this).UpdateNowPlayingAsync(currentSong);
                //await ((ILastfmService)this).EnrichSongMetadataAsync(currentSong.Id);
                break;

            case DimmerPlaybackState.Resumed:
                if (currentSong is null)
                    return;
                await ((ILastfmService)this).UpdateNowPlayingAsync(currentSong);
                break;

            case DimmerPlaybackState.PlayCompleted:
                break;

            // No Last.fm action needed for these states.
            case DimmerPlaybackState.Skipped: // Handled by the next 'Playing' state
            case DimmerPlaybackState.PausedDimmer:
            default:
                break;
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
        ((ILastfmService)this).Logout();
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

    public async Task ScrobbleAsync(SongModelView songV)
    {
        if (!((ILastfmService)this).IsAuthenticated || songV == null||songV.ArtistName is null )
            return;

        var artistName = songV.ArtistName.Split("| ", StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();


        // CORRECTED: Use parameterless constructor and set properties.
        var scrobble = new Scrobble
        {
            Artist = artistName,
            Track = songV.Title,
            Date = DateTime.UtcNow, // Timestamp is required for a scrobble
            Album = songV.AlbumName,
            
        };

        try
        {
            // The API takes a single scrobble or an IEnumerable<Scrobble>
            var result=await _client.Track.ScrobbleAsync(new[] { scrobble });
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
    public async Task<bool> CompleteAuthenticationAsync(string authenticatedUser)
    {
        try
        {
            // This calls the second method from your fork. It uses the internally stored token.
            await _client.AuthenticateViaWebAsync();

            if (_client.Session.Authenticated)
            {

                _username = authenticatedUser;

                // Now we save the session key AND the username.
                SaveSession();
                _isAuthenticatedSubject.OnNext(true);

                User userInfo = await ((ILastfmService)this).GetUserInfoAsync();
                Realm realmm = _realmFactory.GetRealmInstance()!;
                await realmm.WriteAsync(() =>
                {

                    var usrs = realmm.All<UserModel>().ToList();
                    if (usrs is null)
                    {
                        UserModel newUsr = new UserModel();
                        newUsr.UserName = userInfo.Name;
                        newUsr.LastFMAccountInfo=new();
                        newUsr.LastFMAccountInfo.Name=userInfo.Name;
                        newUsr.LastFMAccountInfo.RealName=userInfo.RealName;
                        newUsr.LastFMAccountInfo.Gender=userInfo.Gender;
                        newUsr.LastFMAccountInfo.Playcount=userInfo.Playcount;
                        newUsr.LastFMAccountInfo.Age=userInfo.Age;
                        newUsr.LastFMAccountInfo.Country=userInfo.Country;
                        newUsr.LastFMAccountInfo.Url=userInfo.Url;
                        newUsr.LastFMAccountInfo.Type=userInfo.Type;
                        newUsr.LastFMAccountInfo.Registered=userInfo.Registered;
                        newUsr.LastFMAccountInfo.Playlists = userInfo.Playlists;
                        realmm.Add(newUsr, update: true);   
                    }
                    else
                    {
                        UserModel newUsr = realmm.Find<UserModel>(usrs.FirstOrDefault()?.Id);
                        if (newUsr is not null)
                        {

                            newUsr.LastFMAccountInfo??=new();
                            newUsr.LastFMAccountInfo.Name=userInfo.Name;
                            newUsr.LastFMAccountInfo.RealName=userInfo.RealName;
                            newUsr.LastFMAccountInfo.Gender=userInfo.Gender;
                            newUsr.LastFMAccountInfo.Playcount=userInfo.Playcount;
                            newUsr.LastFMAccountInfo.Age=userInfo.Age;
                            newUsr.LastFMAccountInfo.Country=userInfo.Country;
                            newUsr.LastFMAccountInfo.Url=userInfo.Url;
                            newUsr.LastFMAccountInfo.Type=userInfo.Type;
                            newUsr.LastFMAccountInfo.Registered=userInfo.Registered;
                            newUsr.LastFMAccountInfo.Playlists = userInfo.Playlists;

                            newUsr.LastFMAccountInfo.Image =new LastFMUser.LastImage();
                            newUsr.LastFMAccountInfo.Image.Url=userInfo.Images.LastOrDefault().Url;
                            newUsr.LastFMAccountInfo.Image.Size=userInfo.Images.LastOrDefault().Size;

                        }
                        else
                        {
                            newUsr = new UserModel();
                            newUsr.LastFMAccountInfo=new();
                            newUsr.Id=new();
                            newUsr.UserName= userInfo.Name;
                            newUsr.LastFMAccountInfo.Name = userInfo.Name;
                            newUsr.LastFMAccountInfo.RealName = userInfo.RealName;
                            newUsr.LastFMAccountInfo.Url = userInfo.Url;
                            newUsr.LastFMAccountInfo.Country = userInfo.Country;
                            newUsr.LastFMAccountInfo.Age = userInfo.Age;
                            newUsr.LastFMAccountInfo.Playcount= userInfo.Playcount;
                            newUsr.LastFMAccountInfo.Playlists = userInfo.Playlists;
                            newUsr.LastFMAccountInfo.Registered = userInfo.Registered;
                            newUsr.LastFMAccountInfo.Gender = userInfo.Gender;
                            newUsr.LastFMAccountInfo.Image =new LastFMUser.LastImage();
                            newUsr.LastFMAccountInfo.Image.Url=userInfo.Images.LastOrDefault().Url;
                            newUsr.LastFMAccountInfo.Image.Size=userInfo.Images.LastOrDefault().Size;

                            realmm.Add(newUsr, update: true);
                        }
                    }
                });
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.Log(logLevel: LogLevel.Error, ex, ex.Message);

        }
        return false;
    }

    public async Task<User> GetUserInfoAsync()
    {
        if (((ILastfmService)this).AuthenticatedUser is null)
        {
            LoadSession();
            if (((ILastfmService)this).AuthenticatedUser is null)
            {
                return new User("Unknown");
            }

          return  await _client.User.GetInfoAsync(((ILastfmService)this).AuthenticatedUser);
        }


        return  await _client.User.GetInfoAsync(((ILastfmService)this).AuthenticatedUser);
        // Gets info for the NOW authenticated user.
    }

    #endregion
    public async Task UpdateNowPlayingAsync(SongModelView song)
    {
        if (!((ILastfmService)this).IsAuthenticated || song == null)
            return;

        try
        {
            // i can have songs like "artst | artst2 | artst3 - title"
            // We need to split the artist names correctly.
            // and get only the first artist name.
            // This is a workaround for the fact that Last.fm API does not handle multiple artists in the same way.
            if (string.IsNullOrEmpty(song.ArtistName))
            {
                await Shell.Current.DisplayAlert("Error", "Artist name is empty. Cannot update now playing.", "OK");
                return;
            }
            
            var artistName = song.ArtistName.Split("| ", StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

         var isUpdated=   await _client.Track.UpdateNowPlayingAsync(song.Title, artistName, album: song.AlbumName
             );
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

    public async Task<ObservableCollection<Track>> GetTopCountryTracksAsync(string country)
    {
        try
        { 
            var res = await _client.Geo.GetTopTracksAsync(country);
            return res.ToObservableCollection();
        }
        catch 
        {
            return new ObservableCollection<Track>();
        }
    }

    public async Task<ObservableCollection<Artist>> GetTopCountryArtistAsync(string country)
    {
        try
        { 
            var res = await _client.Geo.GetTopArtistsAsync(country);
            return res.ToObservableCollection();
        }
        catch 
        {
            return new ObservableCollection<Artist>();
        }
    }

    public async Task<ObservableCollection<Artist>> GetUserLibArtistsAsync(string country)
    {
        try
        {
            var res = await _client.Library.GetArtistsAsync(_username, 250);
            return res.ToObservableCollection();
        }
        catch 
        {
            return new ObservableCollection<Artist>();
        }
    }

    public async Task<Track> GetTrackInfoAsync(string artistName, string trackName)
    {
        try
        { 
          
            return await _client.Track.GetInfoAsync(trackName, artistName); 
        }
        catch 
        { 
            return new Track(); 
        }
    }


    public async Task<Track> GetCorrectionAsync(string artistName, string trackName)
    {
        try
        { 
          
            return await _client.Track.GetCorrectionAsync(trackName, artistName); 
        }
        catch 
        { 
            return new Track(); 
        }
    }

    public async Task<ObservableCollection<Track>> GetSimilarAsync(string artistName, string trackName)
    {
        try
        { 
          
            var res= await _client.Track.GetSimilarAsync(trackName, artistName,40,true);
            return res.ToObservableCollection();
        }
        catch 
        { 
            return Enumerable.Empty<Track>().ToObservableCollection();
        }
    }

    public async Task<ObservableCollection<Tag>> GetTagsAsync(string artistName, string trackName)
    {
        try
        { 
          
            var res= await _client.Track.GetTagsAsync(_username, trackName, artistName); 
            return res.ToObservableCollection(); 
        }
        catch 
        { 
            return Enumerable.Empty<Tag>().ToObservableCollection();
        }
    }

    public async Task<ObservableCollection<Track>> GetLovedTracksAsync()
    {
        try
        {

            var res = await _client.User.GetLovedTracksAsync(_username);
            return res.ToObservableCollection();
        }
        catch 
        {
            return new ObservableCollection<Track>();
        }
    }

    public async Task<ObservableCollection<Album>> GetUserWeeklyAlbumChartAsync( )
    {
        try
        {

            var res= await _client.User.GetWeeklyAlbumChartAsync(_username);
            return res.ToObservableCollection();
        }
        catch
        {
            return new ObservableCollection<Album>();
        }
    }

    public async Task<ObservableCollection<ChartTimeSpan>> GetWeeklyUserChartListAsync( )
    {
        try
        {

            var res  = await _client.User.GetWeeklyChartListAsync(_username);
            return res.ToObservableCollection();
        }
        catch 
        {
            return new ObservableCollection<ChartTimeSpan>();
        }
    }

    public async Task<ObservableCollection<Album>> GetTopUserAlbumsAsync()
    {
        try
        {

            var res = await _client.User.GetTopAlbumsAsync(_username);
            return res.ToObservableCollection();
        }
        catch 
        {
            return new ObservableCollection<Album>();
        }
    }

    public async Task<ObservableCollection<Track>> GetUserTopTracksAsync( )
    {
        try
        {

            var res= await _client.User.GetTopTracksAsync(_username);
            return res.ToObservableCollection();
        }
        catch 
        {
            return new ObservableCollection<Track>();
        }
    }

    public async Task<ObservableCollection<Track>> GetUserWeeklyTrackChartAsync( )
    {
        try
        {

            var res = await _client.User.GetWeeklyTrackChartAsync(_username);
            return res.ToObservableCollection();
        }
        catch 
        {
            return new ObservableCollection<Track>();
        }
    }


    public async Task<ObservableCollection<Artist>> GetTopArtistsChartAsync(int limit)
    {
        try
        {
            // The method returns a ObservableCollection<Artist>, we need the Items.
            var pagedResponse = await _client.Chart.GetTopArtistsAsync(1, limit);
            return pagedResponse.Items.ToObservableCollection();
        }
        catch
        {
            return new ObservableCollection<Artist>();
        }
    }

    #endregion
    public async Task<ObservableCollection<Track>> GetUserRecentTracksAsync(string username, int limit)
    {
        if (string.IsNullOrEmpty(username))
            return new ObservableCollection<Track>();
        try
        {
            var pagedResponse = await _client.User.GetRecentTracksAsync(username, page: 1, limit: limit);
            return pagedResponse.Items.ToObservableCollection();
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Failed to get recent tracks for user {User}", username); return new ObservableCollection<Track>(); }
    }

    public async Task<bool> LoveTrackAsync(SongModelView song)
    {
        if (!((ILastfmService)this).IsAuthenticated || song is null)
            return false;
        try
        {


            var artistName = song.ArtistName.Split("| ", StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

            var isUpdated = await _client.Track.LoveAsync(song.Title, artistName);
            
            return true;
        }
        catch (Exception ex)
        {
            
            return false;
        }
    }

    public async Task<bool> UnloveTrackAsync(SongModelView song)
    {
        if (!((ILastfmService)this).IsAuthenticated || song is null)
            return false;
        try
        {

            var artistName = song.ArtistName.Split("| ", StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

            var isUpdated = await _client.Track.UnloveAsync(song.Title, artistName);

            
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
            var trackInfo = await ((ILastfmService)this).GetTrackInfoAsync(song.ArtistName, song.Title);
            if (trackInfo is null || trackInfo.IsNull ||trackInfo.Duration==0)
                return false;

            Album? albumInfo = trackInfo.Album != null ? await ((ILastfmService)this).GetAlbumInfoAsync(trackInfo.Artist.Name, trackInfo.Album.Name) : null;
            if (albumInfo is null)
            {
                return false;
            }
            bool hasChanges = false;
            realm.Write(() =>
            {
                var liveSong = realm.Find<SongModel>(songId);
                if (liveSong is null)
                    return;

                // Update Genre
                if (string.IsNullOrEmpty(liveSong.Genre?.Name) && trackInfo.Tags.Count!=0)
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
        if (!((ILastfmService)this).IsAuthenticated || _username is null)
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

        var newEvents = new ObservableCollection<DimmerPlayEvent>();
        foreach (var track in recentTracks.Where(t => t.Date.HasValue))
        {
            var song = _songRepo.Query(s => s.Title == track.Name && s.DurationInSeconds == (track.Duration/1000)).FirstOrDefault();

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