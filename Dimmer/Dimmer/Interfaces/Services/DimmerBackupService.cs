using Dimmer.Interfaces.Services.Interfaces.FileProcessing.FileProcessorUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Dimmer.Interfaces.Services;
public class DimmerBackupService
{
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly object _logLock = new();
    private readonly string _backupDirectory;
    IRealmFactory RealmFactory;
    public DimmerBackupService(IRealmFactory factory)
    {
        RealmFactory = factory;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true, // Makes JSON human-readable
            Converters = { new ObjectIdNullableConverter(), new ObjectIdNullableConverter() }
        };

        _backupDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "DimmerBackUp");
    }

    // Save individual backup files (legacy support)
    public void SaveBackUp(string logContent, string name, string fileExt, string? secondPath=null)
    {
        try
        {
            if (!Directory.Exists(_backupDirectory))
                Directory.CreateDirectory(_backupDirectory);

            string fileName = $"DimmerBackUp_{DateTime.Now:yyyy-MM-dd_HHmmss}_{name}.{fileExt}";
            string filePath = Path.Combine(_backupDirectory, fileName);
            
            lock (_logLock)
            {
                File.WriteAllText(filePath, logContent);
            }
            if (!string.IsNullOrEmpty(secondPath))
            {

                string secondFilePath = Path.Combine(secondPath, fileName);

                lock (_logLock)
                {
                    File.WriteAllText(secondFilePath, logContent);
                }
            }

            Debug.WriteLine($"Backup saved: {fileName}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Backup write failed: {ex}");
        }
    }

    // New method: Create complete backup
    public async Task<bool> CreateCompleteBackupAsync(string? secondPath =null)
    {
        try
        {
            var realm = RealmFactory.GetRealmInstance();

            // Get app state
            var appModel = realm.All<AppStateModel>().FirstOrDefaultNullSafe();
            var appStateView = appModel?.ToAppStateModelView();

            // Get favorite songs
            var favoriteSongs = realm.All<SongModel>()
                .Where(x => x.IsFavorite)
                .AsEnumerable()
                .Select(x => x.ToSongModelView())
                .ToList();

            // Get play events (only plain data)
            var playEvents = realm.All<DimmerPlayEvent>()
                .AsEnumerable()
                .Select(x => ConvertToBackup(x))
                .ToList();

            // Create complete backup package
            var backupData = new CompleteBackupData
            {
                AppState = appStateView,
                FavoriteSongs = favoriteSongs,
                PlayEvents = playEvents,
                BackupDate = DateTime.Now,
                Version = "1.0"
            };

            // Serialize to JSON
            var json = JsonSerializer.Serialize(backupData, _jsonOptions);

            // Save as single file
            SaveBackUp(json, "CompleteBackup", "json",secondPath);

        

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Complete backup failed: {ex}");
            return false;
        }
    }

    // Convert DimmerPlayEventView to plain backup model
    private DimmerPlayEventBackup ConvertToBackup(DimmerPlayEvent playEvent)
    {
        return new DimmerPlayEventBackup
        {
            Id = playEvent.Id.ToString(),
            SongName = playEvent.SongName,
            ArtistName = playEvent.ArtistName,
            AlbumName = playEvent.AlbumName,
            TitleAndDurationKey = playEvent.SongsLinkingToThisEvent.FirstOrDefaultNullSafe()?.TitleDurationKey,
            CoverImagePath = playEvent.CoverImagePath,
            IsFav = playEvent.IsFav,
            SongId = playEvent.SongId?.ToString(),
            PlayType = playEvent.PlayType,
            PlayTypeStr = playEvent.PlayTypeStr,
            DatePlayed = playEvent.DatePlayed,
            WasPlayCompleted = playEvent.WasPlayCompleted,
            PositionInSeconds = playEvent.PositionInSeconds,
            EventDate = playEvent.EventDate,
            DeviceName = playEvent.DeviceName,
            DeviceFormFactor = playEvent.DeviceFormFactor,
            DeviceModel = playEvent.DeviceModel,
            DeviceManufacturer = playEvent.DeviceManufacturer,
            DeviceVersion = playEvent.DeviceVersion
        };
    }

    // Restore from backup file
    public async Task<RestoreResult> RestoreFromBackupAsync(string filePath)
    {
        var result = new RestoreResult();

        try
        {
            Stream? stream = null;

            if (filePath.StartsWith("content://", StringComparison.OrdinalIgnoreCase))
            {
                if (TaggingUtils.PlatformGetStreamHook != null)
                {
                    stream = TaggingUtils.PlatformGetStreamHook(filePath);
                }
            }
            else
            {
                stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            }

            if (stream == null)
            {
                result.ErrorMessage = "Could not open file stream";
                return result;
            }

            using (stream)
            {
                // Try to read as complete backup first
                if (filePath.Contains("CompleteBackup", StringComparison.OrdinalIgnoreCase))
                {
                    var completeData = await JsonSerializer.DeserializeAsync<CompleteBackupData>(stream, _jsonOptions);
                    if (completeData != null)
                    {
                        await RestoreCompleteDataAsync(completeData, result);
                        ShareFile shrFile = new ShareFile(filePath);
                        

                        await Share.Default.RequestAsync(new ShareFileRequest
                        {
                            Title = "Share Events BackUp file",
                            File = shrFile
                        });
                    }
                }
                else if (filePath.Contains("AllFavs", StringComparison.OrdinalIgnoreCase))
                {
                    var favorites = await JsonSerializer.DeserializeAsync<List<SongModelView>>(stream, _jsonOptions);
                    await RestoreFavoritesAsync(favorites, result);
                }
                else if (filePath.Contains("AppState", StringComparison.OrdinalIgnoreCase))
                {
                    var appState = await JsonSerializer.DeserializeAsync<AppStateModelView>(stream, _jsonOptions);
                    await RestoreAppStateAsync(appState, result);
                }
                else if (filePath.Contains("AllDimmerEvents", StringComparison.OrdinalIgnoreCase))
                {
                    var events = await JsonSerializer.DeserializeAsync<List<DimmerPlayEventBackup>>(stream, _jsonOptions);
                    await RestorePlayEventsAsync(events, result);
                }
            }
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"Restore failed: {ex.Message}";
            Debug.WriteLine($"Error during restore: {ex}");
        }

        return result;
    }

    // Restore complete data to Realm
    private async Task RestoreCompleteDataAsync(CompleteBackupData data, RestoreResult result)
    {
        var realm = RealmFactory.GetRealmInstance();

        await realm.WriteAsync(() =>
        {
            // Restore app state
            if (data.AppState != null)
            {
                var existingAppState = realm.All<AppStateModel>().FirstOrDefaultNullSafe();
                if (existingAppState != null)
                {
                    // Update existing
                    UpdateAppStateFromView(existingAppState, data.AppState);
                }
                else
                {
                    // Create new
                    var newAppState = new AppStateModel();
                    UpdateAppStateFromView(newAppState, data.AppState);
                    realm.Add(newAppState);
                }
                result.AppStateRestored = true;
            }

            // Restore favorite songs
            if (data.FavoriteSongs?.Count >0)
            {
                // First, clear existing favorite flags
                var allSongs = realm.All<SongModel>().ToList();
               

                // Set favorites based on backup
                foreach (var favView in data.FavoriteSongs)
                {
                    if (favView is null) continue;
                    var song = realm.All<SongModel>()
                        .FirstOrDefaultNullSafe(s => s.TitleDurationKey == favView.TitleDurationKey);

                    if (song != null)
                    {
                        song.IsFavorite = true;
                    }
                }
                result.FavoritesRestored = data.FavoriteSongs.Count;
            }

            // Restore play events
            if (data.PlayEvents?.Any() == true)
            {



                // Add restored events
                foreach (var backupEvent in data.PlayEvents)
                {
                    var newEvent = ConvertFromBackup(backupEvent);
                    if (newEvent != null)
                    {
                        realm.Add(newEvent);
                    }
                }
                result.EventsRestored = data.PlayEvents.Count;
            }
        });
    }

    // Individual restore methods
    private async Task RestoreFavoritesAsync(List<SongModelView>? favorites, RestoreResult result)
    {
        if (favorites?.Any() != true) return;

        var realm = RealmFactory.GetRealmInstance();

        await realm.WriteAsync(() =>
        {
          
            // Set new favorites
            foreach (var favView in favorites)
            {
                var song = realm.All<SongModel>()
                    .FirstOrDefaultNullSafe(s => s.TitleDurationKey == favView.TitleDurationKey);

                if (song != null)
                {
                    song.IsFavorite = true;
                }
            }
        });

        result.FavoritesRestored = favorites.Count;
    }

    private async Task RestoreAppStateAsync(AppStateModelView? appState, RestoreResult result)
    {
        if (appState == null) return;

        var realm = RealmFactory.GetRealmInstance();

        await realm.WriteAsync(() =>
        {
            var existingAppState = realm.All<AppStateModel>().FirstOrDefaultNullSafe();
            if (existingAppState != null)
            {
                UpdateAppStateFromView(existingAppState, appState);
            }
            else
            {
                var newAppState = new AppStateModel();
                UpdateAppStateFromView(newAppState, appState);
                realm.Add(newAppState);
            }
        });

        result.AppStateRestored = true;
    }

    private async Task RestorePlayEventsAsync(List<DimmerPlayEventBackup>? events, RestoreResult result)
    {
        if (events?.Any() != true) return;

        var realm = RealmFactory.GetRealmInstance();

        await realm.WriteAsync(() =>
        {
           

            // Add restored events
            foreach (var backupEvent in events)
            {
                var newEvent = ConvertFromBackup(backupEvent);
                var isNull = realm.Find<DimmerPlayEvent>(newEvent.Id);
                if (isNull is null)
                {
                    realm.Add(newEvent);
                }
            }
        });

        result.EventsRestored = events.Count;
        
    }

    // Helper methods
    private DimmerPlayEvent ConvertFromBackup(DimmerPlayEventBackup backup)
    {
        return new DimmerPlayEvent
        {
            Id = ObjectId.Parse(backup.Id),
            SongName = backup.SongName,

            ArtistName = backup.ArtistName,
            AlbumName = backup.AlbumName,
            CoverImagePath = backup.CoverImagePath,
            IsFav = backup.IsFav,
            SongId = !string.IsNullOrEmpty(backup.SongId) ? ObjectId.Parse(backup.SongId) : null,
            PlayType = backup.PlayType,
            PlayTypeStr = backup.PlayTypeStr,
            DatePlayed = backup.DatePlayed,
            WasPlayCompleted = backup.WasPlayCompleted,
            PositionInSeconds = backup.PositionInSeconds,
            EventDate = backup.EventDate,
            DeviceName = backup.DeviceName,
            DeviceFormFactor = backup.DeviceFormFactor,
            DeviceModel = backup.DeviceModel,
            DeviceManufacturer = backup.DeviceManufacturer,
            DeviceVersion = backup.DeviceVersion
        };
    }

    private void UpdateAppStateFromView(AppStateModel model, AppStateModelView view)
    {
        // Map properties from view to model
        // Add your property mappings here based on your AppStateModel structure
        // Example:
        // model.CurrentSongId = view.CurrentSongId != null ? ObjectId.Parse(view.CurrentSongId) : null;
        // model.Volume = view.Volume;
        // etc.
    }

    // Get all backup files
    public async Task<List<string>> GetBackupFilesAsync(string? OptionalPath = null, bool includeDefaultPath = true)
    {
        if(!includeDefaultPath && string.IsNullOrEmpty(OptionalPath))
            return new List<string>();
        List<string>? filesInDefaultPath = new();
        List<string>? filesInSecondPath = new();

        if (includeDefaultPath)
        {
            var pathsToScan = new List<string>();
            if (!Directory.Exists(OptionalPath))
                return new List<string>();

            var supportedExtensions = new HashSet<string> { ".json" };
            filesInSecondPath = await TaggingUtils.GetAllFilesFromPathsAsync(
                new List<string> { OptionalPath },
                supportedExtensions);
        }

        if (includeDefaultPath)
        {
            var pathsToScan = new List<string>();
            if (!Directory.Exists(_backupDirectory))
                return new List<string>();

            var supportedExtensions = new HashSet<string> { ".json" };
            filesInDefaultPath = await TaggingUtils.GetAllFilesFromPathsAsync(
                new List<string> { _backupDirectory },
                supportedExtensions);
        }
        return filesInDefaultPath.Concat(filesInSecondPath).ToList();
    }

    // Delete old backups (optional)
    public void CleanupOldBackups(int keepLatest = 10)
    {
        try
        {
            if (!Directory.Exists(_backupDirectory))
                return;

            var files = Directory.GetFiles(_backupDirectory, "*.json")
                .OrderByDescending(f => File.GetCreationTime(f))
                .Skip(keepLatest)
                .ToList();

            foreach (var file in files)
            {
                File.Delete(file);
                Debug.WriteLine($"Deleted old backup: {file}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Cleanup failed: {ex}");
        }
    }
}

// Result class for restore operations
public class RestoreResult
{
    public bool Success => !string.IsNullOrEmpty(ErrorMessage);
    public string? ErrorMessage { get; set; }
    public int FavoritesRestored { get; set; }
    public int EventsRestored { get; set; }
    public bool AppStateRestored { get; set; }

    public override string ToString()
    {
        if (!string.IsNullOrEmpty(ErrorMessage))
            return $"Restore failed: {ErrorMessage}";

        return $"Restore completed: {FavoritesRestored} favorites, {EventsRestored} events, " +
               $"AppState: {(AppStateRestored ? "Yes" : "No")}";
    }
}

// Additional converter for nullable ObjectId
public class ObjectIdNullableConverter : JsonConverter<ObjectId?>
{
    public override ObjectId? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var str = reader.GetString();
        return string.IsNullOrEmpty(str) ? null : new ObjectId(str);
    }

    public override void Write(Utf8JsonWriter writer, ObjectId? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteStringValue(value.Value.ToString());
        else
            writer.WriteNullValue();
    }
}

