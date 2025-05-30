// ----------------------------
// RealmCoreRepo.cs
// ----------------------------
using Dimmer.Utilities.Extensions;
using Microsoft.Maui.Controls;
using Realms;
using System.Linq.Expressions;
using System.Reflection;

namespace Dimmer.Interfaces.Services;
public interface IRealmObjectWithObjectId
{
    [PrimaryKey]
    ObjectId Id { get; set; }
    bool IsNewOrModified { get; set; } // Property to check if the object is new or modified
}
/// <summary>
/// Thread‑safe Realm repo. Each call opens its own Realm;
/// holds its Realm open until you unsubscribe.
/// </summary>
public class RealmCoreRepo<T>(IRealmFactory factory) : IRepository<T> where T : RealmObject, IRealmObjectWithObjectId, new()
{
    private readonly IRealmFactory _factory = factory;
    private IMapper? _mapper;

    private Realm GetNewRealm() => _factory.GetRealmInstance();


    public T AddOrUpdate(T entity) // CORRECTED version
    {
        T frozenEntity; // Declare here to be accessible outside the using block

        using (var realmInstance = GetNewRealm())
        {
            T managedEntity = realmInstance.Write(() =>
            {
                return realmInstance.Add(entity, update: true);
            });

            frozenEntity = managedEntity.Freeze();

        }
        return frozenEntity;
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
        if (entity == null)
            return;

        using var realm = GetNewRealm();
        realm.Write(() =>
        {
            // Find the entity by its PK within the current realm transaction to ensure it's managed
            var liveEntity = realm.Find<T>(entity.Id);
            if (liveEntity != null)
            {
                realm.Remove(liveEntity);
                Debug.WriteLine($"Deleted {typeof(T).Name} with Id {entity.Id}");
            }
            else
            {
                Debug.WriteLine($"{typeof(T).Name} with Id {entity.Id} not found for deletion.");
            }
        });
    }
    //public void Delete(T entity)
    //{
    //    using var realm = GetNewRealm();
    //    realm.Write(() => realm.Remove(entity));
    //    Debug.WriteLine($"Deleted {nameof(entity)}");
    //}

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

        return (IRealmCollection<T>)realm.All<T>().Freeze();
    }
    public T? GetById(ObjectId primaryKey) // Input 'primaryKey' is a string
    {
        using var realmInstance = GetNewRealm();

        // If the PrimaryKey property on T is indeed of type string,
        // Realm's Find<T>(string pkValue) overload will be used directly.
        var obj = realmInstance.Find<T>(primaryKey);

        return obj?.Freeze(); // Return a frozen copy
    }
    public List<T> Query(Expression<Func<T, bool>> predicate)
    {
        using var realm = GetNewRealm();
        try
        {
            // Realm processes the predicate here. If unsupported, it throws.
            var results = realm.All<T>().Where(predicate).ToList();
            return results.Select(o => o.Freeze()).ToList();
        }
        catch (NotSupportedException ex)
        {
            // Provide more context to the developer about the failing predicate.
            string errorMessage = $"Realm LINQ query not supported by its provider. " +
                                  $"This usually means the predicate contains operations (like complex .Any() or .Contains() on collections) " +
                                  $"that Realm cannot translate to a native query. " +
                                  $"Consider simplifying the predicate for Realm and performing complex filtering in-memory after fetching an initial dataset. " +
                                  $"Failing Predicate: {predicate.ToString()}";
            Debug.WriteLine($"{errorMessage}\nException: {ex.ToString()}");
            throw new NotSupportedException(errorMessage, ex);
        }
        catch (Exception ex) // Catch other potential exceptions during query/materialization
        {
            Debug.WriteLine($"An unexpected error occurred during Realm query: {predicate.ToString()}\nException: {ex.ToString()}");
            throw; // Rethrow to allow higher-level error handling
        }
    }

    public List<T> GetPage(int skip, int take)
    {
        if (skip < 0)
            throw new ArgumentOutOfRangeException(nameof(skip), "Skip cannot be negative.");
        if (take <= 0)
            throw new ArgumentOutOfRangeException(nameof(take), "Take must be positive.");

        using var realm = GetNewRealm();
        var results = realm.All<T>().Skip(skip).Take(take).ToList();
        return results.Select(o => o.Freeze()).ToList();
    }
    public int Count(Expression<Func<T, bool>>? predicate = null)
    {
        using var realm = GetNewRealm();
        if (predicate == null)
            return realm.All<T>().Count();
        return realm.All<T>().Count(predicate);
    }
    public List<T> QueryOrdered<TKey>(Expression<Func<T, bool>> predicate, Expression<Func<T, TKey>> keySelector, bool ascending)
    {
        using var realm = GetNewRealm();
        var query = realm.All<T>().Where(predicate);
        query = ascending ? query.OrderBy(keySelector) : query.OrderByDescending(keySelector);
        return query.ToList().Select(o => o.Freeze()).ToList();
    }

    public IEnumerable<SongModel> Query(Expression<Func<DimmerPlayEvent, bool>> realmPredicate)
    {
        throw new NotImplementedException();
    }
}
// 1) A simple comparer for two song‐lists
class SongListComparer : IEqualityComparer<IList<SongModel>>
{

    public bool Equals(IList<SongModel>? a, IList<SongModel>? b)
    {
        if (ReferenceEquals(a, b))
            return true;
        if (a is null || b is null)
            return false;
        if (a.Count != b.Count)
            return false;
        for (int i = 0; i < a.Count; i++)
        {
            if (a[i].Id != b[i].Id || a[i].Title != b[i].Title)
                return false;
        }
        return true;
    }

    public int GetHashCode(IList<SongModel> obj)
    {
        if (obj == null)
            return 0;
        return obj.Count;
    }
}
