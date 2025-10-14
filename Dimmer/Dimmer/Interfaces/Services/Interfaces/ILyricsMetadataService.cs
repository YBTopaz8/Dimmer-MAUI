namespace Dimmer.Interfaces.Services.Interfaces;
public interface ILyricsMetadataService
{
    Task<List<LrcLibLyrics>?> GetAllPlainLyricsOnlineAsync(SongModelView song, CancellationToken token);
    Task<List<LrcLibLyrics>?> GetAllSyncLyricsOnlineAsync(SongModelView song, CancellationToken token);
    Task<string?> GetLocalLyricsAsync(SongModelView song);
    Task<LrcLibLyrics?> GetLyricsByIdAsync(int id, CancellationToken token);
    Task<LrcLibLyrics?> GetLyricsBySignatureAsync(string trackName, string artistName, string albumName, int duration, CancellationToken token, bool useCacheOnly = false);
    Task<LrcLibLyrics?> GetLyricsOnlineAsync(SongModelView song, CancellationToken token);
    Task<bool> PublishLyricsAsync(LrcLibPublishRequest lyricsToPublish, CancellationToken token);
    Task<bool> SaveLyricsForSongAsync(ObjectId SongID, string? plainLyrics, string? syncedLyrics, bool isInstrument = false);
    Task<bool> SaveLyricsToDB(bool IsInstru, string planLyrics, SongModel song, string lrcContent, LyricsInfo? lyrics);
    Task<IEnumerable<LrcLibLyrics>?> SearchLyricsAsync(string trackName, string? artistName, string? albumName, CancellationToken token);
}

