using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Utilities.Extensions;

public static class RealmExtensions
{
    public static T? FirstOrDefaultNullSafe<T>(this IQueryable<T> query) where T : class
    {
        return query.Any() ? query.First() : null;
    }
 
    public static T? FirstOrDefaultNullSafe<T>(this IQueryable<T> query, Expression<Func<T, bool>> predicate) where T : class
    {
        return query.Any() ? query.First(predicate) : null;
    }
    public static T? LastOrDefaultNullSafe<T>(this IQueryable<T> query) where T : class
    {
        return query.Any() ? query.Last() : null;
    }
}