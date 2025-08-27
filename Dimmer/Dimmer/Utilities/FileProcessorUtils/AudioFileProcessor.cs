using ATL;

using Dimmer.Interfaces.Services.Interfaces.FileProcessing;

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

    public List<FileProcessingResult> ProcessFiles(IEnumerable<string> filePaths)
    {
        var results = new List<FileProcessingResult>();

        foreach (var path in filePaths)
        {
            try
            {
                // We call ProcessFile for each path.
                // If an exception happens inside ProcessFile, we will catch it here.
                var singleResult = ProcessFile(path);
                results.Add(singleResult);
            }
            catch (Exception ex)
            {
                // --- THIS IS THE SAFETY NET ---
                // An unexpected exception occurred while processing a single file.
                // This could be the ATL library crash or something else.

                // Log the error with the specific file path so you can debug it later.
                Debug.WriteLine($"A critical, unhandled error occurred while processing file: {path}. Skipping this file.");

                // Create a result object indicating failure for this specific file.
                var errorResult = new FileProcessingResult();
                errorResult.Errors.Add($"A critical error occurred: {ex.Message}. The file was skipped.");
                results.Add(errorResult);

                // The 'continue' statement is implied by the end of the loop block.
                // The loop will simply move on to the next file path.
            }
        }
        return results;
    }

    public FileProcessingResult ProcessFile(string filePath)
    {
        var result = new FileProcessingResult();

        if (!AudioFileUtils.IsValidFile(filePath, _config.SupportedAudioExtensions))
        {
            result.Errors.Add($"File is invalid or not supported: {filePath}");
            return result;
        }

        Track track;

        track = new Track(filePath);


        string title = track.Title;
        string primaryArtist = track.Artist;
        string albumArtist = track.AlbumArtist;

        // --- Step 1: Check if the embedded tags are weak or missing ---
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(primaryArtist))
        {
            Debug.WriteLine($"Weak metadata tags for {filePath}. Attempting to parse from filename.");

            // Use our new smart parser
            var (parsedArtist, parsedTitle) = FilenameParser.Parse(filePath);

            // Overwrite the weak tags with our parsed values if they are better.
            if (string.IsNullOrWhiteSpace(title))
                title = parsedTitle;
            if (string.IsNullOrWhiteSpace(primaryArtist))
                primaryArtist = parsedArtist;
        }

        // --- Step 2: Sanitize and Fallback ---
        title = AudioFileUtils.SanitizeTrackTitle(title);
        if (string.IsNullOrWhiteSpace(title))
        {
            title = Path.GetFileNameWithoutExtension(filePath); // Final fallback
        }



        // --- IT'S A NEW SONG! Proceed with creation. ---
        Debug.WriteLine($"Song '{title}' is new. Creating new entry.");

        // --- Artist Processing ---

        List<string> rawArtistNames = AudioFileUtils.ExtractArtistNames(primaryArtist, albumArtist);
        var artists = new List<ArtistModelView>();
        foreach (var name in rawArtistNames)
        {
            artists.Add(_metadataService.GetOrCreateArtist(track, name));
        }
        string artistString = string.Join(", ", artists.Select(a => a.Name));

        // --- Album Processing ---

        string albumName = string.IsNullOrWhiteSpace(track.Album) ? "Unknown Album" : track.Album.Trim();
        // Try to get cover art path early to associate with album

        var album = _metadataService.GetOrCreateAlbum(track, albumName, string.Empty
            );


        // --- Genre Processing ---
        string genreName = string.IsNullOrWhiteSpace(track.Genre) ? "Unknown Genre" : track.Genre.Trim();
        var genre = _metadataService.GetOrCreateGenre(track, genreName);


        // --- Song Model Creation ---
        var song = new SongModelView
        {
            FilePath = filePath,
            Title = title,
            Album = album,
            AlbumName = album.Name,
            ArtistName= artists.FirstOrDefault()?.Name ?? "Unknown Artist",
            OtherArtistsName= artistString,
            Genre = genre,
            GenreName=track.Genre,
            BPM = track.BPM,
            Composer = track.Composer,
            DurationInSeconds = track.Duration,
            BitRate = track.Bitrate,
            TrackNumber= track.TrackNumber,
            FileSize = new FileInfo(filePath).Length,
            FileFormat = Path.GetExtension(filePath).TrimStart('.').ToLowerInvariant(),
            ReleaseYear = track.Year,
            DiscNumber = track.DiscNumber,
            DiscTotal = track.DiscTotal,
            Description= track.Description ?? string.Empty,
            Language = track.Language ?? string.Empty,
            IsNew=true,
            Conductor= track.Conductor ?? string.Empty,
            Id= ObjectId.GenerateNewId()
            ,

        };
        try
        {


            foreach (var id in artists)
            {
                if (song.ArtistToSong is null)
                {
                    song.ArtistToSong =new();

                    song.ArtistToSong.Add(id);
                }
            }


            song.LastDateUpdated = DateTimeOffset.UtcNow;
            result.ProcessedSong = song;
            Debug.WriteLine($"Processed: {song.Title} by {song.ArtistName}");

            song.HasLyrics = track.Lyrics.Any();
            if (track.Lyrics is not null && track.Lyrics.Count > 0)
            {
                song.UnSyncLyrics = track.Lyrics[0].UnsynchronizedLyrics;

                // Basic to string, real app might parse into a structured format or just store raw
                foreach (var item in track.Lyrics[0].SynchronizedLyrics)
                {
                    song.EmbeddedSync.Add(new LyricPhraseModelView(item));
                }


            }
            _metadataService.AddSong(song);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
        return result;

    }
}