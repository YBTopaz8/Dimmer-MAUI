﻿namespace Dimmer.Data;


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

        // Set schema version to 5.
        _config = new RealmConfiguration(filePath)
        {
            SchemaVersion = 7,
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
            }
        };
    }

    public Realm GetRealmInstance() => Realm.GetInstance(_config);
}
