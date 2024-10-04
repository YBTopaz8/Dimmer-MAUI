namespace Dimmer_MAUI.DataAccess.Services;
public class DataBaseService : IDataBaseService
{
    public void DeleteDB() => throw new NotImplementedException();

    public Realm GetRealm()
    {
        string dbPath;
#if ANDROID
        dbPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Dimmer";
#elif WINDOWS
        dbPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\DimmerDD";
#endif

        if (!Directory.Exists(dbPath))
        {
            Directory.CreateDirectory(dbPath);
        }

        string filePath = Path.Combine(dbPath, "DimmerDB.realm");
     

        var config = new RealmConfiguration(filePath)
        {
            SchemaVersion = 3, // Increment schema version for each migration
            MigrationCallback = (migration, oldSchemaVersion) =>
            {
                var oldSongs = migration.OldRealm.DynamicApi.All("SongsModel");
                var newSongs = migration.NewRealm.All<SongsModel>();
                if (oldSchemaVersion < 2)
                {
                    // Migrate each song
                    foreach (var oldSong in oldSongs)
                    {
                        var newSong = newSongs.FirstOrDefault(s => s.Id == oldSong.DynamicApi.Get<ObjectId>("Id"));

                        if (newSong != null)
                        {
                            // Initialize DatesPlayed and DatesSkipped with empty lists if they are new
                            // Note: In Realm, these lists will be automatically initialized, so you don't need to set them explicitly
                            // Just ensure that the schema version is correct and the property is added
                            if (oldSchemaVersion < 2)
                            {
                                // Since these are new properties, they should be empty lists by default
                                // No need to manually set them here
                            }
                        }
                    }
                }
                if (oldSchemaVersion < 3)
                {
                    foreach (var oldSong in oldSongs)
                    {
                        var oldId = oldSong.DynamicApi.Get<ObjectId>("Id");
                        var newSong = newSongs.FirstOrDefault(s => s.Id == oldId);

                        if (newSong != null)
                        {
                            // Assign Genre by reading from the file
                            var filePath = oldSong.DynamicApi.Get<string?>("FilePath");
                            if (!string.IsNullOrEmpty(filePath))
                            {
                                try
                                {
                                    newSong.Genre = GetGenreNameFromFile(filePath);
                                }
                                catch (Exception ex)
                                {
                                    // Log the exception if necessary
                                    // Assign a default value
                                    newSong.Genre = "Unknown Genre";
                                }
                            }
                            else
                            {
                                newSong.Genre = "Unknown Genre";
                            }
                            newSong.Achievement = string.Empty;
                        }
                    }
                }
            }
        };


        return Realm.GetInstance(config);
    }

    private string GetGenreNameFromFile(string filePath)
    {
        return new Track(filePath).Genre;
    }
}

