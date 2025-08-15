using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Interfaces.Services.Lyrics;
public class LocalLrcFileProvider : ILyricsProvider
{
    public string ProviderName => "Local .lrc File";
    public bool IsOnlineProvider => false;

    public async Task<LyricsResult> GetLyricsAsync(SongModelView song)
    {
        // All the logic from your old GetExternalLrcFileAsync method goes here.
        string lrcPath = Path.ChangeExtension(song.FilePath, ".lrc");
        if (File.Exists(lrcPath))
        {
            try
            {
                string content = await File.ReadAllTextAsync(lrcPath);
                return LyricsResult.Success(content, ProviderName);
            }
            catch (Exception ex)
            {
                // Log the error
            }
        }
        return LyricsResult.Fail(ProviderName);
    }
}