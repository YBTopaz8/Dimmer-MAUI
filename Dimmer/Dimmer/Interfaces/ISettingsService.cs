namespace Dimmer.Interfaces;
public interface ISettingsService
{
    // Playback Settings
    bool ShuffleOn { get; set; }
    RepeatMode RepeatMode { get; set; }
    double LastVolume { get; set; } // Example: to remember volume across sessions

    // UI/Other Settings
    bool MinimizeToTrayPreference { get; set; }
    // string LastSelectedOutputDeviceId { get; set; } // Example
    IList<string> UserMusicFoldersPreference { get; } // Read-only view
    string LastPlayedSong { get; set; }

    void AddMusicFolder(string folderPath);
    bool RemoveMusicFolder(string path);
    bool ClearAllFolders();
    void SetMusicFolders(IEnumerable<string> paths);
    void SaveLastFMUserSession(string sessionTok);
    string? GetLastFMUserSession();

    // Potentially methods to Save/Load settings if not done automatically on set
    // Task SaveAsync();
}