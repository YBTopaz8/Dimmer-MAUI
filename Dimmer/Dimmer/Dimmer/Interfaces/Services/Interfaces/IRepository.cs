using System.Linq.Expressions;

namespace Dimmer.Interfaces.Services.Interfaces;
public interface IRepository<T> where T : new()
{
    T? AddOrUpdate(T entity);
    void AddOrUpdate(IEnumerable<T> entities);
    void BatchUpdate(Action<Realm> updates);
    void Delete(T entity);
    void Delete(IEnumerable<T> entities);

    IReadOnlyCollection<T> GetAll(bool IsShuffled = false);
    IRealmCollection<T> GetAllLive();
    T? GetById(ObjectId primaryKey);
    List<T> GetPage(int skip, int take);
    List<T> Query(Expression<Func<T, bool>> predicate);
    IEnumerable<SongModel> Query(Expression<Func<DimmerPlayEvent, bool>> realmPredicate);
    //IObservable<IList<T>> WatchAll();
    List<T> QueryOrdered<TKey>(Expression<Func<T, bool>> predicate, Expression<Func<T, TKey>> keySelector, bool ascending);
    int Count(Expression<Func<T, bool>> predicate);
    IReadOnlyCollection<T> GetAllUnfrozen(bool IsShuffled = false);
}