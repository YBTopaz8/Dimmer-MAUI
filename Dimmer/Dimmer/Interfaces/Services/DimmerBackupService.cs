using Dimmer.Interfaces.Services.Interfaces.FileProcessing.FileProcessorUtils;
using MongoDB.Bson.IO;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using JsonReader = Newtonsoft.Json.JsonReader;
using JsonToken = Newtonsoft.Json.JsonToken;

namespace Dimmer.Interfaces.Services;
public class DimmerBackupService
{
    private readonly JsonSerializerSettings _jsonSettings;
    private readonly object _logLock = new();
    private readonly string _backupDirectory;
    IRealmFactory RealmFactory;
    public DimmerBackupService(IRealmFactory factory)
    {
        RealmFactory = factory;
        _jsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore
        };
        // Add converter
        _jsonSettings.Converters.Add(new ObjectIdNullableConverter());

        _backupDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "DimmerBackUp");
    }

    // Save individual backup files (legacy support)
    public void SaveBackUp(string logContent, string name, string fileExt, string? secondPath = null)
    {
        try
        {
            // Use your FileExists method to check if we can write to the backup directory
            if (!Directory.Exists(_backupDirectory))
                Directory.CreateDirectory(_backupDirectory);

            string fileName = $"DimmerBackUp_{DateTime.Now:yyyy-MM-dd_HHmmss}_{name}.{fileExt}";
            string filePath = Path.Combine(_backupDirectory, fileName);

            lock (_logLock)
            {
                // For the primary path (assuming it's a regular path)
                File.WriteAllText(filePath, logContent);
            }

            if (!string.IsNullOrEmpty(secondPath))
            {
                lock (_logLock)
                {
                    if (secondPath.StartsWith("content://", StringComparison.OrdinalIgnoreCase))
                    {
                        // FIX: Don't construct URI with string concatenation!
                        // Use the platform hook that accepts folder URI + filename
                        if (TaggingUtils.PlatformGetStreamHook != null)
                        {
                            TaggingUtils.PlatformSpecificFileCreator(secondPath, fileName, logContent);
                        }
                    }
                    else
                    {
                        string secondFilePath = Path.Combine(secondPath, fileName);
                        File.WriteAllText(secondFilePath, logContent);
                    }
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
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(backupData, Newtonsoft.Json.Formatting.Indented);

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



    private DimmerPlayEventBackup ConvertToBackup(DimmerPlayEvent playEvent)
    {
        var songg = playEvent.SongsLinkingToThisEvent.FirstOrDefaultNullSafe();
        if(songg is null)
        {
            var tempRealm = RealmFactory.GetRealmInstance();
            songg = tempRealm.Find<SongModel>
                (playEvent.SongId);
            if (songg is not null)
            {
                tempRealm.Write(() =>
                {
                    if(!songg.PlayHistory.Contains(playEvent))
                        songg.PlayHistory.Add(playEvent);
                });
            }
        }
        return new DimmerPlayEventBackup
        {
            Id = playEvent.Id.ToString(),
            SongName = playEvent.SongName,
            ArtistName = songg?.ArtistName,
            AlbumName = songg?.AlbumName,
            TitleAndDurationKey = songg?.TitleDurationKey,
            CoverImagePath = songg?.CoverImagePath,
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


    public async Task<CompleteBackupData?> PickFolderTeRestoreFromBackupAsync(
        string filePath,
        IProgress<string>? progress = null)
    {
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
                return null;
            }

            if (filePath.Contains("CompleteBackup", StringComparison.OrdinalIgnoreCase))
            {
                progress?.Report("Loading song cache...");
                var songCache = await BuildSongCacheAsync(progress);

                progress?.Report("Reading backup file...");
                return await DeserializeBackupWithStreamingAsync(stream, songCache, progress);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error during restore: {ex}");
        }

        return null;
    }


    private async Task<Dictionary<string, SongModel>> BuildSongCacheAsync(IProgress<string>? progress = null)
    {
        return await Task.Run(() =>
        {
            try
            {
                var realm = RealmFactory.GetRealmInstance();
                var allSongs = realm.All<SongModel>().ToList();

                progress?.Report($"Caching {allSongs.Count} songs...");

                var cache = new Dictionary<string, SongModel>(
                    allSongs.Count,
                    StringComparer.OrdinalIgnoreCase);

                foreach (var song in allSongs)
                {
                    if (!string.IsNullOrEmpty(song.TitleDurationKey))
                    {
                        cache[song.TitleDurationKey] = song;
                    }
                }

                progress?.Report($"Cached {cache.Count} songs");
                return cache;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error building song cache: {ex}");
                return new Dictionary<string, SongModel>(StringComparer.OrdinalIgnoreCase);
            }
        }).ConfigureAwait(false);
    }

    private async Task<CompleteBackupData?> DeserializeBackupWithStreamingAsync(
        Stream stream,
        Dictionary<string, SongModel> songCache,
        IProgress<string>? progress = null)
    {
        var completeData = new CompleteBackupData();
        var dimmerPlayEvents = new List<DimmerPlayEventBackup>();

        using (var reader = new StreamReader(stream))
        using (var jsonReader = new JsonTextReader(reader))
        {
            int eventCount = 0;
            int processedCount = 0;

            while (await jsonReader.ReadAsync().ConfigureAwait(false))
            {
                if (jsonReader.TokenType == JsonToken.PropertyName &&
                    jsonReader.Value?.ToString() == "PlayEvents")
                {
                    await jsonReader.ReadAsync().ConfigureAwait(false); // Move to array start

                    if (jsonReader.TokenType == JsonToken.StartArray)
                    {
                        var serializer = new  Newtonsoft.Json.JsonSerializer();

                        while (await jsonReader.ReadAsync().ConfigureAwait(false) &&
                               jsonReader.TokenType != JsonToken.EndArray)
                        {
                            if (jsonReader.TokenType == JsonToken.StartObject)
                            {
                                var playEvent = serializer.Deserialize<DimmerPlayEventBackup>(jsonReader);
                                if (playEvent != null)
                                {
                                    dimmerPlayEvents.Add(playEvent);
                                    eventCount++;

                                    // Update progress every 1000 events
                                    if (eventCount % 1000 == 0)
                                    {
                                        progress?.Report($"Processed {eventCount} events...");
                                        await Task.Yield(); // Allow UI to update
                                    }
                                }
                            }
                        }
                    }
                }
            }

            progress?.Report($"Processing {eventCount} events with song cache...");

            // Update cover paths using cache
            foreach (var evt in dimmerPlayEvents)
            {
                if (!string.IsNullOrEmpty(evt.TitleAndDurationKey) &&
                    songCache.TryGetValue(evt.TitleAndDurationKey, out var songInDb))
                {
                    evt.CoverImagePath = songInDb.CoverImagePath;
                    processedCount++;

                    // Update progress every 1000 updates
                    if (processedCount % 1000 == 0)
                    {
                        progress?.Report($"Updated {processedCount}/{eventCount} events...");
                        await Task.Yield();
                    }
                }
            }

            progress?.Report($"Completed: {processedCount} events updated");
        }

        completeData.PlayEvents = dimmerPlayEvents;
        return completeData;
    }


    // Restore complete data to Realm
    public async Task<bool> RestoreCompleteDataAsync(CompleteBackupData data, RestoreResult result)
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
                Dictionary<string, SongModel> songsByKey=new();
                var songs = realm.All<SongModel>();
            foreach (var item in songs)
            {
                if(string.IsNullOrEmpty(item.TitleDurationKey))
                    return;
                if(songsByKey.ContainsKey(item.TitleDurationKey))
                {
                    // Handle duplicate key scenario if needed
                }
                else
                {
                    songsByKey[item.TitleDurationKey] = item;
                }
            }


            // Restore favorite songs
            if (data.FavoriteSongs?.Count >0)
            {
                foreach (var favView in data.FavoriteSongs)
                {
                    if (favView != null)
                    {
                        if (songsByKey.TryGetValue(favView.TitleDurationKey!, out var song))
                        {

                            song.IsFavorite = true;
                        }
                    }
                }

                result.FavoritesRestored = data.FavoriteSongs.Count;
            }

            // Restore play events
            if (data.PlayEvents?.Count > 0)
            {
                foreach (var backupEvent in data.PlayEvents)
                {
                    var newEvent = ConvertFromBackup(backupEvent);

                    if (newEvent != null)
                    {
                        var existingEvent = realm.Find<DimmerPlayEvent>(newEvent.Id);

                        if (existingEvent != null)
                        {
                            // Update the existing event's properties
                            if (backupEvent.TitleAndDurationKey is not null &&
                                songsByKey.TryGetValue(backupEvent.TitleAndDurationKey, out var song))
                            {
                                existingEvent.SongId = song.Id;
                                existingEvent.SongName = song.Title;

                                if (!song.PlayHistory.Contains(existingEvent))
                                {
                                    song.PlayHistory.Add(existingEvent);
                                }
                            }

                            // No need to add - it's already in the database
                        }
                        else
                        {
                            // This is a new event - set up its relationships
                            if (backupEvent.TitleAndDurationKey is not null &&
                                songsByKey.TryGetValue(backupEvent.TitleAndDurationKey, out var song))
                            {
                                newEvent.SongId = song.Id;
                                newEvent.SongName = song.Title;
                                song.PlayHistory.Add(newEvent);
                            }

                            // Add the new event to Realm
                            realm.Add(newEvent);
                        }
                    }
                }
                result.EventsRestored = data.PlayEvents.Count;
            }
        });

        return true;
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
            foreach (var backupEvent in events)
            {
                var newEvent = ConvertFromBackup(backupEvent);
                var eventInDb = realm.Find<DimmerPlayEvent>(newEvent.Id);
                if (eventInDb is null)
                {
                    eventInDb = newEvent;
                    var songInDb = realm.All<SongModel>().FirstOrDefaultNullSafe(x=>x.TitleDurationKey == backupEvent.TitleAndDurationKey);
                    songInDb?.PlayHistory.Add(eventInDb);
                    
                    realm.Add(eventInDb);
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
        if (!includeDefaultPath && string.IsNullOrEmpty(OptionalPath))
            return new List<string>();

        List<string> filesInDefaultPath = new();
        List<string> filesInSecondPath = new();

        // FIX: Handle optional path FIRST (this is the user-selected folder)
        if (!string.IsNullOrEmpty(OptionalPath))
        {
            var supportedExtensions = new HashSet<string> { ".json" };

            // Check if it's a content URI
            if (OptionalPath.StartsWith("content://", StringComparison.OrdinalIgnoreCase))
            {
                // Use platform-specific scanner for content URIs
                if (TaggingUtils.PlatformSpecificScanner != null)
                {
                    filesInSecondPath = TaggingUtils.PlatformSpecificScanner(OptionalPath, supportedExtensions);
                }
            }
            else
            {
                // Regular path
                filesInSecondPath = await TaggingUtils.GetAllFilesFromPathsAsync(
                    new List<string> { OptionalPath },
                    supportedExtensions);
            }
        }

        // THEN handle default path if requested
        if (includeDefaultPath && Directory.Exists(_backupDirectory))
        {
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

public class ObjectIdNullableConverter : Newtonsoft.Json.JsonConverter<ObjectId?>
{
   

    public override void WriteJson(Newtonsoft.Json.JsonWriter writer, ObjectId? value, Newtonsoft.Json.JsonSerializer serializer)
    {
        if (value.HasValue)
            writer.WriteValue(value.Value.ToString());
        else
            writer.WriteNull();
    }

    public override ObjectId? ReadJson(JsonReader reader, Type objectType, ObjectId? existingValue, bool hasExistingValue, Newtonsoft.Json.JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        var str = reader.Value?.ToString();
        return string.IsNullOrEmpty(str) ? null : new ObjectId(str);
    }
}
