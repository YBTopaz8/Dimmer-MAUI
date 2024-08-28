namespace Dimmer.DataAccess.IServices;
public interface IDataBaseService
{
    Realm GetRealm();
    void DeleteDB();
}
