using SkiaSharp;

namespace Dimmer.Utilities;
public static class AppUtils
{
    public static int UserScreenHeight { get; set; }
    public static int UserScreenWidth { get; set; }

    public static bool IsUserFirstTimeOpening { get; set; } = false;

    public static string GetWellFormattedSharingTextHavingSongStats(SongModelView selectedSong)
    {

        //Do a well formatted sharing text having song stats including
        //title, artist, album, duration, play count, last played date (humanized), and a link to the app.
        // and playcompletedcount , skipped, paused count, rank in album etc
        string title = string.IsNullOrWhiteSpace(selectedSong.Title) ? "Unknown Title" : selectedSong.Title;
        string artist = string.IsNullOrWhiteSpace(selectedSong.ArtistName) ? "Unknown Artist" : selectedSong.ArtistName;
        string album = string.IsNullOrWhiteSpace(selectedSong.AlbumName) ? "Unknown Album" : selectedSong.AlbumName;
        string duration = TimeUtils.FormatDuration(selectedSong.DurationInSeconds);
        string playCount = selectedSong.PlayCount > 0 ? selectedSong.PlayCount.ToString() : "0";
        string lastPlayed = selectedSong.LastPlayed.HasValue ? TimeUtils.HumanizeDate(selectedSong.LastPlayed.Value) : "Never";
        string playCompletedCount = selectedSong.PlayCompletedCount > 0 ? selectedSong.PlayCompletedCount.ToString() : "0";
        string skippedCount = selectedSong.SkipCount > 0 ? selectedSong.SkipCount.ToString() : "0";
        string pausedCount = selectedSong.PauseCount > 0 ? selectedSong.SkipCount.ToString() : "0";
        string rankInAlbum = selectedSong.RankInAlbum > 0 ? selectedSong.RankInAlbum.ToString() : "N/A";
            string seekCount = selectedSong.SeekCount > 0 ? selectedSong.SeekCount.ToString() : "0";
        string rankInArtist = selectedSong.RankInArtist > 0 ? selectedSong.RankInArtist.ToString() : "N/A";
        string numberOfFavs = selectedSong.NumberOfTimesFaved > 0 ? selectedSong.NumberOfTimesFaved.ToString() : "0";

        string appLink = "https://github.com/YBTopaz8/Dimmer-MAUI/releases/"; // Replace with your actual app link
        string shareText = $"🎵 Now Playing: \"{title}\" by {artist}\n" +
                           $"💿 Album: {album}\n" +
                           $"⏱ Duration: {duration}\n" +
                           $"▶️ Play Count: {playCount}\n" +
                           $"✅ Completed Plays: {playCompletedCount}\n" +
                           $"⏭ Skips: {skippedCount}\n" +
                           $"⏸ Pauses: {pausedCount}\n" +
                           $"🔄 Seeks: {seekCount}\n" +
                           $"🏆 Rank in Album: {rankInAlbum}\n" +
                           $"🏅 Rank in Artist: {rankInArtist}\n" +
                           $"❤️ Favorited: {numberOfFavs} times\n" +
                           $"🕒 Last Played: {lastPlayed}\n\n" +
                            $"Listen on Dimmer: {appLink}\n" +
                           $"Shared via Dimmer";
        return shareText;


    }
}
public static class UserFriendlyLogGenerator
{
    public static string GetPlaybackStateMessage(PlayType? type, SongModelView? currentSong, double? position = null)
    {
        if (type == null || currentSong == null)
        {
            return "Playback state is unknown or song information is missing.";
        }
        // Gracefully handle if currentSong or its Title is null/empty
        string songTitle = !string.IsNullOrWhiteSpace(currentSong?.Title) ? $"\"{currentSong.Title}\"" : "the current track";
        string artistName = !string.IsNullOrWhiteSpace(currentSong?.OtherArtistsName) ? $" by {currentSong.OtherArtistsName}" : ""; // Optional: Add artist if available

        // Combine title and artist for a richer description
        string fullSongDescription = $"{songTitle}{artistName}";

        switch (type)
        {
            case PlayType.Play:
                return $"Now playing: {fullSongDescription}.";
            case PlayType.Pause:
                return $"Paused: {fullSongDescription}.";
            case PlayType.Resume:
                return $"Resumed: {fullSongDescription}.";
            case PlayType.Completed:
                return $"{fullSongDescription} finished playing.";
            case PlayType.Seeked:
                string timePosition = position.HasValue ? TimeSpan.FromSeconds(position.Value).ToString(@"mm\:ss") : "a new position";
                return $"Seeked to {timePosition} in {fullSongDescription}.";
            case PlayType.Skipped:
                return $"Skipped: {fullSongDescription}.";
            case PlayType.Restarted:
                return $"Restarted: {fullSongDescription}.";
            case PlayType.SeekRestarted: // Potentially more specific than just Restarted
                return $"Restarted {fullSongDescription} from the beginning.";
            case PlayType.CustomRepeat: // Assuming this means looping/repeating the current song
                return $"Looping: {fullSongDescription}.";
            case PlayType.Previous:
                return $"Playing previous track: {fullSongDescription}."; // Assumes currentSong is now the previous track

            // Chat related (could be more specific if song context is relevant, otherwise generic)
            case PlayType.ChatSent:
                return "Message sent.";
            case PlayType.ChatReceived:
                return "New message received.";
            // Add more chat types as needed with user-friendly messages...
            // e.g., "Message deleted.", "Your message was pinned."

            // Playlist and Sharing
            case PlayType.AddToPlaylist: // Note: Your enum has 37 for both Add and Remove. I'll assume one for now.
                                         // If they are distinct, you'll need another enum member.
                return $"Added {fullSongDescription} to playlist.";
            // case PlayType.RemoveFromPlaylist: // If you have a distinct enum value
            //    return $"Removed {fullSongDescription} from playlist.";
            case PlayType.ShareSong:
                return $"Sharing {fullSongDescription}.";
            case PlayType.ReceiveShare:
                return $"Received shared song: {fullSongDescription}.";

            case PlayType.LogEvent: // Generic, as "LogEvent" is internal-facing
                return "An event was recorded.";

            default:
                // Fallback for any PlayType not explicitly handled
                // Capitalize the first letter of the enum name and add spaces before other capitals for readability
                string formattedType = System.Text.RegularExpressions.Regex.Replace(type.ToString(), "([A-Z])", " $1").TrimStart();
                return $"{formattedType} event for {fullSongDescription}.";
        }
    }


}

