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

        string filePath = Path.Combine(dbPath, "DimmerDbB.realm");

        if (!File.Exists(filePath))
        {
            AppUtils.IsUserFirstTimeOpening = true;
        }
        // Set schema version to 5.
        _config = new RealmConfiguration(filePath)
        {
            SchemaVersion = 19,
            MigrationCallback = (migration, oldSchemaVersion) =>
            {
                
                if (oldSchemaVersion < 4)
                {
                    
                }
                
                if (oldSchemaVersion < 5)
                {
                   
                }
                if (oldSchemaVersion < 6)
                {
                    
                   
                }
                if (oldSchemaVersion < 7)
                {
                    
                   
                }
                if (oldSchemaVersion < 8)
                {
                    
                   
                }
                if (oldSchemaVersion < 9)
                {
                    
                   
                }
                if (oldSchemaVersion < 10)
                {
                    
                   
                }
                if (oldSchemaVersion < 11)
                {
                    
                   
                }
                if (oldSchemaVersion < 12)
                {
                    
                   
                }
            }
        };
    }

    public Realm GetRealmInstance()
    {
        return Realm.GetInstance(_config);
    }
}
