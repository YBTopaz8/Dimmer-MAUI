﻿
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

    Task<(bool IsFetchSuccessul, Content[] contentData)> FetchLyricsOnlineLrcLib(SongsModelView songs);
    Task<(bool IsFetchSuccessul, LyristApiResponse contentData)> FetchLyricsOnlineLyrics(SongsModelView songs);
    Task FetchAndDownloadCoverImage(SongsModelView songs);
    bool WriteLyricsToLrcFile(string syncedLyrics, SongsModelView songObj);
    void InitializeLyrics(string synclyrics);
}
