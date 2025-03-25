

namespace Dimmer_MAUI.DataAccess.Services;
public class DataBaseService : IDataBaseService
{
    Realm? db;

    public required AppPreferencesModel ApplicationPreferences { get; set; }

    public void DeleteDB()
    {
        throw new NotImplementedException();
    }

    public RealmConfiguration GetRealm()
    {
        string dbPath;
#if ANDROID && NET9_0
        dbPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Dimmer";
#elif WINDOWS && NET9_0
        dbPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\DimmerDD";
#endif

        if (!Directory.Exists(dbPath))
        {
            Directory.CreateDirectory(dbPath);
        }
        
        string filePath = Path.Combine(dbPath, "DimmerDbB.realm");
        //File.Delete(filePath);
        var config = new RealmConfiguration(filePath)
        {
            SchemaVersion = 3,
        };

        return config;
    }

    public DataBaseService()
    {
        LoadAppPreference();
    }
    public void LoadAppPreference()
    {
        db = Realm.GetInstance(GetRealm());
        var ApplicationPreferencesList = db.All<AppPreferencesModel>().ToList();
        if (ApplicationPreferencesList is null || ApplicationPreferencesList.Count<1 )
        {

            ApplicationPreferences = new AppPreferencesModel();
            ApplicationPreferences.ShowCloseConfirmation = true;
            db.Write(() =>
            {
                db.Add(ApplicationPreferences);
            });
        }

    }

    public void SetAppPreference(AppPreferencesModel model)
    {
        db = Realm.GetInstance(GetRealm());
        
    }
}

