namespace Dimmer_MAUI.DataAccess.IServices;
public interface IDataBaseService
{
    RealmConfiguration GetRealm();
    void DeleteDB();
}
