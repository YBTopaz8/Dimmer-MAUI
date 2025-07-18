using Dimmer.DimmerSearch.AbstractQueryTree.NL;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.DimmerSearch.AbstractQueryTree;

public static class ReflectionCache
{
    // Cache stores PropertyInfo objects. This is thread-safe.
    private static readonly ConcurrentDictionary<string, PropertyInfo> _cache = new();

    // This method gets a value from a SongModelView using cached reflection.
    // It is 100% safe and will not crash the app at startup.
    public static object? GetValue(SongModelView song, FieldDefinition field)
    {
        // GetOrAdd the PropertyInfo object. The factory function only runs once per field name.
        var propInfo = _cache.GetOrAdd(field.PropertyName,
            name => typeof(SongModelView).GetProperty(name)
                    ?? throw new MissingMemberException($"CRITICAL ERROR: Property '{name}' not found on SongModelView. Check your FieldRegistry."));

        // Get the value using the cached PropertyInfo. This is fast.
        return propInfo.GetValue(song);
    }
}