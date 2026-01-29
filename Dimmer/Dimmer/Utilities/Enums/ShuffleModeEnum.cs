namespace Dimmer.Utilities.Enums;

/// <summary>
/// Defines the shuffle mode for playback
/// </summary>
public enum ShuffleMode
{
    /// <summary>
    /// Pure random shuffle with equal probability for all songs
    /// </summary>
    Random = 0,
    
    /// <summary>
    /// Weighted shuffle based on user behavior and ratings
    /// </summary>
    Weighted = 1
}
