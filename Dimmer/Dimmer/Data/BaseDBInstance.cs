using Dimmer.Interfaces.Services.Interfaces.FileProcessing.FileProcessorUtils;

using static Dimmer.Data.Models.LastFMUser;

namespace Dimmer.Data;


public interface IRealmFactory
{
    Realm GetRealmInstance();
    Realm? GetLogRInstance();
}

public class RealmFactory : IRealmFactory
{
    private readonly RealmConfiguration _config;
    private RealmConfiguration _logConfig;

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
        string fileLogPath = Path.Combine(dbPath, "DimmerDbBLog.realm");
#elif DEBUG
        string filePath = Path.Combine(dbPath, "DimmerDdebugB.realm");
        string fileLogPath = Path.Combine(dbPath, "DimmerDDebugBLog.realm");

#endif
        if (!TaggingUtils.FileExists(filePath))
        {
            AppUtils.IsUserFirstTimeOpening = true;
        }
      
        // Set schema version to 5.
        _config = new RealmConfiguration(filePath)
        {
            SchemaVersion = 21,
      
            MigrationCallback = (migration, oldSchemaVersion) =>
            {

            }
        };
        _logConfig = new RealmConfiguration(fileLogPath)
        {
            SchemaVersion = 1,
            MigrationCallback = (migration, oldSchemaVersion) =>
            {
            }
        };
    }

    public Realm GetRealmInstance()
    {
        return Realm.GetInstance(_config);
    }

    public Realm? GetLogRInstance()
    {
        return Realm.GetInstance(_logConfig);
    }
}

