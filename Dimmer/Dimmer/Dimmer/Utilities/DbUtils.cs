namespace Dimmer.Utilities;
public static class DbUtils
{

    // Generates a local device ID using the first three characters of CallerClass and a new GUID.
    public static string GenerateLocalDeviceID(string callerClass)
    {
        if (string.IsNullOrWhiteSpace(callerClass) || callerClass.Length < 3)
            throw new ArgumentException("CallerClass must be at least 3 characters long", nameof(callerClass));
        return $"{callerClass[..3]}{Guid.NewGuid()}";
    }

    /// <summary>
    /// Adds or updates a single item in the specified Realm database.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="db"></param>
    /// <param name="item"></param>
    /// <param name="IsAdd"></param>
    /// <param name="updateAction"></param>
    public static void AddOrUpdateSingleRealmItem<T>(Realm db, T item,bool IsAdd) where T : RealmObject
    {
        try
        {

            db.Write(() =>
            {
                var s = db.All<T>().ToList();
                if(s is null || s.Count < 1)
                { 
                    db.Add(item);
                    Debug.WriteLine("added");                    
                }
                else
                {
                    db.Add(item, update: true); // Update existing item
                    Debug.WriteLine("Updated2");
                }
            });
        }
        catch (Exception ex)
        {
            db.Write(() =>
            {
                db.Add(item, update: true); // Update existing item
                Debug.WriteLine("Updated crash");
            });
            Debug.WriteLine(ex.Message);
        }
    }

}
