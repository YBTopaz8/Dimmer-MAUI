namespace Dimmer.Interfaces.Services.Lyrics;
public class EmbeddedLyricsProvider : ILyricsProvider
{
    public string ProviderName => "Embedded Metadata";
    public bool IsOnlineProvider => false;

    public Task<LyricsResult> GetLyricsAsync(SongModelView song)
    {
        // All the logic from your old GetEmbeddedLyrics method goes here.
        // It's a synchronous operation wrapped in a Task for the interface.
        try
        {
            var track = new ATL.Track(song.FilePath);
            if (track.Lyrics != null && track.Lyrics.Count > 0)
            {
                // Prioritize synchronized, then unsynchronized.
                var syncLyrics = track.Lyrics[0].SynchronizedLyrics.ToString();
                if (!string.IsNullOrWhiteSpace(syncLyrics) && syncLyrics.Length > 10) // Basic check for valid content
                    return Task.FromResult(LyricsResult.Success(syncLyrics, ProviderName));

                var unsyncLyrics = track.Lyrics[0].UnsynchronizedLyrics;
                if (!string.IsNullOrWhiteSpace(unsyncLyrics))
                    return Task.FromResult(LyricsResult.Success(unsyncLyrics, ProviderName));
            }
        }
        catch (Exception ex)
        {
            // Log the error
        }
        return Task.FromResult(LyricsResult.Fail(ProviderName));
    }
}