using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using Dimmer.Hoarder.Models;
using Dimmer.Interfaces;
using Dimmer.Interfaces.Services.Interfaces.FileProcessing.FileProcessorUtils;

namespace Dimmer.Hoarder;

public class HoarderService : IHoarderService
{
    private readonly IDimmerAudioEditorService _ffmpegService; // Reuse your existing FFmpeg wrapper
    // Add repositories if needed for DB lookups

    public HoarderService(IDimmerAudioEditorService ffmpegService)
    {
        _ffmpegService = ffmpegService;
    }

    #region 1. Deep File Analysis & Integrity

    public async Task<FileIntegrityReport> AnalyzeFileIntegrityAsync(string filePath)
    {
        var report = new FileIntegrityReport();

        if (!TaggingUtils.FileExists(filePath))
        {
            report.Issues.Add("File missing.");
            report.IsCorrupted = true;
            return report;
        }

        try
        {
            // 1. Calculate Audio Stream Hash (Ignoring Metadata tags)
            // Command: ffmpeg -i input -map 0:a -f md5 -
            // Note: You need to implement RunCommandAsync in your FFmpeg service to return stdout
            // For now, let's assume we calculate standard file hash as a proxy
            using (var md5 = MD5.Create())
            using (var stream = File.OpenRead(filePath))
            {
                var hash = await md5.ComputeHashAsync(stream);
                report.ChecksumMD5 = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }

            // 2. "True Lossless" Check (Basic Heuristic)
            // If it's FLAC but frequency cuts off at 16kHz -> Upscaled MP3
            // This requires FFT analysis which is heavy. 
            // Simplified Logic: Check Stream Metadata vs Container

            var ext = Path.GetExtension(filePath).ToLower();
            if (ext == ".flac" || ext == ".wav" || ext == ".alac")
            {
                report.Quality = AudioQualityRating.LosslessStandard;
                // If BitDepth > 16 -> HighRes
            }
            else
            {
                report.Quality = AudioQualityRating.LossyHigh;
            }

            // TODO: Use FFmpeg 'error' detection to check for corruption
            // ffmpeg -v error -i file.mp3 -f null -
            // If exit code != 0 or stderr has output -> Corrupted
        }
        catch (Exception ex)
        {
            report.IsCorrupted = true;
            report.Issues.Add($"Analysis Failed: {ex.Message}");
        }

        return report;
    }
    public Dictionary<string, List<string>> SuggestAlbumGroupings(string rootFolder)
    {
        // 1. Scan all files
        var files = Directory.GetFiles(rootFolder, "*.*", SearchOption.AllDirectories)
                             .Where(f => f.EndsWith(".mp3") || f.EndsWith(".flac"));

        // 2. Group by Directory
        var folderGroups = files.GroupBy(Path.GetDirectoryName);

        var suggestions = new Dictionary<string, List<string>>();

        foreach (var folder in folderGroups)
        {
            // 3. Analyze folder content
            // Read tags (using TagLib or your existing parser)
            // Count dominant Album Name

            // Pseudo-code:
            // var albumCounts = folder.Select(GetAlbumTag).GroupBy(x => x).OrderByDescending(g => g.Count());
            // var dominantAlbum = albumCounts.First().Key;

            // If folder has 10 songs, 8 are "Album A", 2 are "Unknown"
            // Suggest: Move the 2 Unknowns to "Album A" context
        }
        return suggestions;
    }
    public async Task<List<SongModelView>> FindDuplicatesByAudioHashAsync(IEnumerable<SongModelView> songs)
    {
        // 1. Group by Duration (Fastest filter)
        var durationGroups = songs.GroupBy(s => Math.Round(s.DurationInSeconds, 1))
                                  .Where(g => g.Count() > 1);

        var duplicates = new List<SongModelView>();

        foreach (var group in durationGroups)
        {
            // 2. Group by Title (Fuzzy match) or just hash everyone in this bucket
            // Calculating hash for 1000s of files is slow.
            // Strategy: Only hash if Title similarity > 80%

            // Simplified: Just return the potential list for user review
            duplicates.AddRange(group);
        }

        return duplicates;
    }

    #endregion

    #region 2. Smart Organization & Sidecars

