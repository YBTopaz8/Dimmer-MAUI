
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Utils;

/// <summary>
/// 
/// </summary>
public enum MediaPlayerState
{
    /// <summary>
    /// The stopped
    /// </summary>
    Stopped,
    /// <summary>
    /// The playing
    /// </summary>
    Playing,
    /// <summary>
    /// The paused
    /// </summary>
    Paused,
    /// <summary>
    /// The loading
    /// </summary>
    Loading,
    /// <summary>
    /// The error
    /// </summary>
    Error,
    /// <summary>
    /// The previewing
    /// </summary>
    Previewing,
    /// <summary>
    /// The lyrics load
    /// </summary>
    LyricsLoad,
    /// <summary>
    /// The show play BTN
    /// </summary>
    ShowPlayBtn,
    /// <summary>
    /// The show pause BTN
    /// </summary>
    ShowPauseBtn,
    /// <summary>
    /// The refresh stats
    /// </summary>
    RefreshStats,
    /// <summary>
    /// The initialized
    /// </summary>
    Initialized,
    /// <summary>
    /// The ended
    /// </summary>
    Ended,
    /// <summary>
    /// The cover image download
    /// </summary>
    CoverImageDownload,
    /// <summary>
    /// The loading songs
    /// </summary>
    LoadingSongs,
    /// <summary>
    /// The syncing data
    /// </summary>
    SyncingData,
    /// <summary>
    /// The done scanning data
    /// </summary>
    DoneScanningData,
    /// <summary>
    /// The play completed
    /// </summary>
    PlayCompleted,

}