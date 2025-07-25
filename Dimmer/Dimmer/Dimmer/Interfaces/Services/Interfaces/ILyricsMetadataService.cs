
using ATL;

namespace Dimmer.Interfaces.Services.Interfaces;
public interface ILyricsMetadataService
{
    /// <summary>
    /// Attempts to find synchronized lyrics for a given song from local sources.
    /// It checks in a specific order:
    /// 1. Embedded tags within the audio file.
    /// 2. An external .lrc file in the same directory as the song.
    /// </summary>
    /// <param name="song">The song to find lyrics for.</param>
    /// <returns>The raw LRC-formatted string content if found; otherwise, null.</returns>
    Task<string?> GetLocalLyricsAsync(SongModelView song);
    Task<bool> SaveLyricsForSongAsync(SongModelView song, string lrcContent, LyricsInfo lyrics);

    /// <summary>
    /// Searches for lyrics online using the LrcLib service.
    /// </summary>
    /// <param name="song">The song to search for.</param>
    /// <returns>A collection of potential search results, or an empty collection if none are found.</returns>
    Task<IEnumerable<LrcLibSearchResult>> SearchOnlineAsync(SongModelView song);
    Task<IEnumerable<LrcLibSearchResult>> SearchOnlineManualParamsAsync(string songName, string songArtist, string songAlbum);

    /// <summary>
    /// Saves the provided LRC content for a song. This involves two steps:
    /// 1. Writing the content to an .lrc file next to the audio file.
    /// 2. Updating the SyncLyrics property on the song's database record.
    /// </summary>
    /// <param name="song">The song to save lyrics for.</param>
    /// <param name="lrcContent">The full string content of the .lrc file.</param>
    /// <returns>A boolean indicating if the save operation was successful.</returns>
}

/// <summary>
/// A Data Transfer Object (DTO) representing a single search result from LrcLib.
/// </summary>