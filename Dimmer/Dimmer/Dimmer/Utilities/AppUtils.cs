using SkiaSharp;

namespace Dimmer.Utilities;
public static class AppUtils
{
    public static int UserScreenHeight { get; set; }
    public static int UserScreenWidth { get; set; }

    public static bool IsUserFirstTimeOpening { get; set; } = false;

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
        string artistName = !string.IsNullOrWhiteSpace(currentSong?.ArtistName) ? $" by {currentSong.ArtistName}" : ""; // Optional: Add artist if available

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
    public static SKColor GetDominantColor(byte[]? imageData, SKColor? defaultColor = null)
    {
        var palette = GetVibrantPalette(imageData, 1);
        return palette.Any() ? palette.First() : (defaultColor ?? SKColors.Gray);
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
            for (int x = 0; x < bitmap.Width; x++)
            {
                for (int y = 0; y < bitmap.Height; y++)
                {
                    SKColor color = bitmap.GetPixel(x, y);

                    // --- Filter out non-vibrant colors ---
                    color.ToHsv(out _, out float saturation, out float value);
                    // Ignore blacks, whites, and grays
                    if (saturation < 0.3f || value < 0.2f || value > 0.95f)
                    {
                        continue;
                    }

                    // --- Group similar colors (Quantization) ---
                    // This reduces the number of unique colors to a manageable amount.
                    var quantizedColor = new SKColor(
                        (byte)(color.Red & 0xF0),
                        (byte)(color.Green & 0xF0),
                        (byte)(color.Blue & 0xF0)
                    );

                    colorCounts.TryGetValue(quantizedColor, out int currentCount);
                    colorCounts[quantizedColor] = currentCount + 1;
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

    public static byte[]? ResizeImage(byte[]? originalImageData, int maxDimension = 1400, int quality = 95)
    {
        try
        {
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

        // Common JPEG headers (Start of Image marker)
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

public enum FilterType
{
    None,
    Blur,
    Grayscale,
    Sepia,
    DarkAcrylic,
    Mauve,
    Ocean
}