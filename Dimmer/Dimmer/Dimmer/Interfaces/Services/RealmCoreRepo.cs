// ----------------------------
// RealmCoreRepo.cs
// ----------------------------
using Dimmer.Interfaces.Services.Interfaces;
using Dimmer.Utilities.Extensions;

using Realms.Exceptions;

using System.Linq.Expressions;

namespace Dimmer.Interfaces.Services;


// The interface remains the same, defining the contract for your objects.
public interface IRealmObjectWithObjectId
{
    [PrimaryKey]
    ObjectId Id { get; set; }
}

/// <summary>
/// A robust, thread-safe generic repository for Realm that enforces safe access patterns.
/// Each public method orchestrates its own Realm instance and transaction, ensuring thread isolation.
/// Public methods operate on ObjectIds or unmanaged objects, never managed RealmObjects from other threads.
/// </summary>
/// <typeparam name="T">The type of RealmObject, which must implement IRealmObjectWithObjectId.</typeparam>
public class RealmCoreRepo<T>(IRealmFactory factory) : IRepository<T> where T : RealmObject, IRealmObjectWithObjectId, new()
{
    private readonly IRealmFactory _factory = factory;

    #region Private Core Logic

    /// <summary>
    /// Core method to execute a write transaction safely on its own Realm instance.
    /// </summary>
    private void ExecuteWrite(Action<Realm> action)
    {
        using var realm = _factory.GetRealmInstance();
        try
        {
            realm.Write(() => action(realm));
        }
        catch (RealmException ex)
        {
            Debug.WriteLine($"[RealmCoreRepo<{typeof(T).Name}>] A Realm error occurred during a write transaction. Exception: {ex}");
            // Depending on your app's needs, you might want to re-throw or handle this.
            throw;
        }
    }

    /// <summary>
    /// Core method to execute a read operation and return a result safely on its own Realm instance.
    /// The result is frozen before being returned, making it thread-safe.
    /// </summary>
    private TResult ExecuteRead<TResult>(Func<Realm, TResult> function)
    {
        using var realm = _factory.GetRealmInstance();
        try
        {
            return function(realm);
        }
        catch (RealmException ex)
        {
            Debug.WriteLine($"[RealmCoreRepo<{typeof(T).Name}>] A Realm error occurred during a read operation. Exception: {ex}");
            throw;
        }
    }

    #endregion

    #region Public API - CRUD Operations

    /// <summary>
    /// Creates a new object in the database from an unmanaged instance.
    /// </summary>
    /// <param name="entity">An unmanaged instance of the object to create. Its Id can be empty.</param>
    /// <returns>The frozen, thread-safe copy of the newly created object.</returns>
    public T Create(T entity)
    {
        if (entity.Id == ObjectId.Empty)
        {
            entity.Id = ObjectId.GenerateNewId();
        }

        T frozenEntity = null!;
        ExecuteWrite(realm =>
        {
            var managedEntity = realm.Add(entity);
            frozenEntity = managedEntity.Freeze();
            Debug.WriteLine($"[RealmCoreRepo<{typeof(T).Name}>] Created new entity with Id: {entity.Id}");
        });
        return frozenEntity;
    }

    /// <summary>
    /// Updates an existing object or inserts it if it doesn't exist.
    /// </summary>
    /// <param name="entity">An unmanaged instance of the object to upsert.</param>
    /// <returns>The frozen, thread-safe copy of the upserted object.</returns>
    public T Upsert(T entity)
    {
        if (entity.Id == ObjectId.Empty)
        {
            // If no ID, it's a create operation.
            return Create(entity);
        }

        T frozenEntity = null!;
        ExecuteWrite(realm =>
        {
            var managedEntity = realm.Add(entity, update: true);
            frozenEntity = managedEntity.Freeze();
            Debug.WriteLine($"[RealmCoreRepo<{typeof(T).Name}>] Upserted entity with Id: {entity.Id}");
        });
        return frozenEntity;
    }

