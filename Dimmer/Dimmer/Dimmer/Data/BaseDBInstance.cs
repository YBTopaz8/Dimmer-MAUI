namespace Dimmer.Data;
public static class BaseDBInstance
{
    public static Realm GetRealm()
    {
        string dbPath;
        dbPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\DimmerDD";
        if (!Directory.Exists(dbPath))
        {
            Directory.CreateDirectory(dbPath);
        }

        string filePath = Path.Combine(dbPath, "DimmerDbB.realm");
        
        RealmConfiguration config = new RealmConfiguration(filePath)
        {
            SchemaVersion = 3,
        };
        return Realm.GetInstance(config);

    }
}
public interface IRealmFactory
{
    Realm CreateRealm();
}

public class RealmFactory : IRealmFactory
{
    private readonly RealmConfiguration _config;

    public RealmFactory()
    {
        string dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DimmerDD");
        if (!Directory.Exists(dbPath))
        {
            Directory.CreateDirectory(dbPath);
        }

        string filePath = Path.Combine(dbPath, "DimmerDbB.realm");
        _config = new RealmConfiguration(filePath)
        {
            SchemaVersion = 3,
        };
    }

    public Realm CreateRealm() => Realm.GetInstance(_config);
}