using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Orchestration;
public class FolderMonitorService : IFolderMonitorService
{
    private readonly List<FileSystemWatcher> _watchers = new();
    public event Action<string> OnChanged;

    public void Start(IEnumerable<string> paths)
    {
        Stop();
        foreach (var p in paths)
        {
            if (!Directory.Exists(p))
                continue;
            var w = new FileSystemWatcher(p)
            {
                IncludeSubdirectories = true,
                EnableRaisingEvents = true
            };
            w.Changed += (_, e) => OnChanged?.Invoke(e.FullPath);
            _watchers.Add(w);
        }
    }

    public void Stop()
    {
        foreach (var w in _watchers)
        {
            w.EnableRaisingEvents = false;
            w.Dispose();
        }
        _watchers.Clear();
    }

    public void Dispose() => Stop();
}