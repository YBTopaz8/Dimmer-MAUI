using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Interfaces.IServices;
public interface ILibraryScannerService
{
    Task<LoadSongsResult?> ScanLibraryAsync(List<string> folderPaths); // Full scan
    Task<LoadSongsResult?> ScanSpecificPathsAsync(List<string> pathsToScan, bool isIncremental = true); // Incremental for new files/folders
}
