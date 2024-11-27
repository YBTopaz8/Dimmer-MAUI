using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
namespace Dimmer_MAUI.Utilities.Services;
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

        public static void SetLastPlayedSong(string songID)
        {
            if (songID != string.Empty)
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

    public static class SortingModePreference
    {
        public const int sortingMode = 0;
        public static int SortingMode
        {
            get => Preferences.Default.Get(nameof(SortingMode), sortingMode);
            set => Preferences.Default.Set(nameof(SortingMode), value);
        }

        public static void SetSortingPref(SortingEnum sortingMode)
        {
            var mode = (int)sortingMode;
            if (mode != SortingMode)
            {
                SortingMode = mode;
            }
        }
        public static SortingEnum GetSortingPref()
        {
            return (SortingEnum)SortingMode;
        }

    }

    public static ObservableCollection<SongModelView> ApplySorting(ObservableCollection<SongModelView> colToSort, SortingEnum mode)
    {
        switch (mode)
        {
            case SortingEnum.TitleAsc:
                colToSort = colToSort.OrderBy(x => x.Title).ToObservableCollection();
                break;
            case SortingEnum.TitleDesc:
                colToSort = colToSort.OrderByDescending(x => x.Title).ToObservableCollection();
                break;
            case SortingEnum.ArtistNameAsc:
                colToSort = colToSort.OrderBy(x => x.ArtistName).ToObservableCollection();
                break;
            case SortingEnum.ArtistNameDesc:
                colToSort = colToSort.OrderByDescending(x => x.ArtistName).ToObservableCollection();
                break;
            case SortingEnum.DateAddedAsc:
                colToSort = colToSort.OrderBy(x => x.DateCreated).ToObservableCollection();
                break;
            case SortingEnum.DateAddedDesc:
                colToSort = colToSort.OrderByDescending(x => x.DateCreated).ToObservableCollection();
                break;
            case SortingEnum.DurationAsc:
                colToSort = colToSort.OrderBy(x => x.DurationInSeconds).ToObservableCollection();
                break;
            case SortingEnum.DurationDesc:
                colToSort = colToSort.OrderByDescending(x => x.DurationInSeconds).ToObservableCollection();
                break;
            case SortingEnum.YearAsc:
                colToSort = colToSort.OrderBy(x => x.Title).ToObservableCollection();
                break;
            case SortingEnum.YearDesc:
                colToSort = colToSort.OrderByDescending(x => x.Title).ToObservableCollection();
                break;
            //case SortingEnum.NumberOfTimesPlayedAsc:
            //    colToSort = colToSort.OrderBy(x => x.DatesPlayedAndWasPlayCompleted.Count).ToObservableCollection();
            //    break;

            //case SortingEnum.NumberOfTimesPlayedDesc:
            //    colToSort = colToSort.OrderByDescending(x => x.DatesPlayedAndWasPlayCompleted.Count).ToObservableCollection();
            //    break;

            //case SortingEnum.NumberOfTimesPlayedCompletelyAsc:
            //    colToSort = colToSort.OrderBy(x => x.DatesPlayedAndWasPlayCompleted.Count(entry => entry.WasPlayCompleted)).ToObservableCollection();
            //    break;

            //case SortingEnum.NumberOfTimesPlayedCompletelyDesc:
            //    colToSort = colToSort.OrderByDescending(x => x.DatesPlayedAndWasPlayCompleted.Count(entry => entry.WasPlayCompleted)).ToObservableCollection();
            //    break;
                
            case SortingEnum.RatingAsc:
                colToSort = colToSort.OrderBy(x => x.Rating).ToObservableCollection();
                break;

            case SortingEnum.RatingDesc:
                colToSort = colToSort.OrderByDescending(x => x.Rating).ToObservableCollection();
                break;

            default:
                break;
        }
        SortingModePreference.SetSortingPref(mode);
        return colToSort;
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