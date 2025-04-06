using System;
using System.IO;
using System.Linq;
using Realms;

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

        // Set schema version to 5.
        _config = new RealmConfiguration(filePath)
        {
            SchemaVersion = 5,
            MigrationCallback = (migration, oldSchemaVersion) =>
            {
                // Migration for schema version 4: set default for SomeProperty.
                if (oldSchemaVersion < 4)
                {
                    //var newAppStates = migration.NewRealm.All<AppStateModel>().ToList();
                    
                }
                // Migration for schema version 5: set default for IsFirstTime.
                if (oldSchemaVersion < 5)
                {
                    //var newAppStates = migration.NewRealm.All<AppStateModel>().ToList();
                   
                }
            }
        };
    }

    public Realm GetRealmInstance() => Realm.GetInstance(_config);
}
