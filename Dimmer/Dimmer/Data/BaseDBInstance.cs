using Dimmer.Interfaces.Services.Interfaces.FileProcessing.FileProcessorUtils;

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
        string dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DimmerRealm");
        if (!Directory.Exists(dbPath))
        {
            Directory.CreateDirectory(dbPath);
        }
#if RELEASE
        string filePath = Path.Combine(dbPath, "DimmerDbB.realm");
#elif DEBUG
        string filePath = Path.Combine(dbPath, "DimmerDsB.realm");
        //string filePath = Path.Combine(dbPath, "DimmerDbDebug.realm");
#endif
        if (!TaggingUtils.FileExists(filePath))
        {
            AppUtils.IsUserFirstTimeOpening = true;
        }
      
        // Set schema version to 5.
        _config = new RealmConfiguration(filePath)
        {
            SchemaVersion = 14,
            MigrationCallback = (migration, oldSchemaVersion) =>
            {

            }
        };
    }

    public Realm GetRealmInstance()
    {
        return Realm.GetInstance(_config);
    }
}

