using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Utils;
public class ExceptionFilterPolicy
{
    /// <summary>
    /// Determines whether a given exception should be logged or ignored.
    /// This is the central place to add rules for "noisy" but expected exceptions.
    /// </summary>
    /// <param name="ex">The exception to check.</param>
    /// <returns>True if the exception should be logged, false if it should be ignored.</returns>
    public bool ShouldLog(Exception ex)
    {
        // --- Rule 1: Ignore TaskCanceledException from the MAUI Community Toolkit GestureManager ---
        // This is not an error. It happens when a user interaction cancels a UI animation
        // (e.g., moving the mouse off a button while the hover animation is running).
        if (ex is TaskCanceledException &&
            ex.StackTrace?.Contains("CommunityToolkit.Maui.Behaviors.GestureManager") == true)
        {
            // This is known noise, do not log it.
            return false;
        }
        // --- Rule 2: Ignore generic TaskCanceledExceptions with a minimal stack trace ---
        // This often occurs from rapidly starting/stopping async UI events where the call stack is very shallow.
        // It's a strong indicator of an expected cancellation, not a bug.
        if (ex is TaskCanceledException or OperationCanceledException)
        {
            var stackTrace = ex.StackTrace?.Trim();

            // Check if the stack trace is present and only contains the generic awaiter infrastructure.
            // A small line count is a good heuristic for this.
            if (!string.IsNullOrEmpty(stackTrace))
            {
                var lines = stackTrace.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length <= 2 && lines.First().Contains("System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess"))
                {
                    return false; // Generic, shallow cancellation noise.
                }
            }
        }


        // --- Rule 3: (Example) Ignore OperationCanceledException from a specific service ---
        // if (ex is OperationCanceledException && ex.StackTrace?.Contains("MyCancellableDataService") == true)
        // {
        //     return false;
        // }


        // --- Default Case ---
        // If no specific rule matched, we should log the exception.
        return true;
    }
}