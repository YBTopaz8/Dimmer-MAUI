namespace Dimmer.Interfaces.Services;

using ATL;

using Microsoft.Extensions.Logging;

using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Web; // Needed for HttpUtility


#region API Data Transfer Objects (DTOs)

// Represents the full lyrics object returned by /get and /search

public class LrcLibLyrics
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("trackName")]
    public string TrackName { get; set; } = string.Empty;

    [JsonPropertyName("artistName")]
    public string ArtistName { get; set; } = string.Empty;

    [JsonPropertyName("albumName")]
    public string AlbumName { get; set; } = string.Empty;

    [JsonPropertyName("duration")]
    public double Duration { get; set; } // <--- CRITICAL FIX: int -> double

    [JsonPropertyName("instrumental")]
    public bool Instrumental { get; set; }

    [JsonPropertyName("plainLyrics")]
    public string? PlainLyrics { get; set; }

    [JsonPropertyName("syncedLyrics")]
    public string? SyncedLyrics { get; set; }
}

// Represents the body for a POST request to /publish
public class LrcLibPublishRequest
{
    // These should also use JsonPropertyName for consistency
    [JsonPropertyName("trackName")]
    public string TrackName { get; set; } = string.Empty;

    [JsonPropertyName("artistName")]
    public string ArtistName { get; set; } = string.Empty;

    [JsonPropertyName("albumName")]
    public string AlbumName { get; set; } = string.Empty;

    [JsonPropertyName("duration")]
    public int Duration { get; set; } // For publishing, int is fine as we control it

    [JsonPropertyName("plainLyrics")]
    public string PlainLyrics { get; set; } = string.Empty;

    [JsonPropertyName("syncedLyrics")]
    public string SyncedLyrics { get; set; } = string.Empty;
}

// Represents the response from /request-challenge
public class LrcLibChallengeResponse
{
    [JsonPropertyName("prefix")]
    public string Prefix { get; set; } = string.Empty;

    [JsonPropertyName("target")]
    public string Target { get; set; } = string.Empty;
}

#endregion

