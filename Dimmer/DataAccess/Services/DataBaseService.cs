namespace Dimmer_MAUI.DataAccess.Services;
public class DataBaseService : IDataBaseService
{
    public void DeleteDB() => throw new NotImplementedException();

    public RealmConfiguration GetRealm()
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
        
        string filePath = Path.Combine(dbPath, "DimmerDbB.realm");
        //File.Delete(filePath);
        var config = new RealmConfiguration(filePath)
        {
            SchemaVersion = 0
        };
        
        return config;
    }

    private string GetGenreNameFromFile(string filePath)
    {
        return new Track(filePath).Genre;
    }
}

