using Android.Content;
using AndroidX.Core.Content;
using Dimmer.Data.ModelView;
using AndroidX.Core.App;

namespace Dimmer.NativeServices;

/// <summary>
/// Android implementation of song story sharing service
/// </summary>
public class AndroidSongStoryShareService : ISongStoryShareService
{
    private readonly Context _context;
    private readonly ILogger<AndroidSongStoryShareService> _logger;

    public AndroidSongStoryShareService(ILogger<AndroidSongStoryShareService> logger)
    {
        _context = Android.App.Application.Context;
        _logger = logger;
    }

    public async Task<string> GenerateStoryCardAsync(SongStoryData storyData)
    {
        try
        {
            return await AndroidSongStoryCardRenderer.GenerateCardAsync(storyData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating story card");
            throw;
        }
    }

    public async Task ShareStoryAsync(string cardImagePath, string? shareText = null)
    {
        try
        {
            await Task.Run(() =>
            {
                var file = new Java.IO.File(cardImagePath);
                var uri = FileProvider.GetUriForFile(
                    _context,
                    $"{_context.PackageName}.fileprovider",
                    file
                );

                var shareIntent = new Intent(Intent.ActionSend);
                shareIntent.SetType("image/png");
                shareIntent.PutExtra(Intent.ExtraStream, uri);
                
                if (!string.IsNullOrEmpty(shareText))
                {
                    shareIntent.PutExtra(Intent.ExtraText, shareText);
                }

                shareIntent.AddFlags(ActivityFlags.GrantReadUriPermission);

                var chooserIntent = Intent.CreateChooser(shareIntent, "Share Song Story");
                chooserIntent!.AddFlags(ActivityFlags.NewTask);

                _context.StartActivity(chooserIntent);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sharing story");
            throw;
        }
    }

    public Task<List<string>?> ShowLyricsSelectionAsync(List<string> allLyrics)
    {
        var tcs = new TaskCompletionSource<List<string>?>();

        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                // Get the current activity
                var activity = Platform.CurrentActivity;
                if (activity == null)
                {
                    _logger.LogError("No current activity available for showing lyrics selection");
                    tcs.SetResult(null);
                    return;
                }

                var dialog = new ViewsAndPages.NativeViews.LyricsSelectionDialogFragment(
                    allLyrics,
                    selectedLyrics =>
                    {
                        tcs.SetResult(selectedLyrics);
                    }
                );

                var fragmentManager = ((AndroidX.AppCompat.App.AppCompatActivity)activity).SupportFragmentManager;
                dialog.Show(fragmentManager, "lyrics_selection");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing lyrics selection dialog");
                tcs.SetException(ex);
            }
        });

        return tcs.Task;
    }
}