public static class ImageResizer
{

    /// <summary>
    /// Extracts the most dominant, vibrant color from an image, suitable for UI accenting.
    /// Ignores blacks, whites, and grays.
    /// </summary>
    /// <param name="imageData">The byte array of the image.</param>
    /// <param name="defaultColor">A fallback color if no suitable color is found.</param>
    /// <returns>The dominant SKColor.</returns>
    public async static Task<SKColor?> GetDominantColor(string coverfilePath, SKColor? defaultColor = null)
    {  // --- Step 1: Validate the input ---
        if (string.IsNullOrWhiteSpace(coverfilePath) || !File.Exists(coverfilePath))
        {
            // Log an error or warning here if you want
            return null;
        }

        try
        {
            // --- Step 2: Read the file data asynchronously ---
            var imageData = await File.ReadAllBytesAsync(coverfilePath);
            var palette = GetVibrantPalette(imageData, 1);
            return palette.Count != 0 ? palette.First() : (defaultColor ?? SKColors.Gray);  
       }
        catch(Exception ex)
        {
            // --- Step 5: Handle potential errors gracefully ---
            // This could happen if the file is not a valid image, is corrupted, etc.
            Debug.WriteLine($"Error getting dominant color for {coverfilePath}: {ex.Message}");
            return null; // Always return a fallback value on error.
       }
    
    }
    /// <summary>
    /// Asynchronously extracts the dominant color from an image file.
    /// This is the recommended version to avoid blocking the UI thread.
    /// </summary>
    /// <param name="coverfilePath">The full path to the image file.</param>
    /// <returns>A MAUI Color object, or null if a color cannot be determined.</returns>
    public static async Task<Color?> GetDominantMauiColorAsync(string coverfilePath, float opacity=0.7f)
    {
        // --- Step 1: Validate the input ---
        if (string.IsNullOrWhiteSpace(coverfilePath) || !File.Exists(coverfilePath))
        {
            // Log an error or warning here if you want
            return null;
        }

        try
        {
            // --- Step 2: Read the file data asynchronously ---
            var imageData = await File.ReadAllBytesAsync(coverfilePath);

            // If GetVibrantPalette also has an async version, you should use that here!
            var palette = GetVibrantPalette(imageData, 1);

            // --- Step 4: Check the result and convert the color ---
            if (palette != null && palette.Count > 0)
            {
                var dominantColorFromPalette = palette.First();

                // Create the MAUI color from the standard byte values.
                var mauiColor = Color.FromRgb(
                    dominantColorFromPalette.Red,
                    dominantColorFromPalette.Green,
                    dominantColorFromPalette.Blue
                ).WithAlpha(opacity);
                

                return mauiColor;
            }

            // If no dominant color was found, return null.
            return null;
        }
        catch (Exception ex)
        {
            // --- Step 5: Handle potential errors gracefully ---
            // This could happen if the file is not a valid image, is corrupted, etc.
            Debug.WriteLine($"Error getting dominant color for {coverfilePath}: {ex.Message}");
            return null; // Always return a fallback value on error.
        }
    }
    /// <summary>
    /// Extracts a palette of the most common vibrant colors from an image.
    /// </summary>
    /// <param name="imageData">The byte array of the image.</param>
    /// <param name="paletteSize">The desired number of colors in the palette.</param>
    /// <returns>A list of vibrant SKColors.</returns>
    public static List<SKColor> GetVibrantPalette(byte[]? imageData, int paletteSize = 5)
    {
        if (imageData == null || imageData.Length == 0)
            return new List<SKColor>();

        try
        {
            using var original = SKBitmap.Decode(imageData);
            if (original == null)
                return new List<SKColor>();

            // Downscale for performance. 100x100 is plenty for color analysis.
            var info = new SKImageInfo(100, 100);
            
            using var bitmap = original.Resize(info, SKSamplingOptions.Default);

            var colorCounts = new Dictionary<SKColor, int>();
            var distinctColors = new Dictionary<uint, SKColor>();

            for (int x = 0; x < bitmap.Width; x++)
            {
                for (int y = 0; y < bitmap.Height; y++)
                {
                    SKColor color = bitmap.GetPixel(x, y);

                    // --- Filter out non-vibrant colors ---
                    color.ToHsv(out _, out float saturation, out float value);
                    if ((saturation < 0.05f && value > 0.95f) || value < 0.05f) // Ignore near-whites and near-blacks
                    {
                        continue;
                    }

                    int r = (color.Red >> 3) << 3;
                    int g = (color.Green >> 3) << 3;
                    int b = (color.Blue >> 3) << 3;

                    // Create a unique key for this quantized color
                    uint colorKey = (uint)((r << 16) | (g << 8) | b);

                    colorCounts.TryGetValue(colorKey, out int currentCount);
                    colorCounts[colorKey] = currentCount + 1;

                    // Store the original average color for this quantized group
                    if (!distinctColors.ContainsKey(colorKey))
                    {
                        distinctColors[colorKey] = new SKColor((byte)r, (byte)g, (byte)b);
                    }
                }
            }

            // Order by most frequent and take the top N.
            return colorCounts.OrderByDescending(kvp => kvp.Value)
                              .Select(kvp => kvp.Key)
                              .Take(paletteSize)
                              .ToList();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting color palette: {ex.Message}");
            return new List<SKColor>();
        }
    }
    
