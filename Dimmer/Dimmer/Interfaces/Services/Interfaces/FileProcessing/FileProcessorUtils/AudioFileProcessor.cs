using System.Collections.Concurrent;

using Dimmer.Utilities;

using Microsoft.Maui.Storage;

namespace Dimmer.Interfaces.Services.Interfaces.FileProcessing.FileProcessorUtils;

public class AudioFileProcessor : IAudioFileProcessor
{
    private readonly IMusicMetadataService _metadataService;
    private  readonly ProcessingConfig _config;

    public AudioFileProcessor(
        IMusicMetadataService metadataService,
        ProcessingConfig config)
    {
        _metadataService = metadataService;
        _config = config;
    }
    public async Task<List<FileProcessingResult>> ProcessFilesAsync(IEnumerable<string> filePaths)
    {
        var results = new ConcurrentBag<FileProcessingResult>();

        await Parallel.ForEachAsync(
            filePaths,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount // 4, 8, etc.
            },
            async (file, ct) =>
            {
                try
                {
                    var result =  ProcessFile(file);
                    results.Add(result);
                }
                catch (Exception ex)
                {
                    var errorListOfString = new List<string> { $"Unhandled: {ex}" };
                    Debug.WriteLine($"[Critical] {file}: {ex}");
                    results.Add(new FileProcessingResult(file,errorListOfString));
                }
            });

