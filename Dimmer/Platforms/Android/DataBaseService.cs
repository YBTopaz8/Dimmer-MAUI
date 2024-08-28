﻿namespace Dimmer.DataAccess;
public class DataBaseService : IDataBaseService
{
    public void DeleteDB()
    {        
        string dbPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\DimmerDB";
        string filePath = Path.Combine(dbPath, "DimmerDB.realm");
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            Debug.WriteLine("Deleted DB");
        }
    }

    public Realm GetRealm()
    {
        string dbPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Dimmerr";

        if(!Directory.Exists(dbPath))
        {
            Directory.CreateDirectory(dbPath);
        }

        string filePath = Path.Combine(dbPath, "Dimmer.realm");
        

        var config = new RealmConfiguration(filePath);

        return Realm.GetInstance(config);
    }
}
