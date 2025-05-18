// ----------------------------
// RealmCoreRepo.cs
// ----------------------------
using Dimmer.Utilities.Extensions;
using Microsoft.Maui.Controls;
using System.Linq.Expressions;
using System.Reflection;

namespace Dimmer.Interfaces.Services;

/// <summary>
/// Thread‑safe Realm repo. Each call opens its own Realm; WatchAll
/// holds its Realm open until you unsubscribe.
/// </summary>
public class RealmCoreRepo<T>(IRealmFactory factory) : IRepository<T> where T : RealmObject, new()
{
    private readonly IRealmFactory _factory = factory;
    private IMapper? _mapper;

    private Realm GetNewRealm() => _factory.GetRealmInstance();

    public T AddOrUpdate(T entity)
    {
        using var realmInstance = GetNewRealm(); // Get a Realm for this operation
      var e =   realmInstance.Write(() =>
        {
            // Now 'realmInstance' is the same for both Write and Add
            return realmInstance.Add(entity, update: true);
        });

        return e;
        Debug.WriteLine($"UpSerted {nameof(entity)} {typeof(T)}");
    }

    public void AddOrUpdate(IEnumerable<T> entities)
    {
        using var realmInstance = GetNewRealm();
        realmInstance.Write(() =>
        {
            foreach (var e in entities)
            {
                realmInstance.Add(e, update: true);
            }
        });
    }


    public void Delete(T entity)
    {
        using var realm = GetNewRealm();
        realm.Write(() => realm.Remove(entity));
        Debug.WriteLine($"Deleted {nameof(entity)}");
    }

    public void Delete(IEnumerable<T> entities)
    {
        using var realm = GetNewRealm();

        realm.Write(() =>
        {
            foreach (var e in entities)
                realm.Remove(e);
        });
    }
    public void BatchUpdate(Action<Realm> updates)
    {
        using var realm = GetNewRealm();

        realm.Write(() => updates(realm));
    }
    public IReadOnlyCollection<T> GetAll(bool IsShuffled = false)
    {
        using var realm = GetNewRealm();

        var list = realm.All<T>()
            .ToList();
        // 2) shuffle in place if requested
        if (IsShuffled)
            list.ShuffleInPlace();  // returns void

        // 3) Freeze and materialize into a List<T> (List<T> implements IReadOnlyCollection<T>)
        var frozen = list
            .Select(o => o.Freeze())
            .ToList();              // now List<T>, which is IReadOnlyCollection<T>

        return frozen;
    }

    /// <summary>
    /// If you really need a live collection (e.g. for direct UI binding),
    /// cast explicitly and hold onto both the Realm and the IRealmCollection.
    /// </summary>
    public IRealmCollection<T> GetAllLive()
    {
        using var realm = GetNewRealm();

        return (IRealmCollection<T>)realm.All<T>();
    }
    public T? GetById(string primaryKey) // Input 'primaryKey' is a string
    {
        using var realmInstance = GetNewRealm();

        // Optional: A development-time check to verify the assumption that T's PK is a string.
        // You can remove this for production if performance is critical and you are certain.
#if DEBUG
        var pkPropertyInfo = typeof(T).GetProperties()
            .FirstOrDefault(p => p.GetCustomAttribute<PrimaryKeyAttribute>() != null);

        if (pkPropertyInfo == null)
        {
            Debug.WriteLine($"Warning: Type {typeof(T).Name} used with GetById does not have a [PrimaryKey] attribute. Find<T> might return null or behave unexpectedly.");
        }
        else if (pkPropertyInfo.PropertyType != typeof(string))
        {
            // This indicates a mismatch between the assumption ("PK is always string") and the actual model.
            // If this happens, the Find<T>(primaryKey) call below might fail or Realm might attempt an implicit conversion
            // which could lead to runtime errors if the string is not in the correct format for the actual PK type.
            Debug.WriteLine($"Warning: Type {typeof(T).Name} has a [PrimaryKey] of type {pkPropertyInfo.PropertyType.Name}, " +
                            $"but GetById(string) was called. Realm's Find<T>(string) will be used. " +
                            $"Ensure this is the intended behavior or that Realm handles the string-to-{pkPropertyInfo.PropertyType.Name} conversion for PKs.");
        }
#endif

        // If the PrimaryKey property on T is indeed of type string,
        // Realm's Find<T>(string pkValue) overload will be used directly.
        var obj = realmInstance.Find<T>(primaryKey);

        return obj?.Freeze(); // Return a frozen copy
    }
    public List<T> Query(Expression<Func<T, bool>> predicate)
    {
    
        using var realm = GetNewRealm();
        return [.. realm.All<T>().Where(predicate)];
    }

    /// <summary>
    /// Emits the full list immediately and on every change. Keeps its Realm open
    /// until you Dispose the subscription.
    /// </summary>
    public IObservable<IList<T>> WatchAll()
    {
        return Observable.Create<IList<T>>(observer =>
        {
            
            using var realm = GetNewRealm();

            var results = realm.All<T>();

            // initial snapshot
            observer.OnNext([.. results.AsEnumerable().Select(o => o.Freeze())]);

            // live notifications
            var token = results.SubscribeForNotifications((col, changes) =>
            {
                observer.OnNext([.. col.Select(o => o.Freeze())]);
            });

            // cleanup both token + realm on unsubscribe
            return () =>
            {
                token.Dispose();
                realm.Dispose();
            };
        });
    }

    public List<T> GetPage(int skip, int take)
    {
        using var realm = GetNewRealm();

        return [.. realm.All<T>().Skip(skip).Take(take)];
    }

}
// 1) A simple comparer for two song‐lists
class SongListComparer : IEqualityComparer<IList<SongModel>>
{
    public bool Equals(IList<SongModel>? a, IList<SongModel>? b)
    {
        if (a is null || b is null)
            return false;
        if (a.Count != b.Count)
            return false;
        for (int i = 0; i < a.Count; i++)
        {
            if (a[i].LocalDeviceId != b[i].LocalDeviceId ||
                a[i].Title         != b[i].Title)
                return false;
        }
        return true;
    }
    public int GetHashCode(IList<SongModel> obj)
    {
        // not used by DistinctUntilChanged
        return obj.Count;
    }
}
