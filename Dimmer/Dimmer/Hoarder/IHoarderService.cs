using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Dimmer.Hoarder.Models;

namespace Dimmer.Hoarder;


public enum AudioQualityRating
{
    Unknown,
    LossyLow,       // < 128kbps MP3
    LossyHigh,      // > 256kbps MP3/AAC
    LosslessStandard, // 16-bit/44.1kHz FLAC/ALAC
    LosslessHighRes,  // > 24-bit/48kHz
    FakeLossless      // Detected upsample
}

public class FileIntegrityReport
{
    public bool IsCorrupted { get; set; }
    public string ChecksumMD5 { get; set; }
    public AudioQualityRating Quality { get; set; }
    public double TrueBitrate { get; set; } // Calculated from stream
    public List<string> Issues { get; set; } = new();
}

public interface IHoarderService
{
    // Integrity
    Task<FileIntegrityReport> AnalyzeFileIntegrityAsync(string filePath);
    Task<List<SongModelView>> FindDuplicatesByAudioHashAsync(IEnumerable<SongModelView> songs);

    // Organization
    Task SmartOrganizeLibraryAsync(string rootFolder, IEnumerable<SongModelView> songs, IProgress<double> progress);
    Task ExportSidecarMetadataAsync(SongModelView song);

    // Completionist
    Task<List<string>> GetMissingTracksFromAlbumAsync(AlbumModelView album);
    Task<OrganizationResult> OrganizeFilesBasedOnMetadataAsync(IEnumerable<SongModelView> songs, string targetRootPath, bool deleteEmptySourceFolders, IProgress<double> progress);
}
