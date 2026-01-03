using Android.Graphics;
using AndroidX.Palette.Graphics;
using Dimmer.Data.ModelView;
using SkiaSharp;
using System.IO;

namespace Dimmer.NativeServices;

/// <summary>
/// Android implementation for rendering song story cards
/// </summary>
public class AndroidSongStoryCardRenderer
{
    private const int CardWidth = 1080;
    private const int CardHeight = 1920;
    private const int Padding = 60;
    private const int CoverSizeLarge = 800;
    private const int CoverSizeSmall = 400;
    private const int BrandingBottomMargin = 80;

    /// <summary>
    /// Generates a story card bitmap from song data
    /// </summary>
    public static async Task<string> GenerateCardAsync(SongStoryData storyData)
    {
        return await Task.Run(() =>
        {
            using var surface = SKSurface.Create(new SKImageInfo(CardWidth, CardHeight));
            var canvas = surface.Canvas;
            canvas.Clear(ToSKColor(storyData.BackgroundColor));

            bool hasLyrics = storyData.SelectedLyrics.Any();
            int coverSize = hasLyrics ? CoverSizeSmall : CoverSizeLarge;
            int currentY = Padding;

            // Draw cover image
            if (!string.IsNullOrEmpty(storyData.CoverImagePath) && File.Exists(storyData.CoverImagePath))
            {
                using var coverStream = File.OpenRead(storyData.CoverImagePath);
                using var coverBitmap = SKBitmap.Decode(coverStream);
                
                if (coverBitmap != null)
                {
                    int coverX = (CardWidth - coverSize) / 2;
                    var destRect = new SKRect(coverX, currentY, coverX + coverSize, currentY + coverSize);
                    
                    // Draw with rounded corners
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

            // Draw song title
            using (var titlePaint = new SKPaint
            {
                Color = ToSKColor(storyData.TextColor),
                TextSize = hasLyrics ? 56 : 72,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("sans-serif", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
            })
            {
                var titleLines = WrapText(storyData.Title, CardWidth - Padding * 2, titlePaint);
                foreach (var line in titleLines)
                {
                    float textWidth = titlePaint.MeasureText(line);
                    float x = (CardWidth - textWidth) / 2;
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
                Typeface = SKTypeface.FromFamilyName("sans-serif", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
            })
            {
                var artistLines = WrapText(storyData.ArtistName, CardWidth - Padding * 2, artistPaint);
                foreach (var line in artistLines)
                {
                    float textWidth = artistPaint.MeasureText(line);
                    float x = (CardWidth - textWidth) / 2;
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
                    Typeface = SKTypeface.FromFamilyName("serif", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Italic)
                };

                foreach (var lyric in storyData.SelectedLyrics)
                {
                    if (string.IsNullOrWhiteSpace(lyric)) continue;

                    var lyricLines = WrapText(lyric, CardWidth - Padding * 2, lyricsPaint);
                    foreach (var line in lyricLines)
                    {
                        float textWidth = lyricsPaint.MeasureText(line);
                        float x = (CardWidth - textWidth) / 2;
                        canvas.DrawText(line, x, currentY, lyricsPaint);
                        currentY += (int)lyricsPaint.TextSize + 10;
                    }
                    currentY += 15; // Extra space between lyric lines
                }
            }

            // Draw "Played on Dimmer" branding at bottom
            using (var brandingPaint = new SKPaint
            {
                Color = ToSKColor(storyData.TextColor.WithAlpha(0.6f)),
                TextSize = 36,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("sans-serif", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
            })
            {
                string branding = "Played on Dimmer";
                float textWidth = brandingPaint.MeasureText(branding);
                float x = (CardWidth - textWidth) / 2;
                float y = CardHeight - BrandingBottomMargin;
                canvas.DrawText(branding, x, y, brandingPaint);
            }

            // Save to file
            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            
            string outputPath = System.IO.Path.Combine(
                Android.App.Application.Context.CacheDir!.AbsolutePath,
                $"dimmer_story_{DateTime.Now.Ticks}.png"
            );
            
            using var stream = File.OpenWrite(outputPath);
            data.SaveTo(stream);
            
            return outputPath;
        });
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
