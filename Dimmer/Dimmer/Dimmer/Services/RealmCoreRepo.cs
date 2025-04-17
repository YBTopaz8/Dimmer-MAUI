using Dimmer.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Services;

public class RealmCoreRepo<T> : IRepository<T> where T : RealmObject, new()
{
    private readonly Realm _realm;

    public RealmCoreRepo(IRealmFactory factory)
        => _realm = factory.GetRealmInstance();

    public void AddOrUpdate(T entity)
        => _realm.Write(() => _realm.Add(entity, update: true));

    public void AddOrUpdate(IEnumerable<T> entities)
        => _realm.Write(() =>
        {
            foreach (var e in entities)
                _realm.Add(e, update: true);
        });

    public void Delete(T entity)
        => _realm.Write(() => _realm.Remove(entity));

    public void Delete(IEnumerable<T> entities)
        => _realm.Write(() =>
        {
            foreach (var e in entities)
                _realm.Remove(e);
        });

    public List<T> GetAll()
        => [.. _realm.All<T>()];

    public T? GetById(string primaryKey)
    {
        // Realm.Find<T> will return null if no PK or not found
        return _realm.Find<T>(primaryKey);
    }

    public List<T> Query(Expression<Func<T, bool>> predicate)
        => [.. _realm.All<T>().Where(predicate)];
    public IObservable<IList<T>> WatchAll()
    {
        var results = _realm.All<T>();
        return Observable.Create<IList<T>>(obs =>
        {
            obs.OnNext([.. results]);
            var token = results.SubscribeForNotifications((col, changes) =>
            {
                obs.OnNext([.. col]);
            });
            return () => token.Dispose();
        });
    }
    public List<T> GetPage(int skip, int take)
    => [.. _realm.All<T>().Skip(skip).Take(take)];
    public void BatchUpdate(Action<Realm> updates)
    => _realm.Write(() => updates(_realm));

}