namespace Dimmer.Data;


public interface IRealmFactory
{
    Realm GetRealmInstance();
    Realm? GetLogRInstance();
}
public partial class RealmFactory : IRealmFactory
{
    private readonly RealmConfiguration _config;
    private readonly RealmConfiguration _logConfig;

    public static string GetMainDatabasePath()
    {
        string dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "DimmerRealm");

        if (!Directory.Exists(dbPath))
            Directory.CreateDirectory(dbPath);

#if RELEASE
        return Path.Combine(dbPath, "DimmerDbB.realm");
#elif DEBUG
        return Path.Combine(dbPath, "DimmerDdebugB.realm");
#endif
    }

    public RealmFactory()
    {
        string filePath = GetMainDatabasePath();
        string dbPath = Path.GetDirectoryName(filePath)!;

#if RELEASE
        string fileLogPath = Path.Combine(dbPath, "DimmerDbBLog.realm");
#elif DEBUG
        string fileLogPath = Path.Combine(dbPath, "DimmerDDebugBLog.realm");
#endif

        if (!TaggingUtils.FileExists(filePath))
        {
            AppUtils.IsUserFirstTimeOpening = true;
        }

        _config = new RealmConfiguration(filePath)
        {
            SchemaVersion = 24,
            MigrationCallback = (migration, oldSchemaVersion) => { }
        };

        _logConfig = new RealmConfiguration(fileLogPath)
        {
            SchemaVersion = 2,
            MigrationCallback = (migration, oldSchemaVersion) => { }
        };
    }

    public Realm GetRealmInstance() => Realm.GetInstance(_config);
    public Realm? GetLogRInstance() => Realm.GetInstance(_logConfig);
}