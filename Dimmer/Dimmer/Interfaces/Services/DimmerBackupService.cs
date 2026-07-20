using Newtonsoft.Json;
using System.IO.Compression;
using JsonReader = Newtonsoft.Json.JsonReader;
using JsonToken = Newtonsoft.Json.JsonToken;

namespace Dimmer.Interfaces.Services;
public class DimmerBackupService
{
    private readonly JsonSerializerSettings _jsonSettings;
    public static string? BackupDirectory { get; internal set; }
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

        BackupDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "DimmerBackUp");
    }


    public async Task<bool> SaveBackUpAsync(string logContent, string name, string fileExt, string? secondPath = null)
    {
        try
        {

            // 2. Serialize to JSON

            byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(logContent);

            // 3. Compress (GZip)
            byte[] compressedBytes;
            using (var outStream = new MemoryStream())
            {
                using (var archive = new GZipStream(outStream, CompressionLevel.Optimal))
                {
                    archive.Write(jsonBytes, 0, jsonBytes.Length);
                }
                compressedBytes = outStream.ToArray();
            }
            // Use your FileExists method to check if we can write to the backup directory
            if (!Directory.Exists(BackupDirectory))
                Directory.CreateDirectory(BackupDirectory);

            string fileName = $"DimmerBackUp_{DateTime.Now:yyyy-MM-dd_HHmmss}_{name}.{fileExt}";
            string filePath = Path.Combine(BackupDirectory, fileName);

            
                await File.WriteAllBytesAsync(filePath, compressedBytes);
            

            if (!string.IsNullOrEmpty(secondPath))
            {
               
                if (secondPath.StartsWith("content://", StringComparison.OrdinalIgnoreCase))
                {
                    TaggingUtils.PlatformSpecificFileCreator?.Invoke(secondPath, fileName, logContent);
                }
                else
                {
                    string secondFilePath = Path.Combine(secondPath, fileName);
                    await File.WriteAllBytesAsync(secondFilePath, compressedBytes);
                }
                
            }

            Debug.WriteLine($"Backup saved: {fileName} at {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Backup write failed: {ex}");
            return false;
        }
    }
    public class BackUpCompleteResult
    {
        public string AppVersion { get; set; } = string.Empty;
        public int FavoriteBackedUp { get; set; }
        public int PlayEventsBackedUp { get; set; }
        public DateTimeOffset BackupDate { get; set; }
        public bool IsBackUpComplete { get; set; }
    }
    // New method: Create complete backup
    public Task<BackUpCompleteResult>? CreateCompleteBackupAsync(string appVersion, string? exportPath = null)
    {
        return Task.Run(async () =>
        {

            try
            {
                var realm = RealmFactory.GetRealmInstance();

                var backupData = new CompleteBackupData
                {
                    AppState = realm.All<AppStateModel>().FirstOrDefaultNullSafe()?.ToAppStateModelView(),
                    FavoriteSongs = realm.All<SongModel>().Filter("IsFavorite == true").AsEnumerable().Select(x => x.ToSongModelView()).ToList(),
                    PlayEvents = realm.All<DimmerPlayEvent>().AsEnumerable().Select(ConvertToBackup).ToList(),
                    BackupDate = DateTime.UtcNow,
                    Version = appVersion
                };

                if (!Directory.Exists(BackupDirectory)) Directory.CreateDirectory(BackupDirectory!);

                // Note: Using a custom extension like .dimmerbak or .json.gz makes it clear it's compressed
                string fileName = $"DimmerBackUp_{DateTime.Now:yyyy-MM-dd_HHmmss}.json.gz";
                string filePath = Path.Combine(BackupDirectory!, fileName);

                // 🚀 STREAMED SERIALIZATION: Writes directly to disk through the GZip compressor
                await Task.Run(() =>
                {
                    using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
                    using var gzipStream = new GZipStream(fileStream, CompressionLevel.Optimal);
                    using var streamWriter = new StreamWriter(gzipStream, System.Text.Encoding.UTF8);
                    using var jsonWriter = new JsonTextWriter(streamWriter);

                    var serializer = Newtonsoft.Json.JsonSerializer.Create(_jsonSettings);
                    serializer.Serialize(jsonWriter, backupData);
                }).ConfigureAwait(false);

                // Optional: Copy to Android scoped storage if requested
                if (!string.IsNullOrEmpty(exportPath))
                {
                    // Implementation depends on your TaggingUtils, but ideally you copy the FileStream, not byte[]
                }

                return new BackUpCompleteResult { IsBackUpComplete = true, PlayEventsBackedUp = backupData.PlayEvents.Count };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Complete backup failed: {ex}");
                return new BackUpCompleteResult { IsBackUpComplete = false };
            }

        });
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
        if (string.IsNullOrEmpty(filePath)) return null;
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

            if (filePath.Contains(".json", StringComparison.OrdinalIgnoreCase))
            {
                progress?.Report("Loading song cache...");
                var songCache = await BuildSongCacheAsync(progress);

                progress?.Report("Reading backup file...");
                return await DeserializeJsonBackupWithStreamingAsync(stream, songCache, progress);
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


    private async Task<Dictionary<string, SongModelView>> BuildSongCacheAsync(IProgress<string>? progress = null)
    {
        return await Task.Run(() =>
        {
            try
            {
                var realm = RealmFactory.GetRealmInstance();
                var allSongs = realm.All<SongModel>().ToList();

                progress?.Report($"Caching {allSongs.Count} songs...");

                var cache = new Dictionary<string, SongModelView>(
                    allSongs.Count,
                    StringComparer.OrdinalIgnoreCase);

                foreach (var song in allSongs)
                {
                    if (!string.IsNullOrEmpty(song.TitleDurationKey))
                    {
                        cache[song.TitleDurationKey] = song.ToSongModelView()!;
                    }
                }

                progress?.Report($"Cached {cache.Count} songs");
                return cache;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error building song cache: {ex}");
                return new Dictionary<string, SongModelView>(StringComparer.OrdinalIgnoreCase);
            }
        }).ConfigureAwait(false);
    }

    private async Task<CompleteBackupData?> DeserializeJsonBackupWithStreamingAsync(
      Stream sourceStream,
      Dictionary<string, SongModelView> songCache,
      IProgress<string>? progress = null)
    {
        var completeData = new CompleteBackupData();
        var dimmerPlayEvents = new List<DimmerPlayEventBackup>();

        using var reader = new StreamReader(sourceStream, System.Text.Encoding.UTF8);
        using var jsonReader = new JsonTextReader(reader);

        int eventCount = 0;
        int processedCount = 0;
        var serializer = Newtonsoft.Json.JsonSerializer.Create(_jsonSettings);

        while (await jsonReader.ReadAsync().ConfigureAwait(false))
        {
            if (jsonReader.TokenType == JsonToken.PropertyName)
            {
                string propertyName = jsonReader.Value?.ToString() ?? string.Empty;

                // 1. Capture AppState (Single Object)
                if (propertyName == "AppState")
                {
                    await jsonReader.ReadAsync().ConfigureAwait(false); // Move to start of object
                    completeData.AppState = serializer.Deserialize<AppStateModelView>(jsonReader);
                }
                // 2. Capture FavoriteSongs (List)
                else if (propertyName == "FavoriteSongs")
                {
                    await jsonReader.ReadAsync().ConfigureAwait(false); // Move to start of array
                    completeData.FavoriteSongs = serializer.Deserialize<List<SongModelView>>(jsonReader);
                }
                // 3. Capture PlayEvents (Chunked stream-deserialization)
                else if (propertyName == "PlayEvents")
                {
                    await jsonReader.ReadAsync().ConfigureAwait(false); // Move to start of array

                    if (jsonReader.TokenType == JsonToken.StartArray)
                    {
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
                                        await Task.Yield(); // Allow UI thread to breathe/update
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        progress?.Report($"Processing {eventCount} events with song cache...");

        // Update cover paths using the in-memory cache
        foreach (var evt in dimmerPlayEvents)
        {
            if (!string.IsNullOrEmpty(evt.TitleAndDurationKey) &&
                songCache.TryGetValue(evt.TitleAndDurationKey, out var songInDb))
            {
                evt.CoverImagePath = songInDb.CoverImagePath;
                processedCount++;

                if (processedCount % 1000 == 0)
                {
                    progress?.Report($"Updated {processedCount}/{eventCount} events...");
                    await Task.Yield();
                }
            }
        }

        progress?.Report($"Completed: {processedCount} events updated");

        completeData.PlayEvents = dimmerPlayEvents;
        return completeData;
    }

    private async Task<CompleteBackupData?> DeserializeBackupWithStreamingAsync(
      Stream sourceStream,
      Dictionary<string, SongModelView> songCache,
      IProgress<string>? progress = null)
    {
        var completeData = new CompleteBackupData();
        var dimmerPlayEvents = new List<DimmerPlayEventBackup>();
        using var gzipStream = new GZipStream(sourceStream, CompressionMode.Decompress);
        using var reader = new StreamReader(gzipStream, System.Text.Encoding.UTF8);
        using var jsonReader = new JsonTextReader(reader);

        int eventCount = 0;
        int processedCount = 0;
        var serializer = Newtonsoft.Json.JsonSerializer.Create(_jsonSettings);

        while (await jsonReader.ReadAsync().ConfigureAwait(false))
        {
            if (jsonReader.TokenType == JsonToken.PropertyName)
            {
                string propertyName = jsonReader.Value?.ToString() ?? string.Empty;

                // 1. Capture AppState (Single Object)
                if (propertyName == "AppState")
                {
                    await jsonReader.ReadAsync().ConfigureAwait(false); // Move to start of object
                    completeData.AppState = serializer.Deserialize<AppStateModelView>(jsonReader);
                }
                // 2. Capture FavoriteSongs (List)
                else if (propertyName == "FavoriteSongs")
                {
                    await jsonReader.ReadAsync().ConfigureAwait(false); // Move to start of array
                    completeData.FavoriteSongs = serializer.Deserialize<List<SongModelView>>(jsonReader);
                }
                // 3. Capture PlayEvents (Chunked stream-deserialization)
                else if (propertyName == "PlayEvents")
                {
                    await jsonReader.ReadAsync().ConfigureAwait(false); // Move to start of array

                    if (jsonReader.TokenType == JsonToken.StartArray)
                    {
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
                                        await Task.Yield(); // Allow UI thread to breathe/update
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        progress?.Report($"Processing {eventCount} events with song cache...");

        // Update cover paths using the in-memory cache
        foreach (var evt in dimmerPlayEvents)
        {
            if (!string.IsNullOrEmpty(evt.TitleAndDurationKey) &&
                songCache.TryGetValue(evt.TitleAndDurationKey, out var songInDb))
            {
                evt.CoverImagePath = songInDb.CoverImagePath;
                processedCount++;

                if (processedCount % 1000 == 0)
                {
                    progress?.Report($"Updated {processedCount}/{eventCount} events...");
                    await Task.Yield();
                }
            }
        }

        progress?.Report($"Completed: {processedCount} events updated");

        completeData.PlayEvents = dimmerPlayEvents;
        return completeData;
    }

    // Restore complete data to Realm
    public async Task<RestoreResult> RestoreCompleteDataAsync(CompleteBackupData data, RestoreResult result)
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

        return result;
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
        if (includeDefaultPath && Directory.Exists(BackupDirectory))
        {
            var supportedExtensions = new HashSet<string> { ".json" };
            filesInDefaultPath = await TaggingUtils.GetAllFilesFromPathsAsync(
                new List<string> { BackupDirectory },
                supportedExtensions);
        }

        return filesInDefaultPath.Concat(filesInSecondPath).ToList();
    }
    // Delete old backups (optional)
    public void CleanupOldBackups(int keepLatest = 10)
    {
        try
        {
            if (!Directory.Exists(BackupDirectory))
                return;

            var files = Directory.GetFiles(BackupDirectory, "*.json")
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
