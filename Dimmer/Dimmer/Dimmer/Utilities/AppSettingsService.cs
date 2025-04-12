using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Linq;

namespace Dimmer.Utilities;
public class AppSettingsService 
{

    public static class ShowCloseConfirmationPopUp
    {
        const bool showCloseConfirmation = false;
        public static bool ShowCloseConfirmation
        {
            get => Preferences.Default.Get(nameof(ShowCloseConfirmation), showCloseConfirmation);
            set => Preferences.Default.Set(nameof(ShowCloseConfirmation), value);
        }
        public static void ToggleCloseConfirmation(bool showClose)
        {
            ShowCloseConfirmation = showClose;
        }
        public static bool GetCloseConfirmation()
        {
            return ShowCloseConfirmation;
        }
    }

    public static class VolumeSettingsPreference
    {
        const double defaultVolume = 15;
        const double maxVolume = 15.0;
        const double minVolume = 0.0;

        public static double Volume
        {
            get => Preferences.Default.Get(nameof(Volume), defaultVolume);
            set
            {
                Preferences.Default.Set(nameof(Volume), Math.Clamp(value, minVolume, maxVolume));
            }
        }

        public static void SetVolumeLevel(double newValue)
        {
            if (newValue != defaultVolume)
            {
                Volume = newValue;
            }
        }
        public static double GetVolumeLevel()
        {
            return Volume;
        }

    }

    public static class IsAuthenticatedPreference
    {
        const bool isConnected = false;
        public static bool IsConnected
        {
            get => Preferences.Default.Get(nameof(IsConnected), isConnected);
            set => Preferences.Default.Set(nameof(IsConnected), value);
        }
        public static void ToggleMuteState(bool muteState)
        {
            IsConnected = muteState;
        }
        public static bool GetMuteState()
        {
            return IsConnected;
        }
    }

    public static class LastPlayedSongSettingPreference
    {
        const string defaultSongId = null;
        public static string LastPlayedSongId
        {
            get => Preferences.Default.Get(nameof(LastPlayedSongId), defaultSongId);
            set => Preferences.Default.Set(nameof(LastPlayedSongId), value);
        }

        public static void SetLastPlayedSong(string songID)
        {
            if (!string.IsNullOrEmpty(songID))
            {
                LastPlayedSongId = songID.ToString();
            }
        }
        public static string? GetLastPlayedSong()
        {
            return LastPlayedSongId is null ? null : new string(LastPlayedSongId);
        }
    }

    public static class LastPlayedSongPositionPref
    {
        const double positionFraction = 0;
        public static double LastSongPosition
        {
            get => Preferences.Default.Get(nameof(LastSongPosition), positionFraction);
            set => Preferences.Default.Set(nameof(LastSongPosition), value);
        }
        public static void SetLastPosition(double Position)
        {
            LastSongPosition = Position;
        }
        public static double GetLastPosition()
        {
            return LastSongPosition;
        }
    }

    public static class ShuffleStatePreference
    {
        public const bool isShuffleOn = false;
        public static bool IsShuffleOn
        {
            get => Preferences.Default.Get(nameof(IsShuffleOn), isShuffleOn);
            set => Preferences.Default.Set(nameof(IsShuffleOn), value);
        }

        public static void ToggleShuffleState(bool shuffleState)
        {
            IsShuffleOn = shuffleState;
        }
        public static bool GetShuffleState()
        {
            return IsShuffleOn;
        }
    }

    public static class IsSticktoTopPreference
    {
        public const bool isSticktoTop = false;
        public static bool IsSticktoTop
        {
            get => Preferences.Default.Get(nameof(IsSticktoTop), isSticktoTop);
            set => Preferences.Default.Set(nameof(IsSticktoTop), value);
        }


        public static void ToggleIsSticktoTopState(bool isSticktoTop)
        {
            IsSticktoTop = isSticktoTop;
        }
        public static bool GetIsSticktoTopState()
        {
            return IsSticktoTop;
        }
    }
    public static class ConfirmAppExitPreference
    {
        public const bool confirmAppExit = false;
        public static bool ConfirmAppExit
        {
            get => Preferences.Default.Get(nameof(ConfirmAppExit), confirmAppExit);
            set => Preferences.Default.Set(nameof(ConfirmAppExit), value);
        }


        public static void ToggleConfirmAppExitState(bool confirmAppExit)
        {
            ConfirmAppExit = confirmAppExit;
        }
        public static bool GetConfirmAppExitState()
        {
            return ConfirmAppExit;
        }
    }
    public static class RepeatModePreference
    {
        public const int repeatState = 0; //0 for repeat OFF, 1 for repeat ALL, 2 for repeat ONE, 3 for repeat custom
        public static int RepeatState
        {
            get => Preferences.Default.Get(nameof(RepeatState), repeatState);
            set => Preferences.Default.Set(nameof(RepeatState), value);
        }


        public static void ToggleRepeatState(int repMode)
        {
            RepeatState = repMode;
        }
        public static int GetRepeatState()
        {
            return RepeatState;
        }
    }

   
    public static class MusicFoldersPreference
    {
        const string defaultFolder = null;

        public static List<string> MusicFolders
        {
            get
            {
                string serializedFolders = Preferences.Default.Get(nameof(MusicFolders), JsonSerializer.Serialize(new List<string> { defaultFolder }));
                return JsonSerializer.Deserialize<List<string>>(serializedFolders);
            }
            set
            {
                string serializedFolders = JsonSerializer.Serialize(value);
                Preferences.Default.Set(nameof(MusicFolders), serializedFolders);
            }
        }

        public static void AddMusicFolder(List<string> folderPaths)
        {
            List<string> folders = MusicFolders;
            foreach (string folderPath in folderPaths)
            {
                if (!folders.Contains(folderPath))
                {
                    folders.Add(folderPath);
                }
            }

            MusicFolders = folders;
        }


        public static void RemoveMusicFolder(string[] folderPaths)
        {
            List<string> folders = MusicFolders;
            foreach (var folderPath in from string folderPath in folderPaths
                                       where folders.Contains(folderPath)
                                       select folderPath)
            {
                folders.Remove(folderPath);
                MusicFolders = folders;
            }
        }

        public static List<string> GetMusicFolders()
        {
            return MusicFolders;
        }

        public static bool ClearListOfFolders()
        {
            MusicFolders = []; 
            return true;
        }
    }
    public static class DiscordRPCPreference
    {
        const bool isDiscordRPCEnabled = false;
        public static bool IsDiscordRPCEnabled
        {
            get => Preferences.Default.Get(nameof(IsDiscordRPCEnabled), isDiscordRPCEnabled);
            set
            {
                Preferences.Default.Set(nameof(IsDiscordRPCEnabled), value);
            }
        }

        public static void ToggleDiscordRPC(bool isEnabled)
        {
            IsDiscordRPCEnabled = isEnabled;
        }
        public static bool GetDiscordRPC()
        {
            return IsDiscordRPCEnabled;
        }
    }
}
public class MyAppJsonContext : JsonSerializerContext
{
    public MyAppJsonContext(JsonSerializerOptions? options) : base(options)
    {
    }

    protected override JsonSerializerOptions? GeneratedSerializerOptions => throw new NotImplementedException();

    public override JsonTypeInfo? GetTypeInfo(Type type)
    {
        throw new NotImplementedException();
    }
}


public enum PlaybackSource
{
    MainQueue,
    AlbumsQueue,
    SearchQueue,
    External,
    PlaylistQueue,
    ArtistQueue,

}
