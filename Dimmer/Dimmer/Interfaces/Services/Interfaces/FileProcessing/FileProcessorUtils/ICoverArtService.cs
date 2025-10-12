using SkiaSharp;

namespace Dimmer.Interfaces.Services.Interfaces.FileProcessing.FileProcessorUtils;
public interface ICoverArtService
{
    Task<string?> SaveOrGetCoverImageAsync(ObjectId songId, string audioFilePath, PictureInfo? embeddedPictureInfo);
    string? GetExistingCoverImageAsync(string audioFilePath);
    /// <summary>
                                                             /// Applies a single cover art image (from a local path or URL) to a list of song files
                                                             /// and updates their database records.
                                                             /// </summary>
                                                             /// <param name="songs">The list of songs to update.</param>
                                                             /// <param name="coverArtSource">A local file path or a public URL to a JPG/PNG image.</param>
                                                             /// <returns>True if the operation succeeded for at least one song.</returns>
    Task<bool> ApplyCoverArtToSongsAsync(IEnumerable<SongModelView> songs, string coverArtSource);
}

public class CoverArtService : ICoverArtService
{
    private readonly ProcessingConfig _config;
    private static readonly string[] _supportedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp" }; // Add more if needed

    private readonly IRealmFactory _realmFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CoverArtService> _logger;
    public CoverArtService(IHttpClientFactory httpClientFactory, IRealmFactory realmFactory, ILogger<CoverArtService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _realmFactory = realmFactory;
        _logger = logger;


        _config = new ProcessingConfig();
    }
    public CoverArtService(ProcessingConfig config)
    {
        _config = config;
        // Ensure the base directory for covers exists
        if (!Directory.Exists(_config.CoverArtBasePath))
        {
            Directory.CreateDirectory(_config.CoverArtBasePath);
        }
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

        string baseFileName = Path.GetFileNameWithoutExtension(audioFilePath);
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

            string baseFileName = Path.GetFileNameWithoutExtension(audioFilePath);
            string fileFolderPath = Path.GetPathRoot(baseFileName);
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

        // 1. Check if an image already exists for this file name
        string? existingPath = GetExistingCoverImageAsync(audioFilePath);
        if (existingPath != null)
        {
            return existingPath;
        }

        // 2. If no existing image and no embedded data, nothing to save
        if (embeddedPictureInfo?.PictureData == null || embeddedPictureInfo.PictureData.Length == 0)
        {
            return null;
        }

        // 3. Save the new image
        string baseFileName = Path.GetFileNameWithoutExtension(audioFilePath);
        string sanitizedBaseFileName = SanitizeFileName(baseFileName);
        string? extension = GetExtensionFromMimeType(embeddedPictureInfo.MimeType) ?? ".png"; // Default to .png if MIME type is unknown/unsupported
        string targetFilePath = Path.Combine(_config.CoverArtBasePath, sanitizedBaseFileName + extension);

        try
        {

            await File.WriteAllBytesAsync(targetFilePath, embeddedPictureInfo.PictureData);
            if (File.Exists(targetFilePath))
            {
                await SaveCoverArtToSingleSongGivenAudioPathAsync(songId, audioFilePath, targetFilePath);
                return targetFilePath;
            }
            return null;
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

        if (successfullyProcessedSongIds.Count==0)
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
        if ( string.IsNullOrWhiteSpace(coverArtSource)|| string.IsNullOrEmpty(audioPath)|| !File.Exists(audioPath))
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
        var coverArtHash =  ComputeHash(imageData); // A unique ID for this specific image

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
            if (Uri.IsWellFormedUriString(source, UriKind.Absolute))
            {
                var client = _httpClientFactory.CreateClient();
                return await client.GetByteArrayAsync(source);
            }
            else if (File.Exists(source))
            {
                return await File.ReadAllBytesAsync(source);
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

        public static (string? filePath, byte[]? stream, Stream? memStream) CreateStoryImageAsync(SongModelView selectedSong , string? SaveTo=null)
        {
            // 1. Validate input
            if (selectedSong == null || string.IsNullOrWhiteSpace(selectedSong.FilePath) || !File.Exists(selectedSong.FilePath))
            {
                return (null,null,null);
            }

            try
            {
                // 2. Extract embedded picture using ATL
                var track = new Track(selectedSong.FilePath);
                PictureInfo? picInfo = track.EmbeddedPictures.FirstOrDefault();
                if (picInfo == null || picInfo.PictureData == null || picInfo.PictureData.Length == 0)
                {
                    // No album art found
                    return (null,null,null);
                }

                byte[] imageData = picInfo.PictureData;

                // 3. Load the original image into a SkiaSharp bitmap
                using SKBitmap originalBitmap = SKBitmap.Decode(imageData);
                if (originalBitmap == null)
                {
                    return (null,null,null);
                }

                // 4. Define target and watermark properties
                const int targetSize = 1080; // Standard square size for Instagram/Facebook
                var watermarkText = $"Dimmer on {DeviceInfo.Idiom}";
                var padding = 40;

                // 5. Create the final canvas and bitmap
                // This will be our 1080x1080 canvas to draw on.
                using var finalBitmap = new SKBitmap(targetSize, targetSize);
                using var canvas = new SKCanvas(finalBitmap);

                // Optional: Clear with a black background in case the image doesn't fill the canvas
                canvas.Clear(SKColors.Black);

                // 6. Calculate aspect-ratio-preserving "center-crop" dimensions
                float scale = Math.Max((float)targetSize / originalBitmap.Width, (float)targetSize / originalBitmap.Height);
                float scaledWidth = originalBitmap.Width * scale;
                float scaledHeight = originalBitmap.Height * scale;
                float left = (targetSize - scaledWidth) / 2;
                float top = (targetSize - scaledHeight) / 2;

                var destRect = new SKRect(left, top, left + scaledWidth, top + scaledHeight);

                // Draw the original image onto the canvas, scaled and centered
                canvas.DrawBitmap(originalBitmap, destRect);

                // 7. Add the watermark with a shadow effect
                using var textPaint = new SKPaint
                {
                    
                    TextSize = 428,
                    Color = SKColors.DarkSlateBlue,
                    IsAntialias = true,
                    TextAlign= SKTextAlign.Center,
                    Typeface =  SKTypeface.FromFamilyName("AleySans", SKFontStyleWeight.ExtraBold, SKFontStyleWidth.Expanded, SKFontStyleSlant.Upright)
                };

                // For the shadow, we use a built-in image filter which looks much better
                textPaint.ImageFilter = SKImageFilter.CreateDropShadow(
                    dx: 2.0f,
                    dy: 2.0f,
                    sigmaX: 3.0f,
                    sigmaY: 3.0f,
                    color: SKColors.Black.WithAlpha(200));

                // Measure the text to position it correctly
                var textBounds = new SKRect();
                textPaint.MeasureText( watermarkText);

                // Position watermark in the bottom-right corner
                float textX = targetSize - textBounds.Width - padding;
                // SkiaSharp's DrawText y-coordinate is the baseline, so we adjust for that
                float textY = targetSize - padding;

                canvas.DrawText(watermarkText, textX, textY, SKTextAlign.Center,new SKFont() { Size=60, Embolden=true},textPaint);

            // 8. Save the final image to a temporary file
            string tempFilePath =string.Empty;
            if (string.IsNullOrEmpty(SaveTo))
            {
             tempFilePath= Path.Combine(Path.GetTempPath(), $"story_{Guid.NewGuid()}.png");

            }
            else
            {
                tempFilePath=    SaveTo;
            }
            // Encode the final bitmap as a PNG
            using var image = SKImage.FromBitmap(finalBitmap);
                using var data = image.Encode(SKEncodedImageFormat.Png, 90); // Quality 0-100

                // Asynchronously write the data to the file

                using var stream = File.OpenWrite(tempFilePath);
                 data.SaveTo(stream);
            MemoryStream memoryStream = new MemoryStream();
            data.SaveTo(memoryStream);
            var bytes = data.ToArray();
            return (tempFilePath,bytes, memoryStream);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating story image for {selectedSong.FilePath}: {ex.Message}");
                return (null,null,null);
            }
        }
    
}