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
    void UpdateMany(IEnumerable<ObjectId> ids, Action<T> updateAction);
    bool ExistsWithRQL(string rqlQuery, params QueryArgument[] args);
    T? FirstOrDefaultWithRQL(string rqlQuery, params QueryArgument[] args);
    List<T> QueryWithRQL(string rqlQuery, params QueryArgument[] args);
    T? FindByTitleAndDuration(string title, double duration);
    Task<T> CreateAsync(T entity);
    Task<T> UpsertAsync(T entity);
    Task<bool> UpdateAsync(ObjectId id, Action<T> updateAction);
    Task DeleteAsync(ObjectId id);
    Task<IReadOnlyCollection<T>> GetAllAsync(bool IsShuffled = false);
    Task<List<T>> QueryAsync(Expression<Func<T, bool>> predicate);
    Task<List<T>> QueryWithRQLAsync(string rqlQuery, params QueryArgument[] args);
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);
    Task DeleteManyAsync(HashSet<ObjectId> missingIds);
}