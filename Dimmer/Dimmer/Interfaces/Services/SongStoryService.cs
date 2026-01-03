using Dimmer.Data.ModelView;
using SkiaSharp;

namespace Dimmer.Interfaces.Services;

/// <summary>
/// Core implementation of song story service for preparing story data
/// </summary>
public class SongStoryService : ISongStoryService
{
    private readonly ILogger<SongStoryService> _logger;
    private static readonly Color FallbackColor = Color.FromArgb("#483D8B"); // darkslateblue

    public SongStoryService(ILogger<SongStoryService> logger)
    {
        _logger = logger;
    }

    public async Task<SongStoryData> PrepareSongStoryAsync(SongModelView song, List<string>? selectedLyrics = null)
    {
        try
        {
            var storyData = new SongStoryData
            {
                Title = song.Title ?? "Unknown",
                ArtistName = song.ArtistName ?? "Unknown Artist",
                AlbumName = song.AlbumName ?? "Unknown Album",
                CoverImagePath = song.CoverImagePath,
                SongId = song.Id.ToString(),
                HasLyrics = song.HasLyrics || song.HasSyncedLyrics
            };

            // Extract dominant color
            storyData.BackgroundColor = await ExtractDominantColorAsync(song.CoverImagePath);
            storyData.TextColor = GetContrastingTextColor(storyData.BackgroundColor);

            // Process lyrics if provided
            if (selectedLyrics != null && selectedLyrics.Any())
            {
                // Limit to 5 lines
                storyData.SelectedLyrics = selectedLyrics.Take(5).ToList();
            }

            return storyData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error preparing song story for {SongTitle}", song.Title);
            throw;
        }
    }

    public async Task<Color> ExtractDominantColorAsync(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
        {
            return FallbackColor;
        }

        try
        {
            return await Task.Run(() =>
            {
                using var stream = File.OpenRead(imagePath);
                using var bitmap = SKBitmap.Decode(stream);

                if (bitmap == null)
                {
                    return FallbackColor;
                }

                // Sample colors from the bitmap
                var colors = new Dictionary<uint, int>();
                int sampleRate = Math.Max(1, bitmap.Width / 20); // Sample every nth pixel

                for (int y = 0; y < bitmap.Height; y += sampleRate)
                {
                    for (int x = 0; x < bitmap.Width; x += sampleRate)
                    {
                        var pixel = bitmap.GetPixel(x, y);
                        
                        // Skip very light or very dark colors
                        if (pixel.Alpha < 128) continue;
                        
                        var luminance = 0.299 * pixel.Red + 0.587 * pixel.Green + 0.114 * pixel.Blue;
                        if (luminance < 20 || luminance > 235) continue;

                        // Quantize color to reduce variations
                        uint key = (uint)((pixel.Red / 32) << 16 | (pixel.Green / 32) << 8 | (pixel.Blue / 32));
                        
                        if (colors.ContainsKey(key))
                            colors[key]++;
                        else
                            colors[key] = 1;
                    }
                }

                if (colors.Count == 0)
                {
                    return FallbackColor;
                }

                // Get most common color
                var dominantKey = colors.OrderByDescending(c => c.Value).First().Key;
                
                // Reconstruct color from quantized value
                int r = (int)((dominantKey >> 16) & 0x7) * 32;
                int g = (int)((dominantKey >> 8) & 0x7) * 32;
                int b = (int)(dominantKey & 0x7) * 32;

                return Color.FromRgb(r, g, b);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting color from {ImagePath}", imagePath);
            return FallbackColor;
        }
    }

    public Color GetContrastingTextColor(Color backgroundColor)
    {
        // Calculate relative luminance using WCAG formula
        double r = backgroundColor.Red;
        double g = backgroundColor.Green;
        double b = backgroundColor.Blue;

        // Convert to linear RGB
        r = r <= 0.03928 ? r / 12.92 : Math.Pow((r + 0.055) / 1.055, 2.4);
        g = g <= 0.03928 ? g / 12.92 : Math.Pow((g + 0.055) / 1.055, 2.4);
        b = b <= 0.03928 ? b / 12.92 : Math.Pow((b + 0.055) / 1.055, 2.4);

        double luminance = 0.2126 * r + 0.7152 * g + 0.0722 * b;

        // Use white text for dark backgrounds, black for light backgrounds
        return luminance > 0.5 ? Colors.Black : Colors.White;
    }
}
