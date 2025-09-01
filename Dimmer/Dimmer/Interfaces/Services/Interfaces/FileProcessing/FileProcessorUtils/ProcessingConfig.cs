namespace Dimmer.Interfaces.Services.Interfaces.FileProcessing.FileProcessorUtils;

public class ProcessingConfig
{
    public string CoverArtBasePath { get; }
    public HashSet<string> SupportedAudioExtensions { get; }

    public ProcessingConfig(string? coverArtBasePath = null, IEnumerable<string>? supportedAudioExtensions = null)
    {
        if (string.IsNullOrWhiteSpace(coverArtBasePath))
        {
            // Default to LocalApplicationData for user-specific, app-managed data
            CoverArtBasePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DimmerApp", // Application-specific folder
                "CoverImages");
        }
        else
        {
            CoverArtBasePath = coverArtBasePath;
        }

        SupportedAudioExtensions = new HashSet<string>(
            supportedAudioExtensions ?? new[] { ".mp3", ".flac", ".wav", ".m4a", ".aac", ".ogg", ".opus" },
            StringComparer.OrdinalIgnoreCase
        );
    }
}