    public async Task ExportSidecarMetadataAsync(SongModelView song)
    {
        if (song == null || string.IsNullOrEmpty(song.FilePath)) return;

        var sidecar = new SongSidecarModel
        {
            Id = song.Id.ToString(),
            Title = song.Title,
            Artist = song.ArtistName,
            PlayCount = song.PlayCount,
            Rating = song.Rating,
            Notes = song.UserNoteAggregatedCol?.Select(n => n.UserMessageText).ToList(),
            PlayHistory = song.PlayEvents?.Select(e => new PlayEventDto
            {
                Date = e.EventDate ?? DateTimeOffset.MinValue,
                Type = e.PlayType
            }).ToList(),

            // Hoarder specific
            OriginalHash = song.CoverArtHash, // Should be AudioHash ideally
            LastVerified = DateTimeOffset.UtcNow
        };

        string json = JsonSerializer.Serialize(sidecar, new JsonSerializerOptions { WriteIndented = true });
        string path = Path.ChangeExtension(song.FilePath, ".json"); // MySong.mp3 -> MySong.json

        await File.WriteAllTextAsync(path, json);
    }

    public async Task SmartOrganizeLibraryAsync(string rootFolder, IEnumerable<SongModelView> songs, IProgress<double> progress)
    {
        int total = songs.Count();
        int current = 0;

        foreach (var song in songs)
        {
            if (!File.Exists(song.FilePath)) continue;

            // Pattern: Root / Artist / [Year] Album / Track - Title.ext
            string safeArtist = SanitizeFileName(song.ArtistName ?? "Unknown Artist");
            string safeAlbum = SanitizeFileName(song.AlbumName ?? "Unknown Album");
            string yearStr = song.ReleaseYear.HasValue ? $"[{song.ReleaseYear}] " : "";
            string safeTitle = SanitizeFileName(song.Title ?? "Unknown Title");
            string trackStr = song.TrackNumber.HasValue ? $"{song.TrackNumber:D2} - " : "";
            string ext = Path.GetExtension(song.FilePath);

            string targetDir = Path.Combine(rootFolder, safeArtist, $"{yearStr}{safeAlbum}");
            string targetName = $"{trackStr}{safeTitle}{ext}";
            string targetPath = Path.Combine(targetDir, targetName);

            if (song.FilePath != targetPath)
            {
                if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

                // Smart Check: Does target exist?
                if (File.Exists(targetPath))
                {
                    // Hoarder Conflict Resolution:
                    // Compare Bitrates/Size. Keep the better one.
                    var existingInfo = new FileInfo(targetPath);
                    var currentInfo = new FileInfo(song.FilePath);

                    if (currentInfo.Length > existingInfo.Length)
                    {
                        // Overwrite (maybe backup old one to a 'Duplicates' folder)
                        File.Move(song.FilePath, targetPath, true);
                    }
                }
                else
                {
                    File.Move(song.FilePath, targetPath);
                }

                // Update DB Model
                song.FilePath = targetPath;
            }

            current++;
            progress?.Report((double)current / total * 100);
        }
    }

   
    #endregion

    #region 3. Completionist & Discovery

    public async Task<List<string>> GetMissingTracksFromAlbumAsync(AlbumModelView album)
    {
        var missing = new List<string>();

        if (album.SongsInAlbum == null || !album.SongsInAlbum.Any()) return missing;

        // 1. Get max track number found
        int maxTrack = album.SongsInAlbum.Max(s => s.TrackNumber ?? 0);

        // 2. If metadata says "TotalTracks", use that
        if (album.TrackTotal.HasValue && album.TrackTotal > maxTrack)
            maxTrack = album.TrackTotal.Value;

        // 3. Find gaps
        var existingTracks = album.SongsInAlbum
            .Where(s => s.TrackNumber.HasValue)
            .Select(s => s.TrackNumber.Value)
            .ToHashSet();

        for (int i = 1; i <= maxTrack; i++)
        {
            if (!existingTracks.Contains(i))
            {
                missing.Add($"Track {i}");
            }
        }

        return await Task.FromResult(missing);
    }

    #endregion


