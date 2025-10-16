﻿using System.Collections.Concurrent;

namespace Dimmer.Interfaces.Services.Interfaces.FileProcessing.FileProcessorUtils;

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
        // This parallel processing approach can significantly speed up scanning large libraries on multi-core CPUs.
        var results = new ConcurrentBag<FileProcessingResult>();

        Parallel.ForEach(filePaths, filePath =>
        {
            try
            {
                var singleResult = ProcessFile(filePath);
                results.Add(singleResult);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CRITICAL FAILURE processing file '{filePath}': {ex.Message}. This file will be skipped.");
                var errorResult = new FileProcessingResult(filePath);
                errorResult.Errors.Add($"A critical, unhandled exception occurred: {ex.Message}");
                results.Add(errorResult);
            }
        });

        return results.ToList();
    }

    public FileProcessingResult ProcessFile(string filePath)
    {
        var result = new FileProcessingResult(filePath);

        if (!TaggingUtils.IsValidFile(filePath, _config.SupportedAudioExtensions))
        {
            result.Errors.Add("File is invalid, non-existent, or has an unsupported extension.");
            return result;
        }

        var track = new Track(filePath);

        // --- Step 1: Intelligent Metadata Aggregation ---
        // We gather info from both tags and the filename, then merge them.

        // From tags
        string tagTitle = track.Title;
        string tagArtist = track.Artist;
        string tagAlbumArtist = track.AlbumArtist;

        // From filename
        var (parsedArtist, parsedTitle) = FilenameParser.Parse(filePath);

        // --- Step 2: Merge and Sanitize Title ---
        // Prefer tag title, but fall back to parsed filename title.
        string rawTitle = !string.IsNullOrWhiteSpace(tagTitle) ? tagTitle : parsedTitle ?? Path.GetFileNameWithoutExtension(filePath);
        var (finalTitle, versionInfo) = TaggingUtils.ParseTrackTitle(rawTitle);

        if (string.IsNullOrWhiteSpace(finalTitle))
        {
            finalTitle = "Unknown Title"; // Final fallback
        }

        // --- Step 3: Merge and Extract Artists ---
        // Prefer tag artists, but fall back to parsed filename artist.
        string? primaryArtist = !string.IsNullOrWhiteSpace(tagArtist) ? tagArtist : parsedArtist;
        string albumArtist = tagAlbumArtist; // No filename equivalent for this

        List<string> artistNames = TaggingUtils.ExtractArtists(primaryArtist, albumArtist);

        // --- Step 4: Check for Duplicates (using your logic) ---
        // It's better to perform this check in the service layer after processing,
        // but if you must do it here:
        // var existingSong = _metadataService.FindSongByTitleAndDuration(finalTitle, track.Duration);
        // if (existingSong != null) { ... return existing song ... }

        // --- Step 5: Create and Populate Rich SongModelView ---
        string primaryArtistName = artistNames.FirstOrDefault() ?? "Unknown Artist";
        string allArtistsString = string.Join(", ", artistNames);

        // Album Processing
        string albumName = string.IsNullOrWhiteSpace(track.Album) ? "Unknown Album" : track.Album.Trim();
        var album = _metadataService.GetOrCreateAlbum(track, albumName, primaryArtistName); // Pass artist for context

        // Genre Processing
        string genreName = string.IsNullOrWhiteSpace(track.Genre) ? "Unknown Genre" : track.Genre.Trim();
        var genre = _metadataService.GetOrCreateGenre(track, genreName);

        var song = new SongModelView
        {
            Id = ObjectId.GenerateNewId(), // Assuming you use MongoDB ObjectId
            FilePath = filePath,
            Title = finalTitle,
            Description = versionInfo ?? track.Description ?? string.Empty, // Store version info in Description!

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
            FileSize = new FileInfo(filePath).Length,
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

            IsNew = true,
            DateCreated = DateTimeOffset.UtcNow,
            LastDateUpdated = DateTimeOffset.UtcNow
        };

        // Your logic to set the unique key
        song.SetTitleAndDuration(song.Title, song.DurationInSeconds);

        // Associate Artists with the song
        song.ArtistToSong = new();
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

        // TODO: Cover Art Processing
        // var pictureInfo = track.EmbeddedPictures.FirstOrDefault();
        // if (pictureInfo != null)
        // {
        //     song.CoverArtHash = _coverArtService.SaveCoverArt(pictureInfo.PictureData, album.Id);
        //     song.CoverImageBytes = pictureInfo.PictureData; // Or however you handle it
        // }

        result.ProcessedSong = song;
        // The service layer should be responsible for calling AddSong
        // _metadataService.AddSong(song); 

        return result;
    }

    internal void Cleanup()
    {
        _metadataService.ClearAll();
        
    }
}
