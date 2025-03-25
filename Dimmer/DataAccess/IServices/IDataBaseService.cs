namespace Dimmer_MAUI.DataAccess.IServices;
public interface IDataBaseService
{
    AppPreferencesModel ApplicationPreferences { get; }

    RealmConfiguration GetRealm();
    void DeleteDB();

    void LoadAppPreference();
    void SetAppPreference(AppPreferencesModel model);
}
