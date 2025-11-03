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
        .Subscribe(async x =>
        {
            await HandlePlaybackStateChange(x);
        }, ex => _logger.LogError(ex, "Error in Last.fm PlaybackStateChanged subscription."))
        .DisposeWith(_disposables);

        Observable.FromEventPattern<PlaybackEventArgs>(
                    h => audioService.PlayEnded += h,
                    h => audioService.PlayEnded -= h)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(async _ => await OnPlaybackEnded(), ex => _logger.LogError(ex, "Error in PlayEnded subscription"))
                .DisposeWith(_disposables);

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

    private async Task HandlePlaybackStateChange(PlaybackEventArgs args)
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
            Debug.WriteLine(ex.Message);
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

    public void LoadSession()
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
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return ;
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
            //await Shell.Current.DisplayAlert("Error", $"Failed to scrobble on Last.fm. {ex.Message}", "OK");
            Debug.WriteLine(ex);
            // Log error
        }
    }
    #region Recommended Web Authentication

    /// <summary>
    /// Step 1: Gets a URL for the user to visit to authorize the app.
    /// The temporary token is managed internally by the LastfmClient instance.
    /// </summary>
    public async Task<string?> GetAuthenticationUrlAsync()
    {
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return null;
        return await _client.GetWebAuthenticationUrlAsync();
    }

    /// <summary>
    /// Step 2: After the user authorizes on the website, this completes the authentication.
    /// </summary>
    /// 
    
    public async Task<bool> CompleteAuthenticationAsync(string authenticatedUser)
    {
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return false;
        try
        {
            if (_client.Session.Authenticated)
            {
                return true; // Already authenticated
            }

                // This calls the second method from your fork. It uses the internally stored token.
            if (!await _client.AuthenticateViaWebAsync())
            {
                return false;
            }
            if (_client.Session.Authenticated)
            {

                _username = authenticatedUser;

                // Now we save the session key AND the username.
                SaveSession();
                _isAuthenticatedSubject.OnNext(true);

                User? userInfo = await ((ILastfmService)this).GetUserInfoAsync();
                if (userInfo == null) return false;
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
                        UserModel? newUsr = realmm.Find<UserModel>(usrs.FirstOrDefault()?.Id);
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
                            newUsr.LastFMAccountInfo.Image.Url=userInfo.Images.LastOrDefault()?.Url;
                            newUsr.LastFMAccountInfo.Image.Size=userInfo.Images.LastOrDefault()?.Size;

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
                            newUsr.LastFMAccountInfo.Image.Url=userInfo.Images.LastOrDefault()?.Url;
                            newUsr.LastFMAccountInfo.Image.Size=userInfo.Images.LastOrDefault()?.Size;

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

    public async Task<User?> GetUserInfoAsync()
    {

        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return null;
        try
        {
            if (((ILastfmService)this).AuthenticatedUser is null)
            {
                LoadSession();
                if (((ILastfmService)this).AuthenticatedUser is null)
                {
                    return new User("Unknown");
                }

                return await _client.User.GetInfoAsync(((ILastfmService)this).AuthenticatedUser);
            }


            return await _client.User.GetInfoAsync(((ILastfmService)this).AuthenticatedUser);
        }
        catch (Exception ex) 
        {
            Debug.WriteLine(ex.Message);
            _isAuthenticatedSubject.OnNext(false);
            if(ex.Message == "Invalid session key - Please re-authenticate")
            {
                ClearSession();
            }
            return null;
        }
        // Gets info for the NOW authenticated user.
    }

    #endregion
    public async Task UpdateNowPlayingAsync(SongModelView song)
    {
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return;
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
                return;
            }
            
            var artistName = song.ArtistName.Split("| ", StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

         var isUpdated=   await _client.Track.UpdateNowPlayingAsync(song.Title, artistName, album: song.AlbumName
             );
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error Failed to update now playing on Last.fm. {ex.Message}");
            // Log error
        }
    }

    #endregion

    #region Data Retrieval

    public async Task<string?> GetMaxResArtistImageLink(string artistName)
    {
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return null;
        try
        {
            var temp = await GetArtistInfoAsync(artistName);
            var ImagePath = temp?.Images.Where(x=>x.Size == "mega").LastOrDefault()?.Url;
            return ImagePath ?? string.Empty;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            return string.Empty;
        }
    }

    public async Task<string?> GetMaxResAlbumImageLink(string albumName,string artistName)
    {
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return null;
        try
        {
            
            var temp = await GetAlbumInfoAsync(artistName,albumName);
            var ImagePath = temp?.Images.LastOrDefault()?.Url;
            return ImagePath ?? string.Empty;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            return string.Empty;
        }
    }

    public async Task<string?> GetMaxResTrackImageLink(string artistName, string trackName)
    {
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return null;
        try
        {
            var temp = await GetTrackInfoAsync(artistName, trackName);
           
            var ImagePath = temp?.Images.LastOrDefault()?.Url;
            return ImagePath ?? string.Empty;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            return string.Empty;
        }
    }

    public async Task<Artist?> GetArtistInfoAsync(string artistName)
    {
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return null;
        try
        { 
            return await _client.Artist.GetInfoAsync(artistName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, ex);
            return new Artist(); 
        }
    }

    public async Task<Album?> GetAlbumInfoAsync(string artistName, string albumName)
    {
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return null;
        try
        { 
            if (string.IsNullOrEmpty(albumName) && string.IsNullOrEmpty(artistName))
            {
                return new Album() { IsNull = true };
            }
            return await _client.Album.GetInfoAsync(artistName, albumName); 
        }
        catch  (Exception ex)
        {
            _logger.LogError(ex.Message, ex);
            return new Album() { IsNull = true };
        }
    }

    public async Task<ObservableCollection<Track>?> GetTopCountryTracksAsync(string country)
    {
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return null;
        try
        { 
            var res = await _client.Geo.GetTopTracksAsync(country);
            return res.ToObservableCollection();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, ex);

            return Enumerable.Empty<Track>().ToObservableCollection();
        }
    }

    public async Task<ObservableCollection<Artist>?> GetTopCountryArtistAsync(string country)
    {
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return null;
        try
        { 
            var res = await _client.Geo.GetTopArtistsAsync(country);
            return res.ToObservableCollection();
        }
        catch  (Exception ex)
        {
            _logger.LogError(ex.Message, ex);
            return new ObservableCollection<Artist>();
        }
    }

    public async Task<ObservableCollection<Artist>?> GetUserLibArtistsAsync(string country)
    {
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return null;
        try
        {
            var res = await _client.Library.GetArtistsAsync(_username, 250);
            return res.ToObservableCollection();
        }
        catch  (Exception ex)
        {
            _logger.LogError(ex.Message, ex);
            return new ObservableCollection<Artist>();
        }
    }

    public async Task<Track> GetTrackInfoAsync(string artistName, string trackName)
    {
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return null;
        try
        {

            return await _client.Track.GetInfoAsync(trackName, artistName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, ex);
            return new Track() { IsNull = true }; 
        }
    }


    public async Task<Track?> GetCorrectionAsync(string artistName, string trackName)
    {
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return null;
        try
        { 
          
            return await _client.Track.GetCorrectionAsync(trackName, artistName); 
        }
        catch  (Exception ex)
        {
            _logger.LogError(ex.Message, ex);
            return new Track() { IsNull = true };
        }
    }

    public async Task<ObservableCollection<Track>?> GetSimilarAsync(string artistName, string trackName)
    {
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return null;
        try
        { 
          
            var res= await _client.Track.GetSimilarAsync(trackName, artistName,40,true);
            return res.ToObservableCollection();
        }
        catch  (Exception ex)
        {
            _logger.LogError(ex.Message, ex); 
            return Enumerable.Empty<Track>().ToObservableCollection();
        }
    }

    public async Task<ObservableCollection<Tag>?> GetTagsAsync(string artistName, string trackName)
    {
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return null;
        try
        { 
          
            var res= await _client.Track.GetTagsAsync(_username, trackName, artistName); 
            return res.ToObservableCollection(); 
        }
        catch  (Exception ex)
        {
            _logger.LogError(ex.Message, ex); 
            return Enumerable.Empty<Tag>().ToObservableCollection();
        }
    }

    public async Task<ObservableCollection<Track>?> GetLovedTracksAsync()
    {
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return null;
        try
        {

            var res = await _client.User.GetLovedTracksAsync(_username);
            return res.ToObservableCollection();
        }
        catch  (Exception ex)
        {
            _logger.LogError(ex.Message, ex);
            return Enumerable.Empty<Track>().ToObservableCollection();
        }
    }

    public async Task<ObservableCollection<Album>?> GetUserWeeklyAlbumChartAsync( )
    {
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return null;
        try
        {

            var res= await _client.User.GetWeeklyAlbumChartAsync(_username);
            return res.ToObservableCollection();
        }
        catch  (Exception ex)
        {
            _logger.LogError(ex.Message, ex);
            return Enumerable.Empty<Album>().ToObservableCollection();
        }
    }

    public async Task<ObservableCollection<ChartTimeSpan>?> GetWeeklyUserChartListAsync( )
    {
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return null;
        try
        {

            var res  = await _client.User.GetWeeklyChartListAsync(_username);
            return res.ToObservableCollection();
        }
        catch  (Exception ex)
        {
            _logger.LogError(ex.Message, ex);
            return new ObservableCollection<ChartTimeSpan>();
        }
    }

    public async Task<ObservableCollection<Album>?> GetTopUserAlbumsAsync()
    {
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return null;
        try
        {

            var res = await _client.User.GetTopAlbumsAsync(_username);
            return res.ToObservableCollection();
        }
        catch  (Exception ex)
        {
            _logger.LogError(ex.Message, ex);
            return new ObservableCollection<Album>();
        }
    }

    public async Task<ObservableCollection<Track>?> GetUserTopTracksAsync( )
    {
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return null;
        try
        {

            var res= await _client.User.GetTopTracksAsync(_username);
            return res.ToObservableCollection();
        }
        catch  (Exception ex)
        {
            _logger.LogError(ex.Message, ex);
            
            return Enumerable.Empty<Track>().ToObservableCollection();
        }
    }

    public async Task<ObservableCollection<Track>?> GetUserWeeklyTrackChartAsync( )
    {
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return null;
        try
        {

            var res = await _client.User.GetWeeklyTrackChartAsync(_username);
            return res.ToObservableCollection();
        }
        catch  (Exception ex)
        {
            _logger.LogError(ex.Message, ex);

            return Enumerable.Empty<Track>().ToObservableCollection();
        }
    }


    public async Task<ObservableCollection<Artist>?> GetTopArtistsChartAsync(int limit)
    {
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return null;
        try
        {
            // The method returns a ObservableCollection<Artist>, we need the Items.
            var pagedResponse = await _client.Chart.GetTopArtistsAsync(1, limit);
            return pagedResponse.Items.ToObservableCollection();
        }
        catch  (Exception ex)
        {
            _logger.LogError(ex.Message, ex);
            return new ObservableCollection<Artist>();
        }
    }

    #endregion
    public async Task<ObservableCollection<Track>?> GetUserRecentTracksAsync(string? username, int limit)
    {
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return null;
        if (string.IsNullOrEmpty(username))
            
            return Enumerable.Empty<Track>().ToObservableCollection();
        try
        {
            var pagedResponse = await _client.User.GetRecentTracksAsync(username, page: 1, limit: limit);
            return pagedResponse.Items.ToObservableCollection();
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Failed to get recent tracks for user {User}", username); 
            return Enumerable.Empty<Track>().ToObservableCollection(); }
    }

    public async Task<bool> LoveTrackAsync(SongModelView song)
    {
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return false;
        if (!((ILastfmService)this).IsAuthenticated || song is null || song.Id == ObjectId.Empty)
            return false;
        try
        {


            var artistName = song.ArtistName.Split("| ", StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

            var isUpdated = await _client.Track.LoveAsync(song.Title, artistName);
            
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            return false;
        }
    }

    public async Task<bool> UnloveTrackAsync(SongModelView song)
    {
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return false;
        if (!((ILastfmService)this).IsAuthenticated || song is null || song.Id == ObjectId.Empty)
            return false;
        try
        {

            var artistName = song.ArtistName.Split("| ", StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

            var isUpdated = await _client.Track.UnloveAsync(song.Title, artistName);

            
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            return false;
        }
    }

    public async Task<bool> EnrichSongMetadataAsync(ObjectId songId)
    {
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return false;
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
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return 0;
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
                var evt = new DimmerPlayEvent
                {
                    SongId = song.Id,
                    SongName = song.Title,
                    PlayType = 3, // Completed
                    PlayTypeStr = "Completed",

                    WasPlayCompleted = true,
                };
                if(track.Date is not null)
                {

                    evt.EventDate = track.Date.Value;
                }
                if(track.Date is not null)
                {

                    evt.DatePlayed = track.Date.Value;
                }
                newEvents.Add(evt);
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