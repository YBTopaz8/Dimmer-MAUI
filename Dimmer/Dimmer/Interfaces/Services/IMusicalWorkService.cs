using Dimmer.Data.Models;
using MongoDB.Bson;

namespace Dimmer.Interfaces.Services;

/// <summary>
/// Service for managing Musical Works and their renditions.
/// Provides functionality for linking songs to works, querying renditions, and finding potential matches.
/// </summary>
public interface IMusicalWorkService
{
    /// <summary>
    /// Creates a new musical work
    /// </summary>
    /// <param name="title">Title of the work</param>
    /// <param name="composer">Primary composer</param>
    /// <param name="canonicalArtist">Primary artist</param>
    /// <returns>The created musical work</returns>
    MusicalWorkModel CreateWork(string title, string? composer = null, string? canonicalArtist = null);

    /// <summary>
    /// Links a song to a musical work as a rendition
    /// </summary>
    /// <param name="songId">ID of the song to link</param>
    /// <param name="workId">ID of the musical work</param>
    /// <returns>True if successful</returns>
    bool LinkSongToWork(ObjectId songId, ObjectId workId);

    /// <summary>
    /// Unlinks a song from its musical work
    /// </summary>
    /// <param name="songId">ID of the song to unlink</param>
    /// <returns>True if successful</returns>
    bool UnlinkSongFromWork(ObjectId songId);

    /// <summary>
    /// Gets all renditions of a musical work
    /// </summary>
    /// <param name="workId">ID of the musical work</param>
    /// <returns>List of songs that are renditions of this work</returns>
    List<SongModel> GetRenditions(ObjectId workId);

    /// <summary>
    /// Gets renditions filtered by specific criteria
    /// </summary>
    /// <param name="workId">ID of the musical work</param>
    /// <param name="instrumentalOnly">Filter for instrumental versions</param>
    /// <param name="liveOnly">Filter for live performances</param>
    /// <param name="instrumentation">Filter by specific instrumentation</param>
    /// <returns>Filtered list of renditions</returns>
    List<SongModel> GetFilteredRenditions(ObjectId workId, bool? instrumentalOnly = null, 
        bool? liveOnly = null, string? instrumentation = null);

    /// <summary>
    /// Finds potential matching works for a song based on title, duration, and artist
    /// </summary>
    /// <param name="songId">ID of the song to find matches for</param>
    /// <param name="maxResults">Maximum number of suggestions to return</param>
    /// <returns>List of suggested work matches with confidence scores</returns>
    List<WorkSuggestion> SuggestMatchingWorks(ObjectId songId, int maxResults = 5);

    /// <summary>
    /// Finds potential matching songs that could be renditions of a work
    /// </summary>
    /// <param name="workId">ID of the work</param>
    /// <param name="maxResults">Maximum number of suggestions</param>
    /// <returns>List of suggested songs with confidence scores</returns>
    List<SongSuggestion> SuggestMatchingSongs(ObjectId workId, int maxResults = 10);

    /// <summary>
    /// Gets a musical work by ID
    /// </summary>
    /// <param name="workId">ID of the work</param>
    /// <returns>The musical work or null</returns>
    MusicalWorkModel? GetWork(ObjectId workId);

    /// <summary>
    /// Gets all musical works
    /// </summary>
    /// <returns>List of all works</returns>
    List<MusicalWorkModel> GetAllWorks();

    /// <summary>
    /// Updates the aggregated statistics for a work based on its renditions
    /// </summary>
    /// <param name="workId">ID of the work to update</param>
    void UpdateWorkStatistics(ObjectId workId);

    /// <summary>
    /// Updates the rendition metadata for a song
    /// </summary>
    /// <param name="songId">ID of the song</param>
    /// <param name="renditionType">Type of rendition</param>
    /// <param name="instrumentation">Instrumentation details</param>
    /// <param name="isLive">Whether it's a live performance</param>
    /// <param name="isRemix">Whether it's a remix</param>
    /// <param name="isCover">Whether it's a cover</param>
    /// <param name="notes">Additional notes</param>
    /// <returns>True if successful</returns>
    bool UpdateRenditionMetadata(ObjectId songId, string? renditionType = null, 
        string? instrumentation = null, bool? isLive = null, bool? isRemix = null, 
        bool? isCover = null, string? notes = null);

    /// <summary>
    /// Deletes a musical work and unlinks all its renditions
    /// </summary>
    /// <param name="workId">ID of the work to delete</param>
    /// <returns>True if successful</returns>
    bool DeleteWork(ObjectId workId);

    /// <summary>
    /// Searches for works by title
    /// </summary>
    /// <param name="searchTerm">Search term</param>
    /// <returns>Matching works</returns>
    List<MusicalWorkModel> SearchWorksByTitle(string searchTerm);
}

/// <summary>
/// Represents a suggested musical work match for a song
/// </summary>
public class WorkSuggestion
{
    public MusicalWorkModel Work { get; set; } = null!;
    public double ConfidenceScore { get; set; }
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Represents a suggested song match for a work
/// </summary>
public class SongSuggestion
{
    public SongModel Song { get; set; } = null!;
    public double ConfidenceScore { get; set; }
    public string Reason { get; set; } = string.Empty;
}
