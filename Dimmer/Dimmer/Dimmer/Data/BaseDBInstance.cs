namespace Dimmer.Data;


public interface IRealmFactory
{
    Realm GetRealmInstance();
}

public class RealmFactory : IRealmFactory
{
    private readonly RealmConfiguration _config;

    public RealmFactory()
    {
        // Create database directory.
        string dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DimmerDD");
        if (!Directory.Exists(dbPath))
        {
            Directory.CreateDirectory(dbPath);
        }
#if RELEASE
        string filePath = Path.Combine(dbPath, "DimmerDbB.realm");
#elif DEBUG

        string filePath = Path.Combine(dbPath, "DimmerDbDebug.realm");
#endif
        if (!File.Exists(filePath))
        {
            AppUtils.IsUserFirstTimeOpening = true;
        }
        // Set schema version to 5.
        _config = new RealmConfiguration(filePath)
        {
            SchemaVersion = 32,
            MigrationCallback = (migration, oldSchemaVersion) =>
            {


                if (oldSchemaVersion < 21) // or your relevant version
                {
                    var songs = migration.NewRealm.All<SongModel>();
                    var idGroups = songs.AsEnumerable().GroupBy(s => s.Id).Where(g => g.Count() > 1);

                    foreach (var group in idGroups)
                    {
                        // Keep only the first, remove the rest
                        bool first = true;
                        foreach (var song in group)
                        {
                            if (first)
                            {
                                first = false;
                                continue;
                            }
                            migration.NewRealm.Remove(song);
                        }
                    }
                }
            }
        };
    }

    public Realm GetRealmInstance()
    {
        return Realm.GetInstance(_config);
    }
}
