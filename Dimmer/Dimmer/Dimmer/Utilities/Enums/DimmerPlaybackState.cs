using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Utilities.Enums;
public enum DimmerPlaybackState
{
    Opening,
    Stopped,
    Playing,
    Paused,
    Loading,
    Error,
    Failed,
    Previewing,
    LyricsLoad,
    ShowPlayBtn,
    ShowPauseBtn,
    RefreshStats,
    Initialized,
    Ended,
    CoverImageDownload,
    LoadingSongs,
    SyncingData,
    Buffering,
    DoneScanningData,
    PlayCompleted,
    PlayPrevious,
    PlayNext,
}