
namespace Dimmer.Interfaces;
public interface ISettingsService
{
    RepeatMode RepeatMode { get; set; }
    bool ShuffleOn { get; set; }
    double VolumeLevel { get; set; }
    string LastPlayedSong { get; set; }
    bool IsStickToTop { get; set; }
    AppStateModel CurrentAppStateModel { get; }

    AppStateModel LoadSettings();
    
    void AddMusicFolder(string path);
    bool RemoveMusicFolder(string path);
    void SetMusicFolders(IEnumerable<string> paths);
    bool ClearAllFolders();
}
