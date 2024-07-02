using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Utilities.IServices;
public class AppSettingsService : IAppSettingsService
{
    public static class VolumeSettingsPreference
    {
        const double defaultVolume = 1;
        const double maxVolume = 1.0;
        const double minVolume = 0.0;

        public static double Volume
        {
            get => Preferences.Default.Get(nameof(Volume), defaultVolume);
            set => Preferences.Default.Set(nameof(Volume), Math.Clamp(value, minVolume, maxVolume));
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

}
