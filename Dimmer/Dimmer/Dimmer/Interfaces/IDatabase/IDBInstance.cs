namespace Dimmer.Interfaces.IDatabase;
public interface IDBInstance
{
    public Realm GetRealm();
    public void LoadAppPreference();
    public void DeleteDB();

}
