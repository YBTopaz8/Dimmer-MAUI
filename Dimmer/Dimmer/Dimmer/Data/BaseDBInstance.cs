namespace Dimmer.Data;
public class BaseDBInstance
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
        //File.Delete(filePath);
        RealmConfiguration config = new RealmConfiguration(filePath)
        {
            SchemaVersion = 3,
        };
        return Realm.GetInstance(config);

    }
}