        return results.ToList();
    }
    
    public FileProcessingResult ProcessFile(string filePath)
    {

    long actualFileSize = 0;
        try
        {

            var result = new FileProcessingResult(filePath);

            if (!TaggingUtils.IsValidFile(filePath, _config.SupportedAudioExtensions))
            {
                
                result.Errors.Add("File is invalid or has an unsupported extension.");
                return result;
            }
            Track track;
            try
            {
                if (filePath.StartsWith("content://", StringComparison.OrdinalIgnoreCase))
                {
                    if (TaggingUtils.PlatformGetStreamHook != null)
                    {
                        using (var fileStream = TaggingUtils.PlatformGetStreamHook(filePath)) 
                        { 

                            if (fileStream == null)
                            {
                                result.Errors.Add("Could not open stream for content URI.");
                                return result;
                            }
                            track = new ATL.Track(fileStream, mimeType: null);
                            actualFileSize = fileStream.Length;
                        }
                    }
                    else
                    {
                        result.Errors.Add("Platform stream hook not initialized.");
                        return result;
                    }
                }
                else
                {
                    // 3. Handle Standard Windows/File Paths
                    track = new ATL.Track(filePath);
                    actualFileSize = new FileInfo(filePath).Length;
                }
            }
            catch (FormatException ex)
            {
                Debug.WriteLine($"ATL FormatException on '{filePath}': {ex.Message}");
                // Recover by retrying without parsing lyrics
                track = new Track(filePath)
                {
                    Lyrics = [] // empty fallback
                };
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Failed to load metadata from file: {ex.Message}");
                return result; // Critical failure, cannot proceed.
            }

            // --- Step 1: Intelligent Metadata Aggregation ---
            // We gather info from both tags and the filename, then merge them.

            // From tags
            string tagTitle = track.Title;
            string tagArtist = track.Artist;
            string tagAlbumArtist = track.AlbumArtist;
            string tagAlbum = track.Album;
            string tagGenre = track.Genre;
            var (filenameArtist, filenameTitle) = FilenameParser.Parse(filePath);

            // --- Step 2: Intelligently coalesce to find the best source of truth ---
            // The rule: Tag is king. If tag is missing, fall back to filename.
            string decodedPath = Uri.UnescapeDataString(filePath);

            //var cleanArtist = StaticUtils.CleanArtist(track.Path, tagArtist, tagTitle);
            var cleanTitle = StaticUtils.CleanTitle(track.Path, tagTitle, tagAlbum, tagAlbumArtist);

            string bestRawTitle = !string.IsNullOrWhiteSpace(tagTitle) ? tagTitle : filenameTitle ?? Path.GetFileNameWithoutExtension(decodedPath);
            string? bestRawArtist = !string.IsNullOrWhiteSpace(tagArtist) ? tagArtist : filenameArtist;
            string bestAlbumArtist = tagAlbumArtist; // No filename equivalent for this.
            string bestAlbum = !string.IsNullOrWhiteSpace(tagAlbum) ? tagAlbum : "Unknown Album";
            string bestGenre = !string.IsNullOrWhiteSpace(tagGenre) ? tagGenre : "Unknown Genre";

            List<string> artistNames = TaggingUtils.ExtractArtists(bestRawArtist, bestAlbumArtist)
                .Select(x=> StaticUtils.CleanArtist(track.Path,x,track.Title)).ToList();

            // --- Step 4: Final validation and fallbacks ---
            string finalTitle = string.IsNullOrWhiteSpace(cleanTitle) ? Path.GetFileNameWithoutExtension(filePath) : cleanTitle;
            string primaryArtistName = artistNames.FirstOrDefault() ?? "Unknown Artist";
            string allArtistsString = string.Join(", ", artistNames);

            // --- Step 5: Create and Populate the Rich Data Model ---
            var album = _metadataService.GetOrCreateAlbum(track, bestAlbum.Trim(), primaryArtistName);
            var genre = _metadataService.GetOrCreateGenre(track, bestGenre.Trim());
            var song = new SongModelView
            {
                Id = ObjectId.GenerateNewId(), // Assuming you use MongoDB ObjectId
                FilePath = filePath,
                Title = finalTitle,
                Description = track.Description ?? string.Empty, // Store version info in Description!

                // Artist Info
                ArtistName = primaryArtistName,
                OtherArtistsName = allArtistsString,

                // Album Info
                Album = album,
                AlbumName = album.Name,

                // Genre Info
                Genre = genre,
                GenreName = genre.Name,

                // Technical Info
                DurationInSeconds = track.Duration,
                BitRate = track.Bitrate,
                FileSize = actualFileSize,
                FileFormat = Path.GetExtension(filePath).TrimStart('.').ToLowerInvariant(),

                // Tag Info
                ReleaseYear = track.Year,
                TrackNumber = track.TrackNumber,
                DiscNumber = track.DiscNumber,
                DiscTotal = track.DiscTotal,
                BPM = track.BPM,
                Composer = track.Composer,
                Conductor = track.Conductor ?? string.Empty,
                Language = track.Language ?? string.Empty,
                PopularityScore = track.Popularity ?? 0, // Map ATL's Popularity to Rating
                TrackTotal=track.TrackTotal,
                SampleRate = track.SampleRate,
                //CoverImagePath = track.EmbeddedPictures.
                Encoder = track.Encoder,
                BitDepth = track.BitDepth,
                NbOfChannels = track.ChannelsArrangement.NbChannels,
                IsNew = true,
                DateCreated = DateTimeOffset.UtcNow,
                LastDateUpdated = DateTimeOffset.UtcNow
            };

            // Your logic to set the unique key
            song.SetTitleAndDuration(song.Title, song.DurationInSeconds);

            // Associate Artists with the song
            song.ArtistToSong = [];
            foreach (var name in artistNames)
            {
                var artistModel = _metadataService.GetOrCreateArtist(track, name);
                song.ArtistToSong.Add(artistModel);
            }

            // Lyrics Processing
            song.HasLyrics = track.Lyrics is { Count: > 0 };
            if (song.HasLyrics)
            {
                var lyricsInfo = track.Lyrics.First();
                song.UnSyncLyrics = lyricsInfo.UnsynchronizedLyrics;
                song.EmbeddedSync = new(lyricsInfo.SynchronizedLyrics.Select(p => new LyricPhraseModelView(p)));
            }
        
            foreach (var artView in song.ArtistToSong)
            {
                if (artView is not null)
                {
                    if (album.Artists is null)
                    {
                        album.Artists = [artView];
                    }
                    else
                    {
                        if (!album.Artists.Any(a => a.Id == artView.Id))
                            album.Artists.Add(artView);
                    }
                }
            }
        
            result.ProcessedSong = song;
            result.Success = true;


            return result;
        }
        catch (Exception ex)
        {

            throw new Exception(ex.Message);
        }
    }

    public void Cleanup()
    {
        _metadataService.ClearAll();
        
    }

    
}