    public async Task<OrganizationResult> OrganizeFilesBasedOnMetadataAsync(
    IEnumerable<SongModelView> songs,
    string targetRootPath,
    bool deleteEmptySourceFolders,
    IProgress<double> progress)
    {
        var result = new OrganizationResult();

        // 1. Defensive Path Validation
        if (string.IsNullOrWhiteSpace(targetRootPath))
        {
            result.Logs.Add("Error: Target path is empty.");
            return result;
        }

        try
        {
            // Normalize path separators and ensure full path
            targetRootPath = Path.GetFullPath(targetRootPath);
            if (!Directory.Exists(targetRootPath))
            {
                // Human Case: User pasted a path that doesn't exist yet. Create it.
                Directory.CreateDirectory(targetRootPath);
            }
        }
        catch (Exception ex)
        {
            result.Logs.Add($"CRITICAL: Invalid target path '{targetRootPath}'. {ex.Message}");
            return result;
        }

        int total = songs.Count();
        int current = 0;

        foreach (var song in songs)
        {
            current++;
            progress?.Report((double)current / total * 100);

            // Validation: Don't move missing files
            if (!File.Exists(song.FilePath))
            {
                result.FailureCount++;
                result.Logs.Add($"Skipped (File not found): {song.FilePath}");
                continue;
            }

            try
            {
                // 2. Metadata Extraction & Sanitization
                string artist = SanitizeFileName(song.ArtistName ?? "Unknown Artist");
                string album = SanitizeFileName(song.AlbumName ?? "Unknown Album");

                // Logic: "Album - Year" or just "Album" if year is missing
                string albumFolder = song.ReleaseYear.HasValue
                    ? $"{album} - {song.ReleaseYear}"
                    : album;

                string title = SanitizeFileName(song.Title ?? "Unknown Title");
                string ext = Path.GetExtension(song.FilePath);

                // Logic: "01 - Song Title.mp3"
                string fileName = song.TrackNumber.HasValue
                    ? $"{song.TrackNumber:D2} - {title}{ext}"
                    : $"{title}{ext}";

                // 3. Construct Destination
                // Structure: TargetRoot / Artist / Album - Year / File
                string destDir = Path.Combine(targetRootPath, artist, albumFolder);
                string destPath = Path.Combine(destDir, fileName);

                // Skip if already in the right place
                if (string.Equals(song.FilePath, destPath, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // 4. Create Directory
                if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);

                // 5. Handle Collisions (Hoarder Friendly)
                if (File.Exists(destPath))
                {
                    // If a file exists, we don't overwrite blindly. We verify.
                    var srcInfo = new FileInfo(song.FilePath);
                    var destInfo = new FileInfo(destPath);

                    if (srcInfo.Length == destInfo.Length)
                    {
                        result.Logs.Add($"Duplicate skipped: {fileName}");
                        continue;
                    }
                    else
                    {
                        // Rename to keep both: "Song (1).mp3"

                        string decodedPath = Uri.UnescapeDataString(fileName);
                        string newName = $"{Path.GetFileNameWithoutExtension(decodedPath)} (1){ext}";
                        destPath = Path.Combine(destDir, newName);
                    }
                }

                // 6. The Move
                File.Move(song.FilePath, destPath);

                // 7. Housekeeping: Update DB Model immediately
                // This ensures the UI doesn't break after the move
                song.FilePath = destPath;

                // Optional: Clean up old folder if empty
                if (deleteEmptySourceFolders)
                {
                    string oldDir = Path.GetDirectoryName(song.FilePath);
                    if (Directory.GetFiles(oldDir).Length == 0 && Directory.GetDirectories(oldDir).Length == 0)
                    {
                        try { Directory.Delete(oldDir); } catch { /* Ignore system folders */ }
                    }
                }

                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                result.FailureCount++;
                result.Logs.Add($"Failed to move '{song.Title}': {ex.Message}");
            }
        }

        return result;
    }







    // Helper to remove illegal chars (\ / : * ? " < > |)
    private string SanitizeFileName(string name)
    {
        string invalidChars = System.Text.RegularExpressions.Regex.Escape(new string(Path.GetInvalidFileNameChars()));
        string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

        return System.Text.RegularExpressions.Regex.Replace(name, invalidRegStr, "_").Trim();
    }
}

// Helper DTOs for JSON Serialization
public class SongSidecarModel
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string Artist { get; set; }
    public int PlayCount { get; set; }
    public int Rating { get; set; }
    public List<string> Notes { get; set; }
    public List<PlayEventDto> PlayHistory { get; set; }
    public string OriginalHash { get; set; }
    public DateTimeOffset LastVerified { get; set; }
}

public class PlayEventDto
{
    public DateTimeOffset Date { get; set; }
    public int Type { get; set; }
}