using ATL;

using Dimmer.Interfaces.Services.Interfaces;

namespace Dimmer.Utilities.FileProcessorUtils;

public class AudioFileProcessor : IAudioFileProcessor
{
    private readonly ICoverArtService _coverArtService;
    private readonly IMusicMetadataService _metadataService;
    private readonly ProcessingConfig _config;

    public AudioFileProcessor(
        ICoverArtService coverArtService,
        IMusicMetadataService metadataService,
        ProcessingConfig config

        )
    {
        _coverArtService = coverArtService;
        _metadataService = metadataService;
        _config = config;
    }

    public async Task<List<FileProcessingResult>> ProcessFilesAsync(IEnumerable<string> filePaths)
    {
        var results = new List<FileProcessingResult>();
        foreach (var path in filePaths)
        {
            results.Add(await ProcessFileAsync(path));
        }
        return results;
    }

    public async Task<FileProcessingResult> ProcessFileAsync(string filePath)
    {
        var result = new FileProcessingResult();

        if (!AudioFileUtils.IsValidFile(filePath, _config.SupportedAudioExtensions))
        {
            result.Errors.Add($"File is invalid or not supported: {filePath}");
            return result;
        }

        Track track;
        try
        {
            track = new Track(filePath);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading track metadata for {filePath}: {ex.Message}");
            result.Errors.Add($"Could not read metadata: {ex.Message}");
            return result;
        }

        // --- Basic Metadata ---
        string title = AudioFileUtils.SanitizeTrackTitle(track.Title);
        if (string.IsNullOrWhiteSpace(title))
        {
            title = Path.GetFileNameWithoutExtension(filePath); // Fallback title
        }

        // --- Duplicate Check ---
        if (_metadataService.DoesSongExist(title, track.Duration))
        {
            result.Skipped = true;
            result.SkipReason = $"Song '{title}' with duration {track.Duration}s already exists.";
            Debug.WriteLine(result.SkipReason);
            return result;
        }

        // --- Artist Processing ---


        List<string> rawArtistNames = AudioFileUtils.ExtractArtistNames(track.Artist, track.AlbumArtist);
        List<ArtistModel> artists = new();
        foreach (var item in rawArtistNames)
        {
            var art = _metadataService.GetOrCreateArtist(item);

            artists.Add(art);
        }

        string artistString = string.Join(", ", artists.Select(a => a.Name));

        // --- Album Processing ---

        string albumName = string.IsNullOrWhiteSpace(track.Album) ? "Unknown Album" : track.Album.Trim();
        // Try to get cover art path early to associate with album
        PictureInfo? firstPicture = track.EmbeddedPictures?.FirstOrDefault(p => p.PictureData?.Length > 0);
        string? coverPath = string.Empty;// await _coverArtService.SaveOrGetCoverImageAsync(filePath, firstPicture);

        var album = _metadataService.GetOrCreateAlbum(albumName, coverPath
            );
        //album.DiscNumber = track.DiscNumber;
        //album.DiscTotal = track.DiscTotal;



        // --- Genre Processing ---
        string genreName = string.IsNullOrWhiteSpace(track.Genre) ? "Unknown Genre" : track.Genre.Trim();
        var genre = _metadataService.GetOrCreateGenre(genreName);

        // --- Song Model Creation ---
        var song = new SongModel
        {
            FilePath = filePath,
            Title = title,
            Album = album,
            AlbumName = album.Name,
            ArtistName= artistString,
            Genre = genre,
            Composer = track.Composer,
            CoverImagePath = coverPath ?? album.ImagePath, // Song specific or album's
            DurationInSeconds = track.Duration,
            BitRate = track.Bitrate,
            TrackNumber= track.TrackNumber,
            FileSize = new FileInfo(filePath).Length,
            FileFormat = Path.GetExtension(filePath).TrimStart('.').ToLowerInvariant(),
            ReleaseYear = track.Year,
            HasLyrics = !string.IsNullOrWhiteSpace(track.Lyrics?.UnsynchronizedLyrics),
            UnSyncLyrics = track.Lyrics?.UnsynchronizedLyrics,
            HasSyncedLyrics = track.Lyrics?.SynchronizedLyrics?.Any() ?? false,
            Conductor= track.Conductor ?? string.Empty,
            SyncLyrics = track.Lyrics.SynchronizedLyrics.Select(l => l.Text).ToList().ToString(),
            IsNew=true,
            //ArtistIds = [.. artists.Select(a => a.Id)],
            Id= ObjectId.GenerateNewId()
            ,
            IsNewOrModified=true
            //SyncLyrics = track.Lyrics?.SynchronizedLyrics // This needs proper formatting
        };

        foreach (var id in artists)
        {
            song.ArtistIds.Add(id);
        }


        if (song.HasSyncedLyrics && track.Lyrics?.SynchronizedLyrics != null)
        {
            // Basic to string, real app might parse into a structured format or just store raw
            song.SyncLyrics = string.Join(Environment.NewLine, track.Lyrics.SynchronizedLyrics.Select(l => $"[{l.TimestampMs / 1000.0:F3}] {l.Text}"));
        }


        _metadataService.AddSong(song);
        result.ProcessedSong = song;

        Debug.WriteLine($"Processed: {song.Title} by {song.ArtistName}");
        return result;
    }
}