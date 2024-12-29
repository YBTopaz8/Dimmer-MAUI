namespace Dimmer_MAUI.DataAccess.Services;
public class DataBaseService : IDataBaseService
{
    public void DeleteDB() => throw new NotImplementedException();

    public RealmConfiguration GetRealm()
    {
        string dbPath;
#if ANDROID && NET9_0
        dbPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Dimmer";
#elif WINDOWS && NET9_0
        dbPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\DimmerDD";
#endif

        if (!Directory.Exists(dbPath))
        {
            Directory.CreateDirectory(dbPath);
        }
        
        string filePath = Path.Combine(dbPath, "DimmerDbB.realm");
        //File.Delete(filePath);
        var config = new RealmConfiguration(filePath)
        {
            SchemaVersion = 3,
           MigrationCallback = (migration, oldSchemaVersion) =>
           {
               if (oldSchemaVersion < 3)
               {
                   var oldObjects = migration.OldRealm.DynamicApi.All("SongModel");
                   var newObjects = migration.NewRealm.DynamicApi.All("SongModel");

                   for (var i = 0; i < oldObjects.Count(); i++)
                   {
                       var oldObject = oldObjects.ElementAt(i);
                       var newObject = newObjects.ElementAt(i);
                       // Example of setting a new property
                       
                   }
               }
           }
        };

        return config;
    }

    private string GetGenreNameFromFile(string filePath)
    {
        return new Track(filePath).Genre;
    }
}