public class LyricsMetadataService : ILyricsMetadataService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IRepository<SongModel> _songRepository; // Use a repository for DB operations
    private readonly ILogger<LyricsMetadataService> _logger;
    private readonly IRealmFactory RealmFactory;

    public LyricsMetadataService(
        IHttpClientFactory httpClientFactory,
        IRealmFactory realmFactory,
IRepository<SongModel> songRepository, // Inject the repository
        ILogger<LyricsMetadataService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _songRepository = songRepository;
        _logger = logger;
        RealmFactory = realmFactory;
    }

    private HttpClient CreateClient()
    {
        var client = _httpClientFactory.CreateClient("LrcLib");
        // As per API docs, it's good practice to set a User-Agent
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Dimmer/2.0 (https://github.com/YBTopaz8/Dimmer-MAUI)");
        return client;
    }

    #region Fetching Lyrics (Get & Search)

    /// <summary>
    /// Fetches lyrics for a song using a tiered online strategy.
    /// First, it tries a highly specific match, then falls back to a broader search.
    /// </summary>
    public async Task<LrcLibLyrics?> GetLyricsOnlineAsync(SongModelView song, CancellationToken token)
    {
        if(Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return null;
        // First, attempt the most efficient API call: /api/get
        var preciseMatch = await GetLyricsBySignatureAsync(song.Title, song.ArtistName, song.AlbumName, (int)song.DurationInSeconds, token);
        if (preciseMatch != null)
        {
            _logger.LogInformation("Found precise lyrics match for '{Track}' using /api/get.", song.Title);
            return preciseMatch;
        }

        // If no precise match, fall back to the broader /api/search
        _logger.LogInformation("No precise match found. Falling back to /api/search for '{Track}'.", song.Title);
        var searchResults = await SearchLyricsAsync(song.Title, song.ArtistName, song.AlbumName, token);

        // Return the first result from the search, if any
        return searchResults.FirstOrDefault();
    }

    /// <summary>
    /// Fetches lyrics for a song using a tiered online strategy.
    /// First, it tries a highly specific match, then falls back to a broader search.
    /// </summary>
    public async Task<List<LrcLibLyrics>?> GetAllSyncLyricsOnlineAsync(SongModelView song, CancellationToken token)
    {
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return null;
        // First, attempt the most efficient API call: /api/get
        var preciseMatch = await GetLyricsBySignatureAsync(song.Title, song.ArtistName, song.AlbumName, (int)song.DurationInSeconds, token);
        if (preciseMatch != null)
        {
            _logger.LogInformation("Found precise lyrics match for '{Track}' using /api/get.", song.Title);
            List<LrcLibLyrics>? lrcs = [preciseMatch];
            if (lrcs is not null && !string.IsNullOrEmpty(lrcs.First().SyncedLyrics))
            {
                return lrcs;
            }
        }

        // If no precise match, fall back to the broader /api/search
        _logger.LogInformation("No precise match found. Falling back to /api/search for '{Track}'.", song.Title);
        var searchResults = await SearchLyricsAsync(song.Title, song.ArtistName, song.AlbumName, token);
        var resultsWithSynced = searchResults.Where(r => !string.IsNullOrEmpty(r.SyncedLyrics));
        // Return the first result from the search, if any
        return resultsWithSynced.ToList();
    }

    /// <summary>
    /// Fetches lyrics for a song using a tiered online strategy.
    /// First, it tries a highly specific match, then falls back to a broader search.
    /// </summary>
    public async Task<List<LrcLibLyrics>?> GetAllPlainLyricsOnlineAsync(SongModelView song, CancellationToken token)
    {
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return null;
        // First, attempt the most efficient API call: /api/get
        var preciseMatch = await GetLyricsBySignatureAsync(song.Title, song.ArtistName, song.AlbumName, (int)song.DurationInSeconds, token);
        if (preciseMatch != null)
        {
            _logger.LogInformation("Found precise lyrics match for '{Track}' using /api/get.", song.Title);
            List<LrcLibLyrics>? lrcs = [preciseMatch];
            if (lrcs is not null && !string.IsNullOrEmpty(lrcs.First().PlainLyrics))
            {
                return lrcs;
            }
        }

        // If no precise match, fall back to the broader /api/search
        _logger.LogInformation("No precise match found. Falling back to /api/search for '{Track}'.", song.Title);
        var searchResults = await SearchLyricsAsync(song.Title, song.ArtistName, song.AlbumName, token);
        var resultsWithSynced = searchResults.Where(r => !string.IsNullOrEmpty(r.PlainLyrics));
        // Return the first result from the search, if any
        return resultsWithSynced.ToList();
    }

    /// <summary>
    /// Implements the GET /api/get endpoint for a precise track match.
    /// </summary>
    public async Task<LrcLibLyrics?> GetLyricsBySignatureAsync(string trackName, string artistName, string albumName, int duration, CancellationToken token, bool useCacheOnly = false)
    {
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return null;
        if (string.IsNullOrWhiteSpace(trackName) || string.IsNullOrWhiteSpace(artistName))
            return null;

        var client = CreateClient();
        var endpoint = useCacheOnly ? "api/get-cached" : "api/get";

        var query = HttpUtility.ParseQueryString(string.Empty);
        query["track_name"] = trackName;
        query["artist_name"] = artistName;
        query["album_name"] = albumName ?? string.Empty;
        query["duration"] = duration.ToString();

        var requestUri = $"{endpoint}?{query}";

        try
        {
            var response = await client.GetAsync(requestUri, token);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<LrcLibLyrics>(cancellationToken: token);
            }
            // 404 is an expected result, meaning no match was found.
            if (response.StatusCode != System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("LRCLIB API '{Endpoint}' failed with status {StatusCode} for track '{Track}'.", endpoint, response.StatusCode, trackName);
            }
            return null;
        }
        catch (OperationCanceledException) { throw; } // Propagate cancellation
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling LRCLIB API '{Endpoint}' for track '{Track}'.", endpoint, trackName);
            return null;
        }
    }

    /// <summary>
    /// Implements the GET /api/search endpoint.
    /// </summary>
    public async Task<IEnumerable<LrcLibLyrics>?> SearchLyricsAsync(string trackName, string? artistName, string? albumName, CancellationToken token)
    {
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return null;
        if (string.IsNullOrWhiteSpace(trackName))
            return Enumerable.Empty<LrcLibLyrics>();

        var client = CreateClient();
        var query = HttpUtility.ParseQueryString(string.Empty);
        query["track_name"] = trackName;
        if (!string.IsNullOrWhiteSpace(artistName))
            query["artist_name"] = artistName;
        if (!string.IsNullOrWhiteSpace(albumName))
            query["album_name"] = albumName;

        var requestUri = $"api/search?{query}";

        try
        {
            var results = await client.GetFromJsonAsync<LrcLibLyrics[]>(requestUri, token);
            return results ?? Enumerable.Empty<LrcLibLyrics>();
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling LRCLIB API '/api/search' for track '{Track}'.", trackName);
            return Enumerable.Empty<LrcLibLyrics>();
        }
    }

    /// <summary>
    /// Implements the GET /api/get/{id} endpoint.
    /// </summary>
    public async Task<LrcLibLyrics?> GetLyricsByIdAsync(int id, CancellationToken token)
    {
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return null;
        var client = CreateClient();
        var requestUri = $"api/get/{id}";
        try
        {
            var response = await client.GetAsync(requestUri, token);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<LrcLibLyrics>(cancellationToken: token);
            }
            _logger.LogWarning("LRCLIB API '/api/get/{Id}' failed with status {StatusCode}.", id, response.StatusCode);
            return null;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling LRCLIB API '/api/get/{Id}'.", id);
            return null;
        }
    }

    #endregion

    #region Publishing Lyrics (Challenge & Publish)

    /// <summary>
    /// Publishes lyrics to LRCLIB, automatically handling the proof-of-work challenge.
    /// </summary>
    /// <returns>True if successfully published, otherwise false.</returns>
    public async Task<bool> PublishLyricsAsync(LrcLibPublishRequest lyricsToPublish, CancellationToken token)
    {
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return false;
        var client = CreateClient();

        try
        {
            // Step 1: Request the challenge
            _logger.LogInformation("Requesting proof-of-work challenge from LRCLIB.");
            var challengeResponse = await client.PostAsync("api/request-challenge", null, token);

            // Check if the request was successful before trying to read the content.
            if (!challengeResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get a valid challenge from LRCLIB. Status: {Status}", challengeResponse.StatusCode);
                return false;
            }

            var challenge = await challengeResponse.Content.ReadFromJsonAsync<LrcLibChallengeResponse>(cancellationToken: token);
            if (challenge == null || string.IsNullOrEmpty(challenge.Prefix))
            {
                _logger.LogError("Failed to get a valid challenge from LRCLIB.");
                return false;
            }

            // Step 2: Solve the challenge
            _logger.LogInformation("Solving challenge with prefix '{Prefix}'.", challenge.Prefix);
            string? nonce = await SolveChallengeAsync(challenge.Prefix, challenge.Target, token);
            if (nonce == null)
            {
                _logger.LogError("Could not solve the proof-of-work challenge in time.");
                return false; // Could not find a solution (or was cancelled)
            }
            _logger.LogInformation("Challenge solved with nonce '{Nonce}'.", nonce);

            // Step 3: Publish with the solved token
            string publishToken = $"{challenge.Prefix}:{nonce}";
            client.DefaultRequestHeaders.Add("X-Publish-Token", publishToken);

            var response = await client.PostAsJsonAsync("api/publish", lyricsToPublish, token);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully published lyrics for '{Track}'.", lyricsToPublish.TrackName);
               
                await Shell.Current.DisplayAlert(nonce, "Successfully published lyrics to LRCLIB!", "OK");
                return true;
            }

            _logger.LogError("Failed to publish lyrics for '{Track}'. Status: {Status}. Reason: {Reason}",
                lyricsToPublish.TrackName, response.StatusCode, await response.Content.ReadAsStringAsync(token));
            return false;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Publishing lyrics was cancelled.");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An exception occurred while publishing lyrics for '{Track}'.", lyricsToPublish.TrackName);
            return false;
        }
    }

    /// <summary>
    /// Solves the LRCLIB proof-of-work challenge. This is a CPU-intensive operation.
    /// </summary>
    private Task<string?> SolveChallengeAsync(string prefix, string target, CancellationToken token)
    {
        // Run the CPU-bound work on a background thread to avoid blocking the caller.
        return Task.Run(() =>
        {
            using var sha256 = SHA256.Create();
            long nonceCounter = 0;

            while (!token.IsCancellationRequested)
            {
                string nonce = nonceCounter.ToString();
                string attempt = prefix + nonce;
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(attempt));
                string hashString = Convert.ToHexString(hashBytes).ToLowerInvariant();

                if (hashString.CompareTo(target) < 0)
                {
                    return (string?)nonce; // Solution found!
                }
                nonceCounter++;
            }

            return null; // Cancelled
        }, token);
    }

    #endregion

    #region Saving Lyrics Locally (File Metadata & Realm DB)

    /// <summary>
    /// Saves lyrics to both the audio file's metadata and the local Realm database.
    /// </summary>
    /// <returns>True if both operations succeed.</returns>
    public async Task<bool> SaveLyricsForSongAsync(ObjectId SongID, string? plainLyrics, string? syncedLyrics,bool isInstrument=false)
    {
        var song = _songRepository.GetById(SongID);
        if (string.IsNullOrEmpty(song?.FilePath))
            return false;

        var newLyricsInfo = new LyricsInfo
        {
            UnsynchronizedLyrics = plainLyrics ?? string.Empty
        };
        // Step 1: Save to the audio file's metadata using ATL.
        //try
        //{
        //    var track = new Track(song.FilePath);
        //    track.Lyrics.Clear(); // Remove any existing lyrics to prevent duplicates


        //    if (!string.IsNullOrWhiteSpace(syncedLyrics))
        //    {
        //        newLyricsInfo.Parse(syncedLyrics);
        //    }

        //    track.Lyrics.Add(newLyricsInfo);

        //    if (!track.Save())
        //    {
        //        _logger.LogError("ATL failed to save lyrics metadata to file: {FilePath}", song.FilePath);
        //        return false; // Failed to write to file, abort.
        //    }
        //}
        //catch (Exception ex)
        //{
        //    _logger.LogError(ex, "Exception while saving lyrics metadata to file: {FilePath}", song.FilePath);
        //    return false;
        //}

        // Step 2: Save to the Realm database using the repository.
        try
        {
            await SaveLyricsToDB(isInstrument, plainLyrics ?? string.Empty, song, syncedLyrics ?? string.Empty, newLyricsInfo);
            _logger.LogInformation("Successfully saved lyrics to file and database for '{Track}'.", song.Title);

            if(!isInstrument)
            {

            // try to publish if not instru to online
            LrcLibPublishRequest newRequest = new LrcLibPublishRequest
            {
                TrackName = song.Title,
                ArtistName = song.ArtistName,
                AlbumName = song.AlbumName ?? string.Empty,
                Duration = (int)song.DurationInSeconds,
                PlainLyrics = plainLyrics ?? string.Empty,
                SyncedLyrics = syncedLyrics ?? string.Empty
            };
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(2));

            await PublishLyricsAsync(newRequest, cancellationTokenSource.Token);

            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update lyrics in database for song ID {SongId}.", song.Id);
            // At this point, the file is updated but the DB is not. This is an inconsistent state.
            // For a production app, you might add logic here to revert the file change.
            return false;
        }
    }
    #endregion



    public async Task<string?> GetLocalLyricsAsync(SongModelView song)
    {
        if (string.IsNullOrEmpty(song?.FilePath) || !File.Exists(song.FilePath))
            return null;

        var realmm = RealmFactory?.GetRealmInstance();
        if (realmm is null)
        {
            _logger.LogWarning("Realm instance is null when trying to get local lyrics for {SongTitle}", song.Title);
            return null;
        }
        var songInDb = realmm.Find<SongModel>(song.Id);
        if (songInDb == null)
        {
            _logger.LogWarning("Could not find song with ID {SongId} in database when trying to get local lyrics for {SongTitle}", song.Id, song.Title);
            return null;
        }

        string? lyrics = songInDb.SyncLyrics;
        if (lyrics is not null)
        {
            return lyrics;
        }


        // Priority 1: Embedded Lyrics
        string? embeddedLyrics = GetEmbeddedLyrics(song.FilePath);
        if (!string.IsNullOrEmpty(embeddedLyrics))
        {
            _logger.LogInformation("Found embedded lyrics for {SongTitle}", song.Title);
            return embeddedLyrics;
        }

        // Priority 2: External .lrc file
        string? lrcFileLyrics = await GetExternalLrcFileAsync(song.FilePath);
        if (!string.IsNullOrEmpty(lrcFileLyrics))
        {
            _logger.LogInformation("Found external .lrc file for {SongTitle}", song.Title);
            return lrcFileLyrics;
        }

        return null;
    }
    private string? GetEmbeddedLyrics(string songPath)
    {
        try
        {
            var tagFile = new Track(songPath);
            // ATL is smart. If lyrics exist, it will populate them.
            // We just need to check if the text is there.
            if (tagFile.Lyrics is not null && tagFile.Lyrics.Count>0)
            {
                if (tagFile.Lyrics[0].SynchronizedLyrics.Count>0)
                {
                    return tagFile.Lyrics[0].SynchronizedLyrics.ToString();
                }

            }
            if (tagFile.Lyrics != null && tagFile.Lyrics.Count > 0 && !string.IsNullOrWhiteSpace(tagFile.Lyrics[0].UnsynchronizedLyrics))
            {
                return tagFile.Lyrics[0].UnsynchronizedLyrics;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading embedded tags from {SongPath}", songPath);
        }
        return null;
    }

    private async Task<string?> GetExternalLrcFileAsync(string songPath)
    {
        string lrcPath = Path.ChangeExtension(songPath, ".lrc");
        if (System.IO.File.Exists(lrcPath))
        {
            try
            {
                return await System.IO.File.ReadAllTextAsync(lrcPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading .lrc file at {LrcPath}", lrcPath);
            }
        }
        return null;
    }


    public async Task<bool> SaveLyricsToDB(bool IsInstru, string planLyrics, SongModel song, string? lrcContent, LyricsInfo? lyrics)
    {
        if (string.IsNullOrEmpty(song?.FilePath) || string.IsNullOrWhiteSpace(lrcContent))
        {
            return false;
        }


        // Step 2: Update the database record
        try
        {

            var realm = RealmFactory?.GetRealmInstance();
            if (realm == null)
            { return false; }
            var songModel = realm.Find<SongModel>(song.Id);
            if (songModel == null)
            {
                _logger.LogWarning("Could not find song with ID {SongId} in database to save lyrics.", song.Id);
                return false;
            }

            await realm.WriteAsync(() =>
            {
                songModel.IsInstrumental = IsInstru;
                songModel.UnSyncLyrics=planLyrics;
                if (string.IsNullOrEmpty(songModel.SyncLyrics) || songModel.EmbeddedSync.Count<1)
                {
                    songModel.UnSyncLyrics=song.UnSyncLyrics;
                    songModel.SyncLyrics = lrcContent;
                    songModel.HasSyncedLyrics = true; // Update flags
                    songModel.HasLyrics = true;
                    songModel.EmbeddedSync.Clear();
                    if (lyrics is null)
                    {

                        // A. Parse the LRC data into ATL's structure
                        var newLyricsInfo = new LyricsInfo();
                        newLyricsInfo.Parse(lrcContent);
                        lyrics=newLyricsInfo;
                    }
                    foreach (var lyr in lyrics.SynchronizedLyrics)
                    {
                        var syncLyrics = new SyncLyrics
                        {
                            TimestampMs = lyr.TimestampEnd,
                            Text = lyr.Text
                        };
                        songModel.EmbeddedSync.Add(syncLyrics);
                    }

                    songModel.LastDateUpdated = DateTimeOffset.UtcNow;

                }
                else
                {
                    songModel.HasLyrics = false;
                    songModel.HasSyncedLyrics=false;
                }
                realm.Add(songModel, true);
            });
            //// Important: Update the view model that was passed in so the UI has the latest data
            //_mapper.Map(songModel, song);

            _logger.LogInformation("Successfully updated lyrics in database for {SongTitle}", song.Title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update song in database with new lyrics for {SongTitle}", song.Title);
            // We could consider deleting the .lrc file here for consistency, but for now we'll leave it.
            return false;
        }


        return true;
        string lrcPath = Path.ChangeExtension(song.FilePath, ".lrc");
        try
        {
            await System.IO.File.WriteAllTextAsync(lrcPath, lrcContent);
            _logger.LogInformation("Successfully saved lyrics to {LrcPath}", lrcPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write .lrc file to {LrcPath}", lrcPath);
            return false;
        }

    }
}

//    #endregion
//}

