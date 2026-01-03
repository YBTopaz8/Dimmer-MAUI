using System;
using System.Diagnostics;
using Android.Widget;
using Dimmer.Interfaces;
using Google.Android.Material.Dialog;

namespace Dimmer.Utils;

/// <summary>
/// Android-specific implementation of IUiErrorPresenter that shows error dialogs to users.
/// </summary>
public class AndroidErrorPresenter : IUiErrorPresenter
{
    public async Task ShowNotImplementedAlert(string message)
    {
        var activity = MainApplication.CurrentActivity;
        if (activity == null)
        {
            // If no activity is available, fall back to Toast notification
            ShowToast(message);
            return;
        }

        // Use TaskCompletionSource to convert callback-based dialog to async/await
        var tcs = new TaskCompletionSource<bool>();

        activity.RunOnUiThread(() =>
        {
            try
            {
                var builder = new MaterialAlertDialogBuilder(activity);
                builder.SetTitle("Feature Not Implemented");
                builder.SetMessage(message);
                builder.SetPositiveButton("OK", (s, e) => tcs.TrySetResult(true));
                builder.SetCancelable(true);
                
                var dialog = builder.Create();
                dialog.Show();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to show alert dialog: {ex.Message}");
                ShowToast(message);
                tcs.TrySetResult(false);
            }
        });

        await tcs.Task;
    }

    private static void ShowToast(string message)
    {
        var activity = MainApplication.CurrentActivity;
        if (activity == null)
        {
            Debug.WriteLine($"Cannot show toast: No activity available. Message was: {message}");
            return;
        }

        activity.RunOnUiThread(() =>
        {
            Toast.MakeText(activity, message, ToastLength.Long)?.Show();
        });
    }
}