    public async static Task<byte[]?> ResizeImage(string coverfilePath, int maxDimension = 1400, int quality = 95)
    {
        if (string.IsNullOrWhiteSpace(coverfilePath) || !File.Exists(coverfilePath))
        {
            // Log an error or warning here if you want
            return null;
        }
        try
        {
            var originalImageData = await File.ReadAllBytesAsync(coverfilePath);
            if (originalImageData == null || originalImageData.Length == 0)
            {
                return null;
            }
            if (originalImageData.Length < 32) // A very rough heuristic
            {
                Debug.WriteLine("Warning: Input image data is too short to be a valid image.");
                return null;
            }
            if (!IsLikelyImageFormat(originalImageData))
            {
                Debug.WriteLine("Warning: Input image data does not appear to be a recognized image format.");
                return null;
            }

            // Decode the original image data into a SkiaSharp bitmap
            using var originalBitmap = SKBitmap.Decode(originalImageData);

            if (originalBitmap == null)
            {
                // SkiaSharp couldn't decode the image.
                return null;
            }

            // Figure out the new dimensions while preserving aspect ratio
            int newWidth, newHeight;
            if (originalBitmap.Width > originalBitmap.Height)
            {
                newWidth = maxDimension;
                newHeight = (int)(originalBitmap.Height * ((float)maxDimension / originalBitmap.Width));
            }
            else
            {
                newHeight = maxDimension;
                newWidth = (int)(originalBitmap.Width * ((float)maxDimension / originalBitmap.Height));
            }

            // Create the info for the new resized bitmap
            var resizeInfo = new SKImageInfo(newWidth, newHeight);
            SKSamplingOptions opt = SKSamplingOptions.Default;
            // Resize the bitmap
            using var resizedBitmap = originalBitmap.Resize(resizeInfo, opt);

            // Encode the resized bitmap into a JPEG stream
            using var image = SKImage.FromBitmap(resizedBitmap);
            // Using JPEG with a quality setting is crucial for reducing file size
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, quality);

            return data.ToArray();
        }
        catch (Exception ex)
        {

            Debug.WriteLine("error in decode img "+ex.Message);
            return null; // Return null if any error occurs during processing
        }
    }
    private static bool IsLikelyImageFormat(byte[] data)
    {
        if (data.Length < 8) // Need at least a few bytes to check headers
            return false;

        // Common JPEG headers (StartAsync of Image marker)
        if (data.Length >= 2 && data.Take(2).SequenceEqual(new byte[] { 0xFF, 0xD8 }))
            return true;

        // Common PNG headers
        if (data.Length >= 8 && data.Take(8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
            return true;

        //// Common GIF headers
        //if (data.Length >= 6 && (data.Take(3).SequenceEqual(new byte[] { 0x47, 0x49, 0x46 }) || // GIF87a
        //                        data.Take(3).SequenceEqual("GIF"u8.ToArray()))) // GIF89a
        //    return true;

        // Common BMP headers
        if (data.Length >= 2 && data.Take(2).SequenceEqual(new byte[] { 0x42, 0x4D }))
            return true;

        // Add more checks for other formats if needed (e.g., WebP)

        return false;
    }

}

public static class ImageFilterUtils
{
  
    /// <summary>
    /// Calculates the perceived luminance of a color and returns a high-contrast
    /// TEXT color (either black or white) suitable for placing on top of it.
    /// </summary>
    public static SKColor GetContrastingTextColor(SKBitmap bmp)
    {
        double sum = 0;
        int count = bmp.Width * bmp.Height;

        for (int y = 0; y < bmp.Height; y++)
        {
            for (int x = 0; x < bmp.Width; x++)
            {
                var c = bmp.GetPixel(x, y);
                double r = Linearize(c.Red / 255.0);
                double g = Linearize(c.Green / 255.0);
                double b = Linearize(c.Blue / 255.0);

                sum += 0.2126 * r + 0.7152 * g + 0.0722 * b;
            }
        }

        double avLum = sum / count;
        return avLum > 0.179 ? SKColors.Black : SKColors.White;
    }
    public static SKColor GetContrastingTextColor(byte[]? imageBytes)
    {
        if (imageBytes == null)
            return SKColors.White;
        using var bmp = SKBitmap.Decode(imageBytes);

        return GetContrastingTextColor(bmp);

    }
    public static SKColor GetContrastingTextColor(string? imagePath)
    {
        if (string.IsNullOrEmpty(imagePath))
            return SKColors.White;
        using var bmp = SKBitmap.Decode(imagePath);

        return GetContrastingTextColor(bmp);

    }

    private static double Linearize(double value)
    {
        return value <= 0.03928 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
    }

    public static Color GetTintedBackgroundColor(Color color, float alpha = 0.1f)
    {
        return color?.MultiplyAlpha(alpha) ?? Colors.Transparent;
    }
    /// <summary>
    /// Applies a specified filter effect to an image.
    /// </summary>
    /// <param name="imageData">The original image data in bytes.</param>
    /// <param name="filterType">The filter effect to apply.</param>
    /// <returns>A new byte array representing the filtered image, or the original if the filter is None or fails.</returns>
    public async static Task<byte[]?> ApplyFilter(string? coverfilePath, FilterType filterType)
    {
        // --- Step 1: Validate the input ---
        if (string.IsNullOrWhiteSpace(coverfilePath) || !File.Exists(coverfilePath))
        {
            // Log an error or warning here if you want
            return null;
        }

        try
        {
            // --- Step 2: Read the file data asynchronously ---
            var imageData = await File.ReadAllBytesAsync(coverfilePath);
            if (imageData == null || imageData.Length == 0 || filterType == FilterType.None)
            {
                return imageData;
            }

            try
            {
                using var original = SKBitmap.Decode(imageData);
                if (original == null)
                    return imageData;

                using var surface = SKSurface.Create(new SKImageInfo(original.Width, original.Height));
                using var canvas = surface.Canvas;
                using var paint = new SKPaint();

                // Apply the filter using an SKImageFilter
                paint.ImageFilter =  CreateFilter(filterType);

                // Draw the original bitmap onto the canvas with the filter applied
                canvas.DrawBitmap(original, 0, 0, paint);
                canvas.Flush();

                using var image = surface.Snapshot();
                using var data = image.Encode(SKEncodedImageFormat.Png, 90); // Encode as JPEG for web/app use
                
                return data.ToArray();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to apply image filter {filterType}: {ex.Message}");
                return imageData; // Return original on failure
            }
        }
        catch (Exception ex)
        {
            // --- Step 5: Handle potential errors gracefully ---
            // This could happen if the file is not a valid image, is corrupted, etc.
            Debug.WriteLine($"Error applying filter {filterType} to {coverfilePath}: {ex.Message}");
            return null; // Always return a fallback value on error.
        }
    }

    private static SKImageFilter? CreateFilter(FilterType filterType)
    {
        switch (filterType)
        {
            case FilterType.Blur:
                // A simple Gaussian blur
                return SKImageFilter.CreateBlur(15.0f, 15.0f);

            case FilterType.Grayscale:
                // A color matrix that converts colors to their grayscale equivalent
                float[] grayscaleMatrix =
                {
                    0.21f, 0.72f, 0.07f, 0, 0,
                    0.21f, 0.72f, 0.07f, 0, 0,
                    0.21f, 0.72f, 0.07f, 0, 0,
                    0,     0,     0,     1, 0
                };
                return SKImageFilter.CreateColorFilter(SKColorFilter.CreateColorMatrix(grayscaleMatrix));

            case FilterType.Sepia:
                // A classic sepia tone color matrix
                float[] sepiaMatrix =
                {
                    0.393f, 0.769f, 0.189f, 0, 0,
                    0.349f, 0.686f, 0.168f, 0, 0,
                    0.272f, 0.534f, 0.131f, 0, 0,
                    0,      0,      0,      1, 0
                };
                return SKImageFilter.CreateColorFilter(SKColorFilter.CreateColorMatrix(sepiaMatrix));

            case FilterType.DarkAcrylic:
                { // Use braces to create a new scope for the variables
                  // --- Step 1: Create the color filter for the dark tint ---
                    var colorFilter = SKColorFilter.CreateBlendMode(new SKColor(0, 0, 0, 120), SKBlendMode.SrcOver);
                    // --- FIX: Create an ImageFilter FROM the ColorFilter ---
                    var darkTintFilter = SKImageFilter.CreateColorFilter(colorFilter);

                    // --- Step 2: Create the blur filter ---
                    var blurFilter = SKImageFilter.CreateBlur(25.0f, 25.0f);

                    // --- Step 3: Compose the two ImageFilters correctly ---
                    // The outer filter is applied last. So we put the blur as the outer filter,
                    // and the tint as the inner filter. This tints the image FIRST, then blurs it.
                    // For a more "frosted glass" look, you might swap them.
                    return SKImageFilter.CreateCompose(blurFilter, darkTintFilter);
                }

            case FilterType.Glassy:
                {
                    // --- Step 1: Create the color filter for the bright tint ---
                    var colorFilter = SKColorFilter.CreateBlendMode(new SKColor(255, 255, 255, 80), SKBlendMode.SrcOver);
                    // --- FIX: Create an ImageFilter FROM the ColorFilter ---
                    var brightTintFilter = SKImageFilter.CreateColorFilter(colorFilter);

                    // --- Step 2: Create the blur filter ---
                    var glassyBlurFilter = SKImageFilter.CreateBlur(20.0f, 20.0f);

                    // --- Step 3: Compose them ---
                    return SKImageFilter.CreateCompose(glassyBlurFilter, brightTintFilter);
                }

            case FilterType.Mauve:
                // A color blend mode to tint the image with a mauve color
                return SKImageFilter.CreateColorFilter(SKColorFilter.CreateBlendMode(new SKColor(224, 176, 255, 100), SKBlendMode.Modulate));

            case FilterType.Ocean:
                // A color blend mode to tint the image with a blue/green color
                return SKImageFilter.CreateColorFilter(SKColorFilter.CreateBlendMode(new SKColor(0, 128, 128, 90), SKBlendMode.Overlay));

            default:
                return null;
        }
    }

}

public enum FilterType
    {
        None,
        Blur,
        Grayscale,
        Sepia,
        DarkAcrylic, // A blur with a dark tint
        Glassy,      // A blur with a bright, semi-transparent overlay
        Mauve,       // A color tint
        Ocean        // Another color tint
    }

  

public static class TimeUtils
{
    /// <summary>
    /// Formats a duration in seconds into a "mm:ss" or "hh:mm:ss" string.
    /// </summary>
    /// <param name="totalSeconds">The duration in seconds.</param>
    /// <returns>A formatted time string.</returns>
    public static string FormatDuration(double totalSeconds)
    {
        if (totalSeconds < 0)
            totalSeconds = 0;
        var timeSpan = TimeSpan.FromSeconds(totalSeconds);

        // If longer than an hour, include the hours. Otherwise, just minutes and seconds.
        return timeSpan.TotalHours >= 1
            ? timeSpan.ToString(@"h\:mm\:ss")
            : timeSpan.ToString(@"m\:ss");
    }

    /// <summary>
    /// Converts a DateTimeOffset into a relative, human-friendly string like "5 minutes ago".
    /// </summary>
    /// <param name="dateTime">The timestamp to format.</param>
    /// <returns>A human-readable relative time string.</returns>
    public static string HumanizeDate(DateTimeOffset dateTime)
    {
        const int SECOND = 1;
        const int MINUTE = 60 * SECOND;
        const int HOUR = 60 * MINUTE;
        const int DAY = 24 * HOUR;
        const int MONTH = 30 * DAY;

        var ts = new TimeSpan(DateTimeOffset.UtcNow.Ticks - dateTime.Ticks);
        double delta = Math.Abs(ts.TotalSeconds);

        if (delta < 1 * MINUTE)
            return ts.Seconds == 1 ? "one second ago" : ts.Seconds + " seconds ago";

        if (delta < 2 * MINUTE)
            return "a minute ago";

        if (delta < 45 * MINUTE)
            return ts.Minutes + " minutes ago";

        if (delta < 90 * MINUTE)
            return "an hour ago";

        if (delta < 24 * HOUR)
            return ts.Hours + " hours ago";

        if (delta < 48 * HOUR)
            return "yesterday";

        if (delta < 30 * DAY)
            return ts.Days + " days ago";

        if (delta < 12 * MONTH)
        {
            int months = Convert.ToInt32(Math.Floor((double)ts.Days / 30));
            return months <= 1 ? "one month ago" : months + " months ago";
        }
        else
        {
            int years = Convert.ToInt32(Math.Floor((double)ts.Days / 365));
            return years <= 1 ? "one year ago" : years + " years ago";
        }
    }

}

public static class BgQueue
{
    public static readonly BackgroundTaskQueue Instance = new();
}
