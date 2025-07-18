using DynamicData;

using System.Linq.Expressions;

namespace Dimmer.Interfaces.Services.Interfaces;
public interface IRepository<T> where T : new()
{
    T? Upsert(T entity);
    void Delete(T entity);

    IReadOnlyCollection<T> GetAll(bool IsShuffled = false);
    T? GetById(ObjectId primaryKey);
    List<T> Query(Expression<Func<T, bool>> predicate);

    int Count(Expression<Func<T, bool>> predicate);
    void DeletMany(IEnumerable<ObjectId> ids);
    T Create(T entity);
    bool Update(ObjectId id, Action<T> updateAction);

    IObservable<IChangeSet<T>> Connect();
}