using System.Text.Json;
namespace Dimmer.Utilities.Services;
public class AppSettingsService : IAppSettingsService
{
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

    public static class LastPlayedSongSettingPreference
    {
        const string defaultSongId = null;
        public static string LastPlayedSongId
        {
            get => Preferences.Default.Get(nameof(LastPlayedSongId), defaultSongId);
            set => Preferences.Default.Set(nameof(LastPlayedSongId), value);
        }

        public static void SetLastPlayedSong(ObjectId songID)
        {
            if (songID != ObjectId.Empty)
            {
                LastPlayedSongId = songID.ToString();
            }
        }
        public static object? GetLastPlayedSong()
        {
            return LastPlayedSongId is null ? null : new ObjectId(LastPlayedSongId);
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

    public static class RepeatModePreference
    {
        public const int repeatState = 0; //0 for repeat OFF, 1 for repeat ALL, 2 for repeat ONE
        public static int RepeatState
        {
            get => Preferences.Default.Get(nameof(RepeatState), repeatState);
            set => Preferences.Default.Set(nameof(RepeatState), value);
        }


        public static void ToggleRepeatState()
        {
            switch (RepeatState)
            {
                case 0:
                    RepeatState = 1;
                    break;
                case 1:
                    RepeatState = 2;
                    break;
                case 2:
                    RepeatState = 0;
                    break;
                default:
                    break;
            }
        }
        public static int GetRepeatState()
        {
            return RepeatState;
        }
    }

    public static class MusicFoldersPreference
    {
        const string defaultFolder = null;//Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);

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
            var folders = MusicFolders;
            foreach (var folderPath in folderPaths)
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
            var folders = MusicFolders;
            foreach (var folderPath in folderPaths)
            {
                if (folders.Contains(folderPath))
                {
                    folders.Remove(folderPath);
                    MusicFolders = folders;
                }
            }
        }

        public static List<string> GetMusicFolders()
        {
            return MusicFolders;
        }

        public static bool ClearListOfFolders()
        {
            MusicFolders = []; // Enumerable.Empty<List<string>>();
            return true;
        }
    }

}
