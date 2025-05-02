// ----------------------------
// RealmCoreRepo.cs
// ----------------------------
using System.Linq.Expressions;
using Dimmer.Data;
using Dimmer.Utilities.Extensions;

namespace Dimmer.Services;

/// <summary>
/// Thread‑safe Realm repo. Each call opens its own Realm; WatchAll
/// holds its Realm open until you unsubscribe.
/// </summary>
public class RealmCoreRepo<T>(IRealmFactory factory) : IRepository<T> where T : RealmObject, new()
{
    private readonly IRealmFactory _factory = factory;

    // Helper to open a thread‑local Realm
    private Realm OpenRealm()
    {
        return _factory.GetRealmInstance();
    }

    public void AddOrUpdate(T entity)
    {
        using var realm = OpenRealm();
        realm.Write(() => realm.Add(entity, update: true));
        Debug.WriteLine($"UpSerted {nameof(entity)} {typeof(T)}");
    }

    public void AddOrUpdate(IEnumerable<T> entities)
    {
        using var realm = OpenRealm();
        realm.Write(() =>
        {
            foreach (var e in entities)
            {
                realm.Add(e, update: true);
            }
        });
    }

    public void Delete(T entity)
    {
        using var realm = OpenRealm();
        realm.Write(() => realm.Remove(entity));
        Debug.WriteLine($"Deleted {nameof(entity)}");
    }

    public void Delete(IEnumerable<T> entities)
    {
        using var realm = OpenRealm();
        realm.Write(() =>
        {
            foreach (var e in entities)
                realm.Remove(e);
        });
    }
    public void BatchUpdate(Action<Realm> updates)
    {
        using var realm = OpenRealm();
        realm.Write(() => updates(realm));
    }
    public IReadOnlyCollection<T> GetAll(bool IsShuffled = false)
    {
        using var realm = OpenRealm();
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
        return (IRealmCollection<T>)OpenRealm().All<T>();
    }

    public T? GetById(string primaryKey)
    {
        using var realm = OpenRealm();
        return realm.Find<T>(primaryKey);
    }

    public List<T> Query(Expression<Func<T, bool>> predicate)
    {
        using var realm = OpenRealm();
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
            var realm = OpenRealm();
            var results = realm.All<T>();

            // initial snapshot
            observer.OnNext([.. results]);

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
        using var realm = OpenRealm();
        return [.. realm.All<T>().Skip(skip).Take(take)];
    }

}
