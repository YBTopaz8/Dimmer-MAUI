namespace Dimmer_MAUI.Utilities.IServices;
public interface ILyricsService
{
    IObservable<IList<LyricPhraseModel>> SynchronizedLyricsStream { get; }
    IObservable<LyricPhraseModel> CurrentLyricStream { get; }
    IObservable<string> UnSynchedLyricsStream { get; }

    void LoadLyrics(SongModelView song);
    IList<LyricPhraseModel> GetSpecificSongLyrics(SongModelView song);
    void UpdateCurrentLyricIndex(double currentPositionInSeconds);
    void StartLyricIndexUpdateTimer();
    void StopLyricIndexUpdateTimer();

    Task<(bool IsFetchSuccessful, Content[]? contentData)> FetchLyricsOnlineLrcLib(SongModelView songs, bool useManualSearch = false, List<string>? manualSearchFields = null);
    Task<(bool IsFetchSuccessful, Content[]? contentData)> FetchLyricsOnlineLyrist(string songTitle, string songArtistName);
    Task<string> FetchAndDownloadCoverImage(string songTitle, string songArtistName, string albumName, SongModelView? song = null);
    bool WriteLyricsToLyricsFile(string syncedLyrics, SongModelView songObj, bool IsSynchedLyrics);
    void InitializeLyrics(string synclyrics);
}
