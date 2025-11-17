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
