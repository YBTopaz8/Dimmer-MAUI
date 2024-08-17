
namespace Dimmer.Utilities.IServices;
public interface ILyricsService
{
    IObservable<IList<LyricPhraseModel>> SynchronizedLyricsStream { get; }
    IObservable<LyricPhraseModel> CurrentLyricStream { get; }
    IObservable<string> UnSynchedLyricsStream { get; }

    void LoadLyrics(SongsModelView song);
    void UpdateCurrentLyricIndex(double currentPositionInSeconds);
    void StartLyricIndexUpdateTimer();
    void StopLyricIndexUpdateTimer();

    Task<(bool IsFetchSuccessful, Content[] contentData)> FetchLyricsOnlineLrcLib(SongsModelView songs, bool useManualSearch=false, List<string>? manualSearchFields=null);
    Task<(bool IsFetchSuccessful, Content[] contentData)> FetchLyricsOnlineLyrist(SongsModelView songs, bool useManualSearch = false, List<string>? manualSearchFields = null);
    Task<string> FetchAndDownloadCoverImage(SongsModelView songs);
    Task<bool> WriteLyricsToLyricsFile(string syncedLyrics, SongsModelView songObj, bool IsSynchedLyrics);
    void InitializeLyrics(string synclyrics);
}
