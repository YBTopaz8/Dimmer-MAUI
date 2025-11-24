// ----------------------------
// RealmCoreRepo.cs
// ----------------------------
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
    [ThreadStatic] private static Realm? _realm;
    public Realm GetRealm() => _realm ??= factory.GetRealmInstance();


    #region Private Core Logic

    /// <summary>
    /// Core method to execute a write transaction safely on its own Realm instance.
    /// </summary>
    private void ExecuteWrite(Action<Realm> action)
    {
        GetRealm();
        try
        {
            _realm.Write(() => action(_realm));
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

        GetRealm();
        try
        {
            return function(_realm);
        }
        catch (RealmException ex)
        {
            Debug.WriteLine($"[RealmCoreRepo<{typeof(T).Name}>] A Realm error occurred during a read operation. Exception: {ex}");
            throw;
        }
    }

    private async Task<TResult> ExecuteReadAsync<TResult>(Func<Realm, Task<TResult>> function)
    {
        
        GetRealm();
        try
        {
            return await function(_realm);
        }
        catch (RealmException ex)
        {
            Debug.WriteLine($"[RealmCoreRepo<{typeof(T).Name}>] A Realm error occurred during an async read operation. Exception: {ex}");
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
    public T? Upsert(T? entity)
    {
        if (entity is null) return null;
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
            
            
            return realm.All<T>().AsEnumerable().Select(x=>x.Freeze()).ToList();
        });
    }

    /// <summary>
    /// Retrieves all objects of type T.
    /// </summary>
    /// <returns>A thread-safe, frozen, read-only collection of all objects.</returns>
    public async Task<IReadOnlyCollection<T>> GetAllAsync()
    {
        return await ExecuteReadAsync(async realm =>
        {
            await Task.Yield(); 
            return realm.All<T>().AsEnumerable().Select(x => x.Freeze()).ToList();
        });

    }

    public IQueryable<T> GetAllAsQueryable()
    {
        return ExecuteRead(realm =>
        {
            return realm.All<T>();
        });
    }

    public IQueryable<T> GetAllAsQueryableFiltered(string rqlQuery, params Realms.QueryArgument[] args)
    {
        return ExecuteRead(realm =>
        {
            return realm.All<T>().Filter(rqlQuery, args);
        });
    }

    public IQueryable<T> GetAllAsQueryableFiltered(Expression<Func<T, bool>> predicate)
    {
        return ExecuteRead(realm =>
        {
            return realm.All<T>().Where(predicate);
        });
    }

    public IQueryable<T> GetAllAsQueryableSorted<TKey>(Expression<Func<T, TKey>> keySelector, bool ascending = true)
    {
        return ExecuteRead(realm =>
        {
            var query = realm.All<T>();
            return ascending ? query.OrderBy(keySelector) : query.OrderByDescending(keySelector);
        });
    }

    public IQueryable<T> GetAllAsQueryableSortedFiltered<TKey>(Expression<Func<T, TKey>> keySelector, Expression<Func<T, bool>> predicate, bool ascending = true)
    {
        return ExecuteRead(realm =>
        {
            var query = realm.All<T>().Where(predicate);
            return ascending ? query.OrderBy(keySelector) : query.OrderByDescending(keySelector);
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

    public IObservable<IChangeSet<T>> Connect()
    {
        // The repository now manages the Realm instance and provides the stream
        
        return _realm.All<T>().AsObservableChangeSet();
    }

    /// <summary>
    /// Updates multiple objects in a single, efficient transaction.
    /// </summary>
    /// <param name="ids">An enumerable of ObjectIds to identify the objects to update.</param>
    /// <param name="updateAction">The action to perform on each found object within the transaction.</param>
    public void UpdateMany(IEnumerable<ObjectId> ids, Action<T> updateAction)
    {
        // We use ExecuteWrite to ensure the entire batch runs in one transaction
        // on a single, isolated Realm instance. This is both performant and thread-safe.
        ExecuteWrite(realm =>
        {
            foreach (var id in ids)
            {
                var liveEntity = realm.Find<T>(id);
                if (liveEntity != null)
                {
                    // The updateAction is invoked on the 'live' object
                    // that belongs to this specific transaction's Realm instance.
                    updateAction(liveEntity);
                }
            }
            Debug.WriteLine($"[RealmCoreRepo<{typeof(T).Name}>] Attempted to update {ids.Count()} entities.");
        });
    }

    /// <summary>
    /// Finds all objects matching a native RQL query string. This is the most flexible
    /// and reliable way to query Realm.
    /// </summary>
    /// <param name="rqlQuery">The RQL filter string (e.g., "ArtistName == $0 AND ReleaseYear > $1").</param>
    /// <param name="args">The arguments to substitute for the $0, $1, etc. placeholders.</param>
    /// <returns>A thread-safe, frozen list of matching objects.</returns>
    public List<T> QueryWithRQL(string rqlQuery, params Realms.QueryArgument[] args)
    {
        return ExecuteRead(realm =>
        {
            var results = realm.All<T>().Filter(rqlQuery, args).ToList();
            return results.Select(o => o.Freeze()).ToList();
        });
    }

    /// <summary>
    /// Finds the first object matching a native RQL query. Guaranteed to be supported by Realm.
    /// </summary>
    /// <param name="rqlQuery">The RQL filter string.</param>
    /// <param name="args">The arguments for the query.</param>
    /// <returns>A frozen, thread-safe copy of the first matching object, or null if not found.</returns>
    public T? FirstOrDefaultWithRQL(string rqlQuery, params Realms.QueryArgument[] args)
    {
        return ExecuteRead(realm =>
        {
            // .Filter() returns an IQueryable, so we still use LINQ's FirstOrDefault() here,
            // but the core filtering has already been done by the more reliable RQL engine.
            var obj = realm.All<T>().Filter(rqlQuery, args).FirstOrDefault();
            return obj?.Freeze();
        });
    }

    /// <summary>
    /// Checks for the existence of any object matching a native RQL query.
    /// </summary>
    /// <param name="rqlQuery">The RQL filter string.</param>
    /// <param name="args">The arguments for the query.</param>
    /// <returns>True if at least one matching object exists, otherwise false.</returns>
    public bool ExistsWithRQL(string rqlQuery, params Realms.QueryArgument[] args)
    {
        return ExecuteRead(realm =>
        {
            return realm.All<T>().Filter(rqlQuery, args).Any();
        });
    }

    /// <summary>
    /// A convenient "power method" to find an object by your business key (Title and Duration)
    /// using a pre-built, reliable RQL query.
    /// </summary>
    /// <param name="title">The title of the song.</param>
    /// <param name="duration">The duration of the song in seconds.</param>
    /// <returns>A frozen, thread-safe copy of the matching song, or null.</returns>
    public T? FindByTitleAndDuration(string title, double duration)
    {
        // This assumes your T is a SongModel. This might be better in a specific SongRepository.
        // For a generic repo, this is an example of a more specialized query.
        if (typeof(T) != typeof(SongModel))
        {
            throw new NotSupportedException("FindByTitleAndDuration is only supported for SongModel.");
        }

        string key = $"{title.ToLowerInvariant().Trim()}|{duration}";
        string rql = "TitleDurationKey == $0";

        return FirstOrDefaultWithRQL(rql, key);
    }


    // ========================================================================
    #region Public API - ASYNCHRONOUS Methods
    // ========================================================================

    /// <summary>
    /// Asynchronously creates a new object in the database. Ideal for UI threads.
    /// </summary>
    /// <returns>A Task representing the asynchronous operation, with the frozen, thread-safe copy of the new object.</returns>
    public async Task<T> CreateAsync(T entity)
    {
        if (entity.Id == ObjectId.Empty)
        {
            entity.Id = ObjectId.GenerateNewId();
        }

        // Realm's WriteAsync handles its own background thread and transaction.
        var frozenEntity = await _realm.WriteAsync(()=>
        {
            var managedEntity = _realm.Add(entity);
            // We must freeze the object inside the transaction to return it.
            return managedEntity.Freeze();
        });
        Debug.WriteLine($"[RealmCoreRepo<{typeof(T).Name}>] (Async) Created entity with Id: {entity.Id}");
        return frozenEntity;
    }

    /// <summary>
    /// Asynchronously updates an existing object or inserts it if it doesn't exist. Ideal for UI threads.
    /// </summary>
    /// <returns>A Task with the frozen, thread-safe copy of the upserted object.</returns>
    public async Task<T> UpsertAsync(T entity)
    {
        if (entity.Id == ObjectId.Empty)
        {
            return await CreateAsync(entity);
        }

        var frozenEntity = await _realm.WriteAsync(()=>
        {
            var managedEntity = _realm.Add(entity, update: true);
            return managedEntity.Freeze();
        });
        Debug.WriteLine($"[RealmCoreRepo<{typeof(T).Name}>] (Async) Upserted entity with Id: {entity.Id}");
        return frozenEntity;
    }

    /// <summary>
    /// Asynchronously and safely updates a specific object identified by its primary key.
    /// </summary>
    /// <returns>A Task with a boolean result: true if updated, false if not found.</returns>
    public async Task<bool> UpdateAsync(ObjectId id, Action<T> updateAction)
    {
        bool success = false;
        await _realm.WriteAsync(()=>
        {
            var liveEntity = _realm.Find<T>(id);
            if (liveEntity != null)
            {
                updateAction(liveEntity);
                success = true;
                Debug.WriteLine($"[RealmCoreRepo<{typeof(T).Name}>] (Async) Updated entity with Id: {id}");
            }
        });
        return success;
    }

    /// <summary>
    /// Asynchronously deletes an object from the database using its primary key.
    /// </summary>
    public async Task DeleteAsync(ObjectId id)
    {
        await _realm.WriteAsync(()=>
        {
            var liveEntity = _realm.Find<T>(id);
            if (liveEntity != null)
            {
                _realm.Remove(liveEntity);
                Debug.WriteLine($"[RealmCoreRepo<{typeof(T).Name}>] (Async) Deleted entity with Id: {id}");
            }
        });
    }

    /// <summary>
    /// Asynchronously retrieves all objects of type T. This operation is performed on a background thread.
    /// </summary>
    /// <returns>A Task with a thread-safe, frozen, read-only collection of all objects.</returns>
    public Task<IReadOnlyCollection<T>> GetAllAsync(bool IsShuffled = false)
    {
        // For read operations without a native async API, we use Task.Run
        // to push the synchronous work to a background thread.
        return Task.Run(() => GetAll(IsShuffled));
    }

    /// <summary>
    /// Asynchronously finds all objects matching a LINQ predicate on a background thread.
    /// </summary>
    /// <returns>A Task with a thread-safe, frozen list of matching objects.</returns>
    public Task<List<T>> QueryAsync(Expression<Func<T, bool>> predicate)
    {
        return Task.Run(() => Query(predicate));
    }

    /// <summary>
    /// Asynchronously finds all objects matching a native RQL query on a background thread.
    /// </summary>
    /// <returns>A Task with a thread-safe, frozen list of matching objects.</returns>
    public Task<List<T>> QueryWithRQLAsync(string rqlQuery, params Realms.QueryArgument[] args)
    {
        return Task.Run(() => QueryWithRQL(rqlQuery, args));
    }

    /// <summary>
    /// Asynchronously gets the total count of objects on a background thread.
    /// </summary>
    public Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
    {
        return Task.Run(() => Count(predicate));
    }

    public async Task DeleteManyAsync(HashSet<ObjectId> missingIds)
    {
        if (missingIds == null || missingIds.Count == 0)
        {
            return;
        }

        await _realm.WriteAsync(() =>
        {
            foreach (var id in missingIds)
            {
                var objectToDelete = _realm.Find<T>(id);
                if (objectToDelete != null)
                {

                    _realm.Remove(objectToDelete);
                }
            }
        });
    }

    #endregion
}