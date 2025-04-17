using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Interfaces;
public interface IFolderMonitorService : IDisposable
{
    void Start(IEnumerable<string> paths);
    void Stop();
    event Action<string> OnChanged;
}