using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Utilities.Enums;
public enum DimmerUtilityEnum
{
    FolderAdded,
    FolderRemoved,
    FileChanged,
    FolderReScanned,
    RefreshStats,
    Initialized,
    CoverImageDownload,
    LoadingSongs,
    Error,
    Failed,
    Previewing,
    LyricsLoad,

    DoneScanningData, SyncingData,
    Buffering,
    FolderNameChanged,
    FolderScanCompleted,
    FolderScanStarted,
    FolderWatchStarted,
    Opening,
}
