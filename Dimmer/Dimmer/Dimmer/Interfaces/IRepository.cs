using System.Linq.Expressions;

namespace Dimmer.Interfaces;
public interface IRepository<T>
{
    void AddOrUpdate(T entity);
    void AddOrUpdate(IEnumerable<T> entities);
    void BatchUpdate(Action<Realm> updates);
    void Delete(T entity);
    void Delete(IEnumerable<T> entities);
    List<T> GetAll();
    IRealmCollection<T> GetAllLive();
    T? GetById(string primaryKey);
    List<T> GetPage(int skip, int take);
    List<T> Query(Expression<Func<T, bool>> predicate);
    IObservable<IList<T>> WatchAll();
}