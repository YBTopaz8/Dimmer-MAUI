
namespace Dimmer.Interfaces;
public interface ISettingsService
{
    RepeatMode RepeatMode { get; set; }
    bool ShuffleOn { get; set; }
    double VolumeLevel { get; set; }
    string LastPlayedSong { get; set; }
    bool IsStickToTop { get; set; }
    AppStateModel CurrentAppStateModel { get; }

    void LoadSettings();
}
