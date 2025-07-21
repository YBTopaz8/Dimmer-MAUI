using System.Linq.Expressions;

namespace Dimmer.Data;


public interface IRealmFactory
{
    Realm GetRealmInstance();
}

public class RealmFactory : IRealmFactory
{
    private readonly RealmConfiguration _config;

    public RealmFactory()
    {
        // Create database directory.
        string dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DimmerRealm");
        if (!Directory.Exists(dbPath))
        {
            Directory.CreateDirectory(dbPath);
        }
#if RELEASE
        string filePath = Path.Combine(dbPath, "DimmerDbB.realm");
#elif DEBUG
        string filePath = Path.Combine(dbPath, "DimmerDbB.realm");
        //string filePath = Path.Combine(dbPath, "DimmerDbDebug.realm");
#endif
        if (!File.Exists(filePath))
        {
            AppUtils.IsUserFirstTimeOpening = true;
        }
        // Set schema version to 5.
        _config = new RealmConfiguration(filePath)
        {
            SchemaVersion = 44,
            MigrationCallback = (migration, oldSchemaVersion) =>
            {

            }
        };
    }

    public Realm GetRealmInstance()
    {
        return Realm.GetInstance(_config);
    }
}
public static class ExpressionBuilder
{
    /// <summary>
    /// Builds a dynamic LINQ expression for a "WHERE IN (...)" style query,
    /// which translates to "WHERE (p.Property == value1 || p.Property == value2 || ...)"
    /// </summary>
    /// <typeparam name="TElement">The type of the element (e.g., SongModel).</typeparam>
    /// <typeparam name="TValue">The type of the property value (e.g., ObjectId).</typeparam>
    /// <param name="propertySelector">An expression to select the property (e.g., p => p.Id).</param>
    /// <param name="values">The collection of values to match against.</param>
    /// <returns>A LINQ expression that can be used in a Where clause.</returns>
    public static Expression<Func<TElement, bool>> BuildOrExpression<TElement, TValue>(
        Expression<Func<TElement, TValue>> propertySelector,
        IEnumerable<TValue> values)
    {
        if (propertySelector == null)
            throw new ArgumentNullException(nameof(propertySelector));
        if (values == null || !values.Any())
            return element => false; // Or handle as needed

        var parameter = propertySelector.Parameters.Single();
        var body = values.Select(value => Expression.Equal(propertySelector.Body, Expression.Constant(value, typeof(TValue))))
                         .Aggregate(Expression.OrElse);

        return Expression.Lambda<Func<TElement, bool>>(body, parameter);
    }
}

