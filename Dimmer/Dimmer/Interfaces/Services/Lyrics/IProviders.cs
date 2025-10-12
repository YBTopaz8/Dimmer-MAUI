namespace Dimmer.Interfaces.Services.Lyrics;

public record LyricsSearchResult(string ProviderName, string Artist, string Title, string Snippet, object InternalId);

// Contract for providers that can SEARCH for lyrics online.
public interface IOnlineLyricsSearchProvider
{
    string ProviderName { get; }
    Task<IEnumerable<LyricsSearchResult>> SearchAsync(SongModelView song);
    // Overload for manual searches from the UI
    Task<IEnumerable<LyricsSearchResult>> SearchAsync(string title, string artist, string album);

    // Contract for FETCHING the actual lyrics content from a search result.
    Task<string?> FetchLyricsAsync(LyricsSearchResult result);
}

public interface ILyricsProvider
{
    // A friendly name for the UI (e.g., "Embedded Tags", "Local .lrc Files", "LrcLib API").
    string ProviderName { get; }

    // Providers can be local (fast) or online (slow).
    bool IsOnlineProvider { get; }

    // The core method. It tries to find lyrics for a given song.
    // It returns a LyricsResult which can be a success or failure.
    Task<LyricsResult> GetLyricsAsync(SongModelView song);
}

 public record LyricsResult(bool IsSuccess, string? LrcContent, string ProviderName)
    {
        public static LyricsResult Success(string content, string provider) => new(true, content, provider);
        public static LyricsResult Fail(string provider) => new(false, null, provider);
    }

    // The contract for simple, local providers (e.g., reading files).
    public interface ILocalLyricsProvider
    {
        string ProviderName { get; }
        Task<LyricsResult> GetLyricsAsync(SongModelView song);
    }

    // The contract for online providers that have separate search/fetch steps.
    public interface IOnlineLyricsProvider
    {
        string ProviderName { get; }
        Task<IEnumerable<LyricsSearchResult>> SearchAsync(string title, string artist, string album);
        Task<string?> FetchAsync(LyricsSearchResult result);
    }

    // The contract for the persistence layer. Its only job is to save lyrics.
    public interface ILyricsPersistenceService
    {
        Task<bool> SaveLyricsAsync(SongModelView song, string lrcContent);
        // You could also add a method to get cached lyrics here.
        Task<string?> GetCachedLyricsAsync(SongModelView song);
    }