    /// <summary>
    /// Safely updates a specific object identified by its primary key.
    /// </summary>
    /// <param name="id">The ObjectId of the entity to update.</param>
    /// <param name="updateAction">An action that receives the live object and modifies it within the transaction.</param>
    /// <returns>True if the object was found and updated; otherwise, false.</returns>
    public bool Update(ObjectId id, Action<T> updateAction)
    {
        bool success = false;
        ExecuteWrite(realm =>
        {
            var liveEntity = realm.Find<T>(id);
            if (liveEntity != null)
            {
                updateAction(liveEntity);
                success = true;
                Debug.WriteLine($"[RealmCoreRepo<{typeof(T).Name}>] Updated entity with Id: {id}");
            }
            else
            {
                Debug.WriteLine($"[RealmCoreRepo<{typeof(T).Name}>] Could not find entity with Id {id} to update.");
            }
        });
        return success;
    }

    /// <summary>
    /// Deletes an object from the database using its primary key.
    /// </summary>
    /// <param name="id">The ObjectId of the entity to delete.</param>
    public void Delete(T Entity)
    {
        ObjectId id = Entity.Id;
        ExecuteWrite(realm =>
        {
            var liveEntity = realm.Find<T>(id);
            if (liveEntity != null)
            {
                realm.Remove(liveEntity);
                Debug.WriteLine($"[RealmCoreRepo<{typeof(T).Name}>] Deleted entity with Id: {id}");
            }
            else
            {
                Debug.WriteLine($"[RealmCoreRepo<{typeof(T).Name}>] Could not find entity with Id {id} to delete.");
            }
        });
    }

    /// <summary>
    /// Deletes multiple objects from the database using their primary keys.
    /// </summary>
    /// <param name="ids">An enumerable of ObjectIds to delete.</param>
    public void DeletMany(IEnumerable<ObjectId> ids)
    {
        ExecuteWrite(realm =>
        {
            foreach (var id in ids)
            {
                var liveEntity = realm.Find<T>(id);
                if (liveEntity != null)
                {
                    realm.Remove(liveEntity);
                }
            }
            Debug.WriteLine($"[RealmCoreRepo<{typeof(T).Name}>] Attempted to delete {ids.Count()} entities.");
        });
    }

    #endregion

    #region Public API - Read/Query Operations

    /// <summary>
    /// Retrieves a single object by its primary key.
    /// </summary>
    /// <param name="id">The ObjectId of the entity to find.</param>
    /// <returns>A frozen, thread-safe copy of the object, or null if not found.</returns>
    public T? GetById(ObjectId id)
    {
        return ExecuteRead(realm =>
        {
            var obj = realm.Find<T>(id);
            return obj?.Freeze();
        });
    }

    /// <summary>
    /// Retrieves all objects of type T.
    /// </summary>
    /// <returns>A thread-safe, frozen, read-only collection of all objects.</returns>
    public IReadOnlyCollection<T> GetAll(bool IsShuffled = false)
    {
        return ExecuteRead(realm =>
        {
            if (IsShuffled)
            {

                return realm.All<T>().ToList().Select(o => o.Freeze()).ToList().Shuffled();

            }
            return realm.All<T>().ToList().Select(o => o.Freeze()).ToList();
        });
    }

    /// <summary>
    /// Finds all objects matching a LINQ predicate.
    /// The query is executed against the database and the results are returned as a frozen list.
    /// </summary>
    /// <param name="predicate">The LINQ expression to filter the objects.</param>
    /// <returns>A thread-safe, frozen list of matching objects.</returns>
    public List<T> Query(Expression<Func<T, bool>> predicate)
    {
        return ExecuteRead(realm =>
        {
            try
            {
                var results = realm.All<T>().Where(predicate).ToList();
                return results.Select(o => o.Freeze()).ToList();
            }
            catch (NotSupportedException ex)
            {
                string errorMessage = $"[RealmCoreRepo<{typeof(T).Name}>] A LINQ predicate was not supported by Realm's provider. Predicate: {predicate}. Exception: {ex}";
                Debug.WriteLine(errorMessage);
                throw new NotSupportedException(errorMessage, ex);
            }
        });
    }

    /// <summary>
    /// Gets the total count of objects, optionally filtered by a predicate.
    /// </summary>
    public int Count(Expression<Func<T, bool>>? predicate = null)
    {
        return ExecuteRead(realm =>
        {
            var query = realm.All<T>();
            return predicate == null ? query.Count() : query.Count(predicate);
        });
    }

    #endregion
}