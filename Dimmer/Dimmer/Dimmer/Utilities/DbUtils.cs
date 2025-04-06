using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    /// <param name="existsCondition"></param>
    /// <param name="updateAction"></param>
    public static void AddOrUpdateSingleRealmItem<T>(Realm db, T item, Func<T, bool>? existsCondition = null, Action<T>? updateAction = null) where T : RealmObject
    {
        db.Write(() =>
        {
            if (existsCondition is null)
            {
                db.Add(item);
                return;
            }
            if (!db.All<T>().Any(existsCondition))
            {
                db.Add(item);
            }
            else
            {
                updateAction?.Invoke(item); // Perform additional updates if needed
                db.Add(item, update: true); // Update existing item

            }
        });
    }

}
