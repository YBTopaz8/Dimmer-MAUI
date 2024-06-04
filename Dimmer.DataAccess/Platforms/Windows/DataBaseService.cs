using Dimmer.DataAccess.IServices;
using Realms;


namespace Dimmer.DataAccess;
public class DataBaseService : IDataBaseService
{
    public void DeleteDB() => throw new NotImplementedException();

    public Realm GetRealm()
    {
        string dbPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\DimmerDB";

        if (!Directory.Exists(dbPath))
        {
            Directory.CreateDirectory(dbPath);
        }

        string filePath = Path.Combine(dbPath, "DimmerDB.realm");
        var config = new RealmConfiguration(filePath);
        return Realm.GetInstance(config);
    }
}

