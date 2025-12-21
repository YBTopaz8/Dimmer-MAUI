using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Dimmer.Utilities;

public static class StaticUtils
{
    
    
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _coverLocks = new();

    public static async Task WriteCoverSafeAsync(string path, byte[] bytes)
    {
        var sem = _coverLocks.GetOrAdd(path, _ => new SemaphoreSlim(1, 1));
        await sem.WaitAsync();
        try
        {
            await File.WriteAllBytesAsync(path, bytes);
        }
        finally
        {
            sem.Release();
        }
    }

}
