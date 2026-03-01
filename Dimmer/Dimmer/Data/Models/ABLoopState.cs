namespace Dimmer.Data.Models;

/// <summary>
/// Represents the state of an A-B loop for repeating a section of a song.
/// </summary>
public class ABLoopState
{
    /// <summary>
    /// Gets or sets whether the A-B loop is currently enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the start position of the loop in seconds (Point A).
    /// </summary>
    public double? StartPosition { get; set; }

    /// <summary>
    /// Gets or sets the end position of the loop in seconds (Point B).
    /// </summary>
    public double? EndPosition { get; set; }

    /// <summary>
    /// Gets or sets the number of times to loop. 
    /// Null means infinite loop.
    /// </summary>
    public int? LoopCount { get; set; }

    /// <summary>
    /// Gets or sets the current iteration count.
    /// </summary>
    public int CurrentIteration { get; set; }

    /// <summary>
    /// Gets whether both points (A and B) have been set.
    /// </summary>
    public bool IsBothPointsSet => StartPosition.HasValue && EndPosition.HasValue;

    /// <summary>
    /// Gets whether the loop should continue based on loop count.
    /// </summary>
    public bool ShouldContinueLooping => !LoopCount.HasValue || CurrentIteration < LoopCount.Value;

    /// <summary>
    /// Resets the loop state.
    /// </summary>
    public void Reset()
    {
        IsEnabled = false;
        StartPosition = null;
        EndPosition = null;
        LoopCount = null;
        CurrentIteration = 0;
    }

    /// <summary>
    /// Increments the current iteration count.
    /// </summary>
    public void IncrementIteration()
    {
        CurrentIteration++;
    }
}
