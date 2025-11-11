using System.Net.Http.Json;

namespace Dimmer.Interfaces.Services.Lyrics.Orchestrator;
public record LrcLibApiSearchResult(int Id, string TrackName, string ArtistName, string AlbumName, string? SyncedLyrics, string? PlainLyrics);

public class LrcLibProvider : IOnlineLyricsProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<LrcLibProvider> _logger;
    public string ProviderName => "LrcLib API";

    public LrcLibProvider(IHttpClientFactory httpClientFactory, ILogger<LrcLibProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<IEnumerable<LyricsSearchResult>> SearchAsync(string title, string artist, string album)
    {
        if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(artist))
            return Enumerable.Empty<LyricsSearchResult>();

        try
        {
            var client = _httpClientFactory.CreateClient("LrcLib");
            string requestUri = $"api/search?track_name={Uri.EscapeDataString(title)}&artist_name={Uri.EscapeDataString(artist)}&album_name={Uri.EscapeDataString(album)}";

            var results = await client.GetFromJsonAsync<LrcLibApiSearchResult[]>(requestUri);

            return results?.Select(r => new LyricsSearchResult(
                ProviderName,
                r.ArtistName,
                r.TrackName,
                (r.SyncedLyrics ?? r.PlainLyrics ?? "No preview available.").SafeSubstring(100),
                r.Id // Store the API's integer ID
            )) ?? Enumerable.Empty<LyricsSearchResult>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search LrcLib for '{Title}'", title);
            return Enumerable.Empty<LyricsSearchResult>();
        }
    }

    public async Task<string?> FetchAsync(LyricsSearchResult result)
    {
        if (result.ProviderName != ProviderName || result.InternalId is not int id)
            return null;

        try
        {
            var client = _httpClientFactory.CreateClient("LrcLib");
            // The API might return a single object, not an array, for a direct fetch
            var apiResult = await client.GetFromJsonAsync<LrcLibApiSearchResult>($"api/lrc/{id}");
            return apiResult?.SyncedLyrics ?? apiResult?.PlainLyrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch LrcLib lyrics for ID {Id}", id);
            return null;
        }
    }
}
// Helper extension method
public static class StringExtensions
{
    public static string SafeSubstring(this string text, int length)
    {
        return text.Length <= length ? text : text.Substring(0, length) + "...";
    }
}