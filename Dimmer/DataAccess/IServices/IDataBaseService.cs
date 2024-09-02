namespace Dimmer_MAUI.DataAccess.IServices;
public interface IDataBaseService
{
    Realm GetRealm();
    void DeleteDB();
}
