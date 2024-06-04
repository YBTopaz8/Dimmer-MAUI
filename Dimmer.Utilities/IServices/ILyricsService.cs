
namespace Dimmer.Utilities.IServices;
public interface ILyricsService
{

    IObservable<IList<LyricPhraseModel>> SynchronizedLyricsStream { get; }
    IObservable<LyricPhraseModel> CurrentLyricStream { get; }
    IObservable<string> UnSynchedLyricsStream { get; }

    void LoadLyrics(string songPath);
    Task UpdateCurrentLyricIndex(double currentPositionInSeconds);

    void StartLyricIndexUpdateTimer();
    void StopLyricIndexUpdateTimer();
}
