using SkiaSharp;

namespace Dimmer.Interfaces.Services.Interfaces.FileProcessing.FileProcessorUtils;


public class CoverArtService : ICoverArtService
{
    private readonly ProcessingConfig _config;
    private static readonly string[] _supportedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp" }; // Add more if needed

    private readonly IRealmFactory _realmFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CoverArtService> _logger;
    private readonly SubscriptionManager _subsManager;
    IDimmerAudioService _audioService;
    public CoverArtService(IHttpClientFactory httpClientFactory,
        SubscriptionManager subsManager, IRealmFactory realmFactory, ILogger<CoverArtService> logger, IDimmerAudioService audService)
    {
        _httpClientFactory = httpClientFactory;
        _realmFactory = realmFactory;
        _logger = logger;
        _audioService = audService;

        _subsManager = subsManager;

        _config = new ProcessingConfig();

        EnsureCoverPath();
    }
  
    private void EnsureCoverPath()
    {
        if (!Directory.Exists(_config.CoverArtBasePath))
            Directory.CreateDirectory(_config.CoverArtBasePath);
    }
    private static string SanitizeFileName(string fileName)
    {
        return string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));
    }

    private static string? GetExtensionFromMimeType(string? mimeType)
    {
        return mimeType?.ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/jpg" => ".jpg",
            "image/png" => ".png",
            "image/gif" => ".gif",
            "image/bmp" => ".bmp",
            _ => null, // Fallback or decide default (e.g., ".png")
        };

    }

    public string? GetExistingCoverImageAsync(string audioFilePath)
    {

        if (string.IsNullOrWhiteSpace(audioFilePath))
            return null;

        string decodedPath = Uri.UnescapeDataString(audioFilePath);
        string baseFileName = Path.GetFileNameWithoutExtension(decodedPath);
        string sanitizedBaseFileName = SanitizeFileName(baseFileName);

        foreach (var ext in _supportedExtensions)
        {
            string potentialPath = Path.Combine(_config.CoverArtBasePath, sanitizedBaseFileName + ext);
            if (File.Exists(potentialPath))
            {
                return potentialPath;
            }
        }
        return null;
    }


    public async static Task<bool> SaveSongLyrics(string audioFilePath, string SyncLyrics)
    {
        if (string.IsNullOrEmpty(audioFilePath) || string.IsNullOrEmpty(audioFilePath))
        {
            return false;
        }
        try
        {

            string decodedPath = Uri.UnescapeDataString(audioFilePath);
            string baseFileName = Path.GetFileNameWithoutExtension(decodedPath);
            string? fileFolderPath = Path.GetDirectoryName(audioFilePath);
            string sanitizedBaseFileName = SanitizeFileName(baseFileName);
            string? extension = ".lrc";
            string targetFilePath = Path.Combine(fileFolderPath, sanitizedBaseFileName + extension);
            await File.WriteAllTextAsync(targetFilePath, SyncLyrics);
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            return false;
        }
;
    }

    public async Task<string?> SaveOrGetCoverImageAsync(ObjectId songId, string audioFilePath, PictureInfo? embeddedPictureInfo)
    {
        if (string.IsNullOrWhiteSpace(audioFilePath))
            return null;

        if (embeddedPictureInfo?.PictureData == null || embeddedPictureInfo.PictureData.Length == 0)
        {
            var fallback = GetFolderCoverImages(audioFilePath).FirstOrDefault();
            if (fallback != null)
            {
                await SaveCoverArtToSingleSongGivenAudioPathAsync(songId, audioFilePath, fallback).ConfigureAwait(false);
                return fallback;
            }
            return null;
        }

        // 3. Save the new image
        string decodedPath = Uri.UnescapeDataString(audioFilePath);
        string baseFileName = Path.GetFileNameWithoutExtension(decodedPath);
        string sanitizedBaseFileName = SanitizeFileName(baseFileName);
        string? extension = GetExtensionFromMimeType(embeddedPictureInfo.MimeType) ?? ".png"; // Default to .png if MIME type is unknown/unsupported
        string targetFilePath = Path.Combine(_config.CoverArtBasePath, sanitizedBaseFileName + extension);

        try
        {

            
            await SaveCoverArtToSingleSongGivenAudioPathAsync(songId, audioFilePath, targetFilePath);


            await File.WriteAllBytesAsync(targetFilePath, embeddedPictureInfo.PictureData);
            return targetFilePath;
            
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error saving cover image for {audioFilePath}: {ex.Message}");
            // Consider proper logging
            return null;
        }
    }

    public async Task<bool> ApplyCoverArtToSongsAsync(IEnumerable<SongModelView> songs, string coverArtSource)
    {
        if (songs == null || !songs.Any() || string.IsNullOrWhiteSpace(coverArtSource))
        {
            _logger.LogWarning("ApplyCoverArtToSongsAsync called with invalid arguments.");
            return false;
        }

        // --- Step 1: Get the image data, either from web or local file ---
        byte[]? imageData = await GetImageDataAsync(coverArtSource);
        if (imageData == null || imageData.Length == 0)
        {
            _logger.LogError("Failed to retrieve or read image data from source: {Source}", coverArtSource);
            return false;
        }

        var picInfo = PictureInfo.fromBinaryData(imageData);
        var successfullyProcessedSongIds = new List<ObjectId>();
        var coverArtHash = ComputeHash(imageData); // A unique ID for this specific image

        // --- Step 2: Loop through songs and embed the image into the file tags ---
        foreach (var song in songs)
        {
            try
            {
                if (string.IsNullOrEmpty(song.FilePath) || !File.Exists(song.FilePath))
                    continue;

                var track = new Track(song.FilePath);
                track.EmbeddedPictures.Clear(); // Remove existing art
                track.EmbeddedPictures.Add(picInfo);
                track.Save();

                successfullyProcessedSongIds.Add(song.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to embed cover art into file: {FilePath}", song.FilePath);
            }
        }

        if (successfullyProcessedSongIds.Count == 0)
            return false;

        // --- Step 3: Update all processed songs in the database in a single transaction ---
        try
        {
            using var realm = _realmFactory.GetRealmInstance();
            await realm.WriteAsync(() =>
            {
                foreach (var songId in successfullyProcessedSongIds)
                {
                    var songInDb = realm.Find<SongModel>(songId);
                    if (songInDb != null)
                    {
                        // IMPORTANT: Add a CoverArtHash property to your SongModel
                        songInDb.CoverImagePath = coverArtSource;
                        songInDb.CoverArtHash = coverArtHash;
                        songInDb.LastDateUpdated = DateTimeOffset.UtcNow;
                    }
                }
            });
            _logger.LogInformation("Successfully updated CoverArtHash in DB for {Count} songs.", successfullyProcessedSongIds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update song records in database with new CoverArtHash.");
            return false; // DB update failed
        }

        // --- Step 4: Update the UI models instantly for a responsive feel ---
        foreach (var song in songs.Where(s => successfullyProcessedSongIds.Contains(s.Id)))
        {
            // You'll need to add this property to your SongModelView as well
            song.CoverArtHash = coverArtHash;
            // This is how you can force an image to refresh if it's bound with a cache-busting mechanism
            //song.CoverArtCacheBuster = Guid.NewGuid().ToString();
        }

        return true;
    }

    public async Task<bool> SaveCoverArtToSingleSongGivenAudioPathAsync(ObjectId songId, string audioPath, string coverArtSource)
    {
        if (string.IsNullOrWhiteSpace(coverArtSource) || string.IsNullOrEmpty(audioPath) || !TaggingUtils.FileExists(audioPath))
        {
            _logger.LogWarning("ApplyCoverArtToSongsAsync called with invalid arguments.");
            return false;
        }

        // --- Step 1: Get the image data, either from web or local file ---
        byte[]? imageData = await GetImageDataAsync(coverArtSource);
        if (imageData == null || imageData.Length == 0)
        {
            _logger.LogError("Failed to retrieve or read image data from source: {Source}", coverArtSource);
            return false;
        }

        var picInfo = PictureInfo.fromBinaryData(imageData);
        var coverArtHash = ComputeHash(imageData); // A unique ID for this specific image

        // --- Step 2: Loop through songs and embed the image into the file tags ---

        //string tempPath = Path.GetTempFileName();

        //File.Copy(audioPath, tempPath, true);

        //try
        //{
        //    var track = new Track(tempPath);
        //    track.EmbeddedPictures.Clear();
        //    track.EmbeddedPictures.Add(picInfo);
        //    await track.SaveAsync();

        //    // Replace original
        //    File.Copy(tempPath, audioPath, true);
        //}
        //catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Failed to embed cover art into file: {FilePath}", audioPath);
        //    }
        //finally
        //{
        //    File.Delete(tempPath);
        //}

        // --- Step 3: Update all processed songs in the database in a single transaction ---
        try
        {
            using var realm = _realmFactory.GetRealmInstance();
            await realm.WriteAsync(() =>
            {

                var songInDb = realm.Find<SongModel>(songId);
                if (songInDb != null)
                {
                    // IMPORTANT: Add a CoverArtHash property to your SongModel
                    songInDb.CoverArtHash = coverArtHash;
                    songInDb.CoverImagePath = coverArtSource;
                    songInDb.LastDateUpdated = DateTimeOffset.UtcNow;
                }

            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update song records in database with new CoverArtHash.");
            return false; // DB update failed
        }



        return true;
    }

    private async Task<byte[]?> GetImageDataAsync(string source)
    {
       

        try
        {
            if (TaggingUtils.FileExists(source))
            {
                var result = await File.ReadAllBytesAsync(source);
                if (result is not null && result.Length > 0)
                {
                    return result;
                }
            }
            else if (Uri.IsWellFormedUriString(source, UriKind.Absolute))
            {
                var client = _httpClientFactory.CreateClient();
                return await client.GetByteArrayAsync(source);
            }
            else
            {
                Console.WriteLine("issue");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting image data from source: {Source}", source);
        }
        return null;
    }

    private string ComputeHash(byte[] data)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(data);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    
    public List<string> GetFolderCoverImages(string audioFilePath)
    {
        if (string.IsNullOrWhiteSpace(audioFilePath) || !File.Exists(audioFilePath))
            return [];

        string? directory = Path.GetDirectoryName(audioFilePath);
        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
            return [];

        var imageExts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp" };

        try
        {
            return Directory.EnumerateFiles(directory, "*.*", SearchOption.TopDirectoryOnly)
                            .Where(f => imageExts.Contains(Path.GetExtension(f)))
                            .OrderBy(f => Path.GetFileName(f))
                            .ToList();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[CoverArt] Error enumerating folder images: {ex.Message}");
            return [];
        }
    }

    public async Task AssignCoverToSongsAsync(IEnumerable<SongModelView> songs, string imagePath)
    {
        if (songs == null || string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
            return;

        byte[] imageBytes;
        try
        {
            // Ensure we open it with shared read to avoid "file in use"
            using var stream = File.Open(imagePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            imageBytes = ms.ToArray();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[CoverArt] Failed reading image: {imagePath} -> {ex.Message}");
            return;
        }

        foreach (var song in songs)
        {
            try
            {
                string sanitizedName = SanitizeFileName(song.Title);
                string targetPath = Path.Combine(_config.CoverArtBasePath, $"{sanitizedName}.jpg");

                // Write safely to avoid concurrency
                await StaticUtils.WriteCoverSafeAsync(targetPath, imageBytes);

                song.CoverImagePath = targetPath;
                await SaveCoverArtToSingleSongGivenAudioPathAsync(song.Id, song.FilePath, targetPath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CoverArt] Error assigning cover to {song.Title}: {ex.Message}");
            }
        }
    }



}