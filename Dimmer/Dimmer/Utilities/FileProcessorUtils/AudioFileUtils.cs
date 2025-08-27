namespace Dimmer.Utilities.FileProcessorUtils;
public static class AudioFileUtils
{
    public static string GenerateId(string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix) || prefix.Length < 4)
            prefix = "YBT"; // Default prefix
        return $"{prefix[..4].ToUpperInvariant()}_{Guid.NewGuid()}";
    }

    public static bool IsValidFile(string filePath, HashSet<string> supportedExtensions)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return false;

        if (!supportedExtensions.Contains(Path.GetExtension(filePath)))
            return false;

        FileInfo fileInfo = new FileInfo(filePath);
        return fileInfo.Length > 1000; // Basic size check, can be adjusted
    }

    public static List<string> ExtractArtistNames(string? primaryArtistField, string? albumArtistField)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        ParseAndAddNames(primaryArtistField, names);
        ParseAndAddNames(albumArtistField, names);

        if (names.Count==0 && !string.IsNullOrWhiteSpace(primaryArtistField))
        {
            // If parsing failed to split but there was content, add the raw field.
            names.Add(primaryArtistField.Trim());
        }
        if (names.Count==0 && !string.IsNullOrWhiteSpace(albumArtistField))
        {
            names.Add(albumArtistField.Trim());
        }
        if (names.Count==0)
        {
            names.Add("Unknown Artist");
        }

        return [.. names];
    }

    private static void ParseAndAddNames(string? input, HashSet<string> names)
    {
        if (string.IsNullOrWhiteSpace(input))
            return;

        // Normalize separators: "feat.", "ft.", "vs.", "vs", "&", " x ", " X "
        string normalized = input
            .Replace("feat.", " . ", StringComparison.OrdinalIgnoreCase)
            .Replace("featuring ", " . ", StringComparison.OrdinalIgnoreCase)
            .Replace("ft. ", " . ", StringComparison.OrdinalIgnoreCase)
            .Replace("v. ", " . ", StringComparison.OrdinalIgnoreCase)
            .Replace("vs", " . ", StringComparison.OrdinalIgnoreCase)
            .Replace(",", " . ", StringComparison.OrdinalIgnoreCase)
            .Replace("&", " . ", StringComparison.OrdinalIgnoreCase)
            .Replace(",", " . ", StringComparison.OrdinalIgnoreCase)
            .Replace("x", " . ", StringComparison.OrdinalIgnoreCase); // "Artist A x Artist B"

        string[] individualArtists = normalized.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var artistName in individualArtists)
        {
            string trimmedName = artistName.Trim();
            if (!string.IsNullOrWhiteSpace(trimmedName))
            {
                names.Add(trimmedName);
            }
        }
    }

    public static string SanitizeTrackTitle(string? rawTitle)
    {
        if (string.IsNullOrWhiteSpace(rawTitle))
            return string.Empty;
        // Example: take content before the first semicolon if present, often used for subtitles
        int semiColonIndex = rawTitle.IndexOf(';');
        if (semiColonIndex > 0)
        {
            return rawTitle[..semiColonIndex].Trim();
        }
        return rawTitle.Trim();
    }

    public static List<string> GetAllAudioFilesFromPaths(IEnumerable<string> pathsToScan, HashSet<string> supportedExtensions)
    {
        var allFiles = new List<string>();
        var uniquePaths = pathsToScan.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        foreach (string path in uniquePaths)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    var s = Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories).ToList();
                    var ss = new List<string>();
                    foreach (var file in s)
                    {
                        string fileExtension = Path.GetExtension(file); // Returns string?
                                                                        // Ensure extension is not null and that supportedExtensions can handle it.
                                                                        // Also, consider case-insensitivity if your supportedExtensions list is, for example, all lowercase.
                        if (fileExtension != null && supportedExtensions.Contains(fileExtension, StringComparer.OrdinalIgnoreCase))
                        // Or if supportedExtensions are already normalized (e.g. all lowercase):
                        // if (fileExtension != null && supportedExtensions.Contains(fileExtension.ToLowerInvariant()))
                        {
                            ss.Add(file);
                        }
                    }
                    allFiles.AddRange(ss);
                }
                else if (File.Exists(path) && supportedExtensions.Contains(Path.GetExtension(path)))
                {
                    allFiles.Add(path);
                }
                else
                {
                    Debug.WriteLine($"Invalid path or unsupported file type: '{path}'");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error processing path '{path}': {ex.Message}");
                // Consider collecting these errors to return to the caller
            }
        }
        return [.. allFiles.Distinct(StringComparer.OrdinalIgnoreCase)];
    }
}