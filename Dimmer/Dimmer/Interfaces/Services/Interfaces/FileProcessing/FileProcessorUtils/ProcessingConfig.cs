namespace Dimmer.Interfaces.Services.Interfaces.FileProcessing.FileProcessorUtils;

public class ProcessingConfig
{
    public string CoverArtBasePath { get; }
    public string BackupRestoreBasePath { get; internal set; }
    public HashSet<string> SupportedAudioExtensions { get; }

    public ProcessingConfig(string? coverArtBasePath = null, IEnumerable<string>? supportedAudioExtensions = null)
    {
        if (string.IsNullOrWhiteSpace(coverArtBasePath))
        {
            // Default to LocalApplicationData for user-specific, app-managed data
            CoverArtBasePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
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
public class BackUpRestoreProcessingConfig
{

    public string BackupRestoreBasePath { get; internal set; }
    public HashSet<string> SupportedFileExtensions { get; }
    public BackUpRestoreProcessingConfig(string? backupRestoreBasePath = null, IEnumerable<string>? supportedFileExtensions = null)
    {
        if (string.IsNullOrWhiteSpace(backupRestoreBasePath))
        {

            BackupRestoreBasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DimmerBackUp");
        }
        else
        {
            BackupRestoreBasePath = backupRestoreBasePath;
        }

        SupportedFileExtensions = new HashSet<string>(
            supportedFileExtensions ?? new[] { ".json" },
            StringComparer.OrdinalIgnoreCase
        );
    }
}