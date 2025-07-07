using System.Net.Http.Json;

using ATL;

using Dimmer.Interfaces.Services.Interfaces;

namespace Dimmer.Interfaces.Services;

public class LyricsMetadataService : ILyricsMetadataService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IRepository<SongModel> _songRepo;
    private readonly IMapper _mapper; // To map between SongModel and SongModelView
    private readonly ILogger<LyricsMetadataService> _logger;

    public LyricsMetadataService(
        IHttpClientFactory httpClientFactory,
        IRepository<SongModel> songRepo,
        IMapper mapper,
        ILogger<LyricsMetadataService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _songRepo = songRepo;
        _mapper = mapper;
        _logger = logger;
    }

    #region Get Local Lyrics

    public async Task<string?> GetLocalLyricsAsync(SongModelView song)
    {
        if (string.IsNullOrEmpty(song?.FilePath) || !System.IO.File.Exists(song.FilePath))
        {
            return null;
        }

        // Strategy 1: Check for embedded lyrics first.
        string? embeddedLyrics = GetEmbeddedLyrics(song.FilePath);
        if (!string.IsNullOrEmpty(embeddedLyrics))
        {
            _logger.LogInformation("Found embedded lyrics for {SongTitle}", song.Title);
            return embeddedLyrics;
        }

        // Strategy 2: Check for an external .lrc file.
        string? lrcFileLyrics = await GetExternalLrcFileAsync(song.FilePath);
        if (!string.IsNullOrEmpty(lrcFileLyrics))
        {
            _logger.LogInformation("Found external .lrc file for {SongTitle}", song.Title);
            return lrcFileLyrics;
        }

        _logger.LogInformation("No local lyrics found for {SongTitle}", song.Title);
        return null;
    }

    private string? GetEmbeddedLyrics(string songPath)
    {
        try
        {
            var tagFile = new Track(songPath);

            tagFile.Lyrics.SynchronizedLyrics?.ToString(); // Ensure the lyrics are loaded
                                                           // TagLib-Sharp stores synchronized lyrics in the USLT frame's "Description" field.


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
        string artistName = Uri.EscapeDataString(song.ArtistName.Split(',')[0].Trim());
        string trackName = Uri.EscapeDataString(song.Title);
        string albumName = Uri.EscapeDataString(song.AlbumName ?? string.Empty);

        string requestUri = $"api/search?track_name={trackName}&artist_name={artistName}&album_name={albumName}";

        try
        {
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
            var dbb = IPlatformApplication.Current.Services.GetService<IRealmFactory>();
            var realm = dbb?.GetRealmInstance();
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

                songModel.SyncLyrics = lrcContent;
                songModel.HasSyncedLyrics = true; // Update flags
                songModel.HasLyrics = true;
                songModel.EmbeddedSync.Clear();
                foreach (var lyr in lyrics.SynchronizedLyrics)
                {
                    var syncLyrics = new SyncLyrics
                    {
                        TimestampMs = lyr.TimestampMs,
                        Text = lyr.Text
                    };
                    songModel.EmbeddedSync.Add(syncLyrics);
                }
                realm.Add(songModel, true);

                songModel.LastDateUpdated = DateTimeOffset.UtcNow;

                }
            });
            // Important: Update the view model that was passed in so the UI has the latest data
            _mapper.Map(songModel, song);
            
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

    public Task<bool> SaveLyricsForSongAsync(SongModelView song, string lrcContent)
    {
        throw new NotImplementedException();
    }

    #endregion
}