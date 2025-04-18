// ----------------------------
// RealmCoreRepo.cs
// ----------------------------
using System.Diagnostics;
using System.Linq.Expressions;
using Dimmer.Data;

namespace Dimmer.Services;

/// <summary>
/// Thread‑safe Realm repo. Each call opens its own Realm; WatchAll
/// holds its Realm open until you unsubscribe.
/// </summary>
public class RealmCoreRepo<T> : IRepository<T> where T : RealmObject, new()
{
    private readonly IRealmFactory _factory;
    public RealmCoreRepo(IRealmFactory factory) => _factory = factory;

    // Helper to open a thread‑local Realm
    private Realm OpenRealm() => _factory.GetRealmInstance();

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
                realm.Add(e, update: true);
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
    public List<T> GetAll()
    {
        using var realm = OpenRealm();
        return realm.All<T>()
            .AsEnumerable()
                   .Select(o => o.Freeze())
                   .ToList();
    }

    /// <summary>
    /// If you really need a live collection (e.g. for direct UI binding),
    /// cast explicitly and hold onto both the Realm and the IRealmCollection.
    /// </summary>
    public IRealmCollection<T> GetAllLive()
        => (IRealmCollection<T>)OpenRealm().All<T>();

    public T? GetById(string primaryKey)
    {
        using var realm = OpenRealm();
        return realm.Find<T>(primaryKey);
    }

    public List<T> Query(Expression<Func<T, bool>> predicate)
    {
        using var realm = OpenRealm();
        return realm.All<T>().Where(predicate).ToList();
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
            observer.OnNext(results.ToList());

            // live notifications
            var token = results.SubscribeForNotifications((col, changes) =>
            {
                observer.OnNext(col.Select(o => o.Freeze()).ToList());
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
        return realm.All<T>().Skip(skip).Take(take).ToList();
    }

}
