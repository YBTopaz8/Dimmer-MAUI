namespace Dimmer.Interfaces;
public interface ISettingsService
{
    // Playback Settings
    bool ShuffleOn { get; set; }
    ShuffleMode ShuffleMode { get; set; }
    RepeatMode RepeatMode { get; set; }
    double LastVolume { get; set; } // Example: to remember volume across sessions

    // UI/Other Settings
    bool MinimizeToTrayPreference { get; set; }
    // string LastSelectedOutputDeviceId { get; set; } // Example
    string LastPlayedSong { get; set; }

    bool ClearAllFolders();
    void SaveLastFMUserSession(string sessionTok);
    string? GetLastFMUserSession();

    // Potentially methods to Save/Load settings if not done automatically on set
    // Task SaveAsync();
}