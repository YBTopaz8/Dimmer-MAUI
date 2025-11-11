namespace Dimmer.Utilities;
public class DeviceStaticUtils
{
    // Helper to get a unique ID for the current physical device
    public static string GetCurrentDeviceId()
    {
        return DeviceInfo.Current.Idiom == DeviceIdiom.Desktop ?
               (DeviceInfo.Current.Name + "_" + DeviceInfo.Current.Model + "_" + System.Net.Dns.GetHostName()).Replace(" ", "_") :
               (DeviceInfo.Current.Platform.ToString() + "_" + DeviceInfo.Current.VersionString); // DeviceInfo.Id can be useful if available & stable
    }

    public static ArtistModelView? SelectedArtistOne { get; set; }
    public static SongModelView SelectedSongOne { get; set; }
    public static ArtistModelView? SelectedArtistTwo { get; set; }

}

public static class LyricsManager // A good practice to put static methods in a static class
{
    // A simple, safe implementation for the missing method.
    // It replaces characters that are invalid in a file name with an underscore.
    private static string SanitizeFileName(string fileName)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(c, '_');
        }
        return fileName;
    }

    /// <summary>
    /// Saves synchronized lyrics to a .lrc file in the same directory as the audio file.
    /// </summary>
    /// <param name="audioFilePath">The full path to the source audio file (e.g., "C:\Music\song.mp3").</param>
    /// <param name="syncLyrics">The string content of the .lrc file.</param>
    /// <returns>True if the file was saved successfully, otherwise false.</returns>
    public static async Task<bool> SaveSongLyrics(string audioFilePath, string syncLyrics)
    {
        // --- FIX: Correctly check both parameters ---
        if (string.IsNullOrEmpty(audioFilePath) || string.IsNullOrEmpty(syncLyrics))
        {
            // It's good to know why it failed. Consider logging this.
            Debug.WriteLine("Error: audioFilePath or syncLyrics was null or empty.");
            return false;
        }

        try
        {
            // --- FIX: Get the directory name from the full audio file path ---
            // Path.GetDirectoryName("C:\\MyMusic\\MySong.mp3") returns "C:\\MyMusic"
            string? fileDirectory = Path.GetDirectoryName(audioFilePath);

            // If the path is a root path (e.g. "C:\\song.mp3") or otherwise invalid, directory could be null.
            if (string.IsNullOrEmpty(fileDirectory))
            {
                Debug.WriteLine($"Error: Could not determine directory for path: {audioFilePath}");
                return false;
            }

            // Get the name of the audio file without its extension (e.g., "MySong")
            string baseFileName = Path.GetFileNameWithoutExtension(audioFilePath);
            string sanitizedBaseFileName = SanitizeFileName(baseFileName);

            const string extension = ".lrc";

            // Combine the parts into a final, safe path
            string targetFilePath = Path.Combine(fileDirectory, sanitizedBaseFileName + extension);

            // Use UTF-8 encoding, which is standard for .lrc files
            await File.WriteAllTextAsync(targetFilePath, syncLyrics, Encoding.UTF8);

            Debug.WriteLine($"Successfully saved lyrics to: {targetFilePath}");
            return true;
        }
        // --- IMPROVEMENT: Catch more specific exceptions ---
        catch (IOException ex) // Covers most file-related errors
        {
            Debug.WriteLine($"IO Error saving lyrics file: {ex.Message}");
            return false;
        }
        catch (UnauthorizedAccessException ex) // Good to handle permission errors separately
        {
            Debug.WriteLine($"Permission denied saving lyrics file: {ex.Message}");
            return false;
        }
        catch (Exception ex) // Fallback for any other unexpected error
        {
            Debug.WriteLine($"An unexpected error occurred: {ex.Message}");
            return false;
        }
        // --- FIX: Removed the stray semicolon that was here ---
    }
}