using Dimmer.Data.ModelView;
using SkiaSharp;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace Dimmer.WinUI.NativeServices;

/// <summary>
/// Windows implementation of song story sharing service
/// </summary>
public class WindowsSongStoryShareService : ISongStoryShareService
{
    private readonly ILogger<WindowsSongStoryShareService> _logger;

    public WindowsSongStoryShareService(ILogger<WindowsSongStoryShareService> logger)
    {
        _logger = logger;
    }

    public async Task<string> GenerateStoryCardAsync(SongStoryData storyData)
    {
        try
        {
            return await Task.Run(() =>
            {
                const int cardWidth = 1080;
                const int cardHeight = 1920;
                const int padding = 60;
                int coverSize = storyData.SelectedLyrics.Any() ? 400 : 800;
                int currentY = padding;

                using var surface = SKSurface.Create(new SKImageInfo(cardWidth, cardHeight));
                var canvas = surface.Canvas;
                canvas.Clear(ToSKColor(storyData.BackgroundColor));

                // Draw cover image
                if (!string.IsNullOrEmpty(storyData.CoverImagePath) && File.Exists(storyData.CoverImagePath))
                {
                    using var coverStream = File.OpenRead(storyData.CoverImagePath);
                    using var coverBitmap = SKBitmap.Decode(coverStream);

                    if (coverBitmap != null)
                    {
                        int coverX = (cardWidth - coverSize) / 2;
                        var destRect = new SKRect(coverX, currentY, coverX + coverSize, currentY + coverSize);

                        using var paint = new SKPaint
                        {
                            IsAntialias = true,
                            FilterQuality = SKFilterQuality.High
                        };

                        var roundRect = new SKRoundRect(destRect, 30, 30);
                        canvas.ClipRoundRect(roundRect, SKClipOperation.Intersect, true);
                        canvas.DrawBitmap(coverBitmap, destRect, paint);
                        canvas.ResetMatrix();
                        canvas.Restore();
                        canvas.Save();
                    }
                }

                currentY += coverSize + 60;

                bool hasLyrics = storyData.SelectedLyrics.Any();

                // Draw song title
                using (var titlePaint = new SKPaint
                {
                    Color = ToSKColor(storyData.TextColor),
                    TextSize = hasLyrics ? 56 : 72,
                    IsAntialias = true,
                    Typeface = SKTypeface.FromFamilyName("Segoe UI", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
                })
                {
                    var titleLines = WrapText(storyData.Title, cardWidth - padding * 2, titlePaint);
                    foreach (var line in titleLines)
                    {
                        float textWidth = titlePaint.MeasureText(line);
                        float x = (cardWidth - textWidth) / 2;
                        canvas.DrawText(line, x, currentY, titlePaint);
                        currentY += (int)titlePaint.TextSize + 20;
                    }
                }

                currentY += 20;

                // Draw artist name
                using (var artistPaint = new SKPaint
                {
                    Color = ToSKColor(storyData.TextColor.WithAlpha(0.8f)),
                    TextSize = hasLyrics ? 40 : 52,
                    IsAntialias = true,
                    Typeface = SKTypeface.FromFamilyName("Segoe UI", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
                })
                {
                    var artistLines = WrapText(storyData.ArtistName, cardWidth - padding * 2, artistPaint);
                    foreach (var line in artistLines)
                    {
                        float textWidth = artistPaint.MeasureText(line);
                        float x = (cardWidth - textWidth) / 2;
                        canvas.DrawText(line, x, currentY, artistPaint);
                        currentY += (int)artistPaint.TextSize + 15;
                    }
                }

                // Draw lyrics if present
                if (hasLyrics)
                {
                    currentY += 60;

                    using var lyricsPaint = new SKPaint
                    {
                        Color = ToSKColor(storyData.TextColor),
                        TextSize = 44,
                        IsAntialias = true,
                        Typeface = SKTypeface.FromFamilyName("Georgia", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Italic)
                    };

                    foreach (var lyric in storyData.SelectedLyrics)
                    {
                        if (string.IsNullOrWhiteSpace(lyric)) continue;

                        var lyricLines = WrapText(lyric, cardWidth - padding * 2, lyricsPaint);
                        foreach (var line in lyricLines)
                        {
                            float textWidth = lyricsPaint.MeasureText(line);
                            float x = (cardWidth - textWidth) / 2;
                            canvas.DrawText(line, x, currentY, lyricsPaint);
                            currentY += (int)lyricsPaint.TextSize + 10;
                        }
                        currentY += 15;
                    }
                }

                // Draw "Played on Dimmer" branding
                using (var brandingPaint = new SKPaint
                {
                    Color = ToSKColor(storyData.TextColor.WithAlpha(0.6f)),
                    TextSize = 36,
                    IsAntialias = true,
                    Typeface = SKTypeface.FromFamilyName("Segoe UI", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
                })
                {
                    string branding = "Played on Dimmer";
                    float textWidth = brandingPaint.MeasureText(branding);
                    float x = (cardWidth - textWidth) / 2;
                    float y = cardHeight - 80;
                    canvas.DrawText(branding, x, y, brandingPaint);
                }

                // Save to file
                using var image = surface.Snapshot();
                using var data = image.Encode(SKEncodedImageFormat.Png, 100);

                string outputPath = Path.Combine(
                    Path.GetTempPath(),
                    $"dimmer_story_{DateTime.Now.Ticks}.png"
                );

                using var stream = File.OpenWrite(outputPath);
                data.SaveTo(stream);

                return outputPath;
            });
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
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var dataTransferManager = DataTransferManager.GetForCurrentView();
                
                // Store the data to share
                string imagePathToShare = cardImagePath;
                string textToShare = shareText ?? "Shared from Dimmer";

                dataTransferManager.DataRequested += (sender, args) =>
                {
                    var request = args.Request;
                    request.Data.Properties.Title = "Share Song Story";
                    request.Data.Properties.Description = textToShare;
                    request.Data.SetText(textToShare);

                    // Add the image
                    var deferral = request.GetDeferral();
                    try
                    {
                        var imageFile = StorageFile.GetFileFromPathAsync(imagePathToShare).AsTask().Result;
                        var imageStream = Windows.Storage.Streams.RandomAccessStreamReference.CreateFromFile(imageFile);
                        request.Data.SetBitmap(imageStream);
                        request.Data.Properties.Thumbnail = imageStream;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error adding image to share data");
                    }
                    finally
                    {
                        deferral.Complete();
                    }
                };

                // Show the share UI
                DataTransferManager.ShowShareUI();
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sharing story");
            throw;
        }
    }

    public async Task<List<string>?> ShowLyricsSelectionAsync(List<string> allLyrics)
    {
        try
        {
            var tcs = new TaskCompletionSource<List<string>?>();

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var dialog = new Views.CustomViews.WinuiViews.LyricsSelectionDialog(allLyrics);
                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    tcs.SetResult(dialog.SelectedLyrics);
                }
                else
                {
                    tcs.SetResult(null);
                }
            });

            return await tcs.Task;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing lyrics selection dialog");
            return null;
        }
    }

    private static SKColor ToSKColor(Microsoft.Maui.Graphics.Color color)
    {
        return new SKColor(
            (byte)(color.Red * 255),
            (byte)(color.Green * 255),
            (byte)(color.Blue * 255),
            (byte)(color.Alpha * 255)
        );
    }

    private static List<string> WrapText(string text, float maxWidth, SKPaint paint)
    {
        var lines = new List<string>();
        var words = text.Split(' ');
        var currentLine = "";

        foreach (var word in words)
        {
            var testLine = string.IsNullOrEmpty(currentLine) ? word : $"{currentLine} {word}";
            var width = paint.MeasureText(testLine);

            if (width > maxWidth && !string.IsNullOrEmpty(currentLine))
            {
                lines.Add(currentLine);
                currentLine = word;
            }
            else
            {
                currentLine = testLine;
            }
        }

        if (!string.IsNullOrEmpty(currentLine))
        {
            lines.Add(currentLine);
        }

        return lines;
    }
}
