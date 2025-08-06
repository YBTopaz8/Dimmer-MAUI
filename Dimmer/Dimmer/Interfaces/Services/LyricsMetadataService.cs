using System.Net.Http.Json;

using ATL;

using Dimmer.Interfaces.Services.Interfaces;

namespace Dimmer.Interfaces.Services;

public class LyricsMetadataService : ILyricsMetadataService
{
    private readonly IRealmFactory realmFact;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMapper _mapper; // To map between SongModel and SongModelView
    private readonly ILogger<LyricsMetadataService> _logger;

    public LyricsMetadataService(
        IHttpClientFactory httpClientFactory,
        IMapper mapper,
        IRealmFactory realmFactory,
        ILogger<LyricsMetadataService> logger)
    {

        realmFact = realmFactory;
        _httpClientFactory = httpClientFactory;
        _mapper = mapper;
        _logger = logger;
    }

    #region Get Local Lyrics

    public async Task<string?> GetLocalLyricsAsync(SongModelView song)
    {
        if (string.IsNullOrEmpty(song?.FilePath) || !File.Exists(song.FilePath))
            return null;

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

    #endregion

    #region Search Online

    public async Task<IEnumerable<LrcLibSearchResult>> SearchOnlineAsync(SongModelView song)
    {
        if (string.IsNullOrEmpty(song.Title) || string.IsNullOrEmpty(song.ArtistName))
        {
            return Enumerable.Empty<LrcLibSearchResult>();
        }

        HttpClient client = _httpClientFactory.CreateClient("LrcLib");

        // URL encode the parameters to handle special characters
        string artistName = Uri.EscapeDataString(song.ArtistName.ToLower());
        string trackName = Uri.EscapeDataString(song.Title);
        string albumName = Uri.EscapeDataString(song.AlbumName ?? string.Empty);

        string requestUri = $"api/search?track_name={trackName}&artist_name={artistName}&album_name={albumName}";

        try
        {
            var ress = await client.GetAsync(requestUri);
            if (!ress.IsSuccessStatusCode)
            {
                _logger.LogWarning("LrcLib search request failed with status code {StatusCode} for {TrackName}", ress.StatusCode, trackName);
                return Enumerable.Empty<LrcLibSearchResult>();
            }
            // Deserialize the response into an array of LrcLibSearchResult
            var con = await ress.Content.ReadAsStringAsync();

            //Debug.WriteLine(con);
            var results = await client.GetFromJsonAsync<LrcLibSearchResult[]>(requestUri);
            return results ?? Enumerable.Empty<LrcLibSearchResult>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error searching for lyrics for {TrackName}", trackName);
            return Enumerable.Empty<LrcLibSearchResult>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize LrcLib response for {TrackName}", trackName);
            return Enumerable.Empty<LrcLibSearchResult>();
        }
    }

    public async Task<IEnumerable<LrcLibSearchResult>> SearchOnlineManualParamsAsync(string songName, string songArtist , string songAlbum)
    {
        if (string.IsNullOrEmpty(songName) || string.IsNullOrEmpty(songArtist)|| string.IsNullOrEmpty(songAlbum))
        {
            return Enumerable.Empty<LrcLibSearchResult>();
        }

        HttpClient client = _httpClientFactory.CreateClient("LrcLib");

        // URL encode the parameters to handle special characters
        string artistName = Uri.EscapeDataString(songArtist);
        string trackName = Uri.EscapeDataString(songName);
        string albumName = Uri.EscapeDataString(songAlbum);

        string requestUri = $"api/search?track_name={trackName}&artist_name={artistName}&album_name={albumName}";

        try
        {
            var ress = await client.GetAsync(requestUri);
            if (!ress.IsSuccessStatusCode)
            {
                _logger.LogWarning("LrcLib search request failed with status code {StatusCode} for {TrackName}", ress.StatusCode, trackName);
                return Enumerable.Empty<LrcLibSearchResult>();
            }
            // Deserialize the response into an array of LrcLibSearchResult
            var con = await ress.Content.ReadAsStringAsync();

            //Debug.WriteLine(con);
            var results = await client.GetFromJsonAsync<LrcLibSearchResult[]>(requestUri);
            return results ?? Enumerable.Empty<LrcLibSearchResult>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error searching for lyrics for {TrackName}", trackName);
            return Enumerable.Empty<LrcLibSearchResult>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize LrcLib response for {TrackName}", trackName);
            return Enumerable.Empty<LrcLibSearchResult>();
        }
    }

    #endregion

    #region Save Lyrics

    public async Task<bool> SaveLyricsForSongAsync(SongModelView song, string lrcContent, LyricsInfo lyrics)
    {
        if (string.IsNullOrEmpty(song?.FilePath) || string.IsNullOrWhiteSpace(lrcContent))
        {
            return false;
        }


        // Step 2: Update the database record
        try
        {
            var realm = realmFact?.GetRealmInstance();
            if (realm == null)
            { return false; }
            var songModel = realm.Find<SongModel>(song.Id);
            if (songModel == null)
            {
                _logger.LogWarning("Could not find song with ID {SongId} in database to save lyrics.", song.Id);
                return false;
            }

            realm.Write(() =>
            {
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


    #endregion
}

public partial class LyricEditingLineViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string Timestamp { get; set; } = "[--:--.--]";// Initial display

    [ObservableProperty]
    public partial string Text { get; set; }

    [ObservableProperty]
    public partial bool IsTimed { get; set; } // To change color in UI once timestamped

    [ObservableProperty]
    public partial bool IsCurrentLine{ get; set; } // To highlight the line the user should be focusing on
}