namespace Dimmer.Utilities.StatsUtils;


public static class EmbeddedArtValidator
{
    // Accept 1 KB – 8 MB as sane art bounds; tweak as you like.
    const int MIN_BYTES = 1 * 1024;
    const int MAX_BYTES = 6 * 1024 * 1024;

    // Main API — returns the first valid embedded picture or null.
    public static PictureInfo? GetValidEmbeddedPicture(string audioPath)
    {
        if (string.IsNullOrWhiteSpace(audioPath) || !File.Exists(audioPath))
            return null;

        long fileLen;
        try { fileLen = new FileInfo(audioPath).Length; }
        catch { return null; }

        try
        {
            var track = new Track(audioPath);
            var pics = track.EmbeddedPictures; // ATL lazy-loads these

            if (pics is null || pics.Count == 0) return null;

            foreach (var p in pics)
            {
                if (IsValidEmbeddedImageBytes(p?.PictureData, fileLen, out var mime))
                    return p;
            }
        }
        catch
        {
            // Swallow: caller just wants "null if invalid/unreadable"
        }
        return null;
    }

    // Fast validation: bounds + header sniff + simple allowlist
    private static bool IsValidEmbeddedImageBytes(byte[]? data, long hostFileLen, out string mime)
    {
        mime = "";
        if (data is null) return false;

        // reject if suspiciously big/small
        if (data.Length < MIN_BYTES || data.Length > MAX_BYTES) return false;

        // reject if essentially the whole audio file (common bad read)
        // allow some slack (e.g., tags can be a couple MB on huge files)
        if (hostFileLen > 0 && data.Length > hostFileLen - 1024)
            return false;

        // header sniff
        if (IsJpeg(data)) { mime = "image/jpeg"; return true; }
        if (IsPng(data)) { mime = "image/png"; return true; }
        if (IsGif(data)) { mime = "image/gif"; return true; }
        if (IsBmp(data)) { mime = "image/bmp"; return true; }
        if (IsWebp(data)) { mime = "image/webp"; return true; }

        return false;
    }

    // ---- Magic-byte checks (no allocations) ----
    private static bool IsJpeg(byte[] d)
        => d.Length >= 4 && d[0] == 0xFF && d[1] == 0xD8 && d[^2] == 0xFF && d[^1] == 0xD9;

    private static bool IsPng(byte[] d)
        => d.Length >= 8 &&
           d[0] == 0x89 && d[1] == 0x50 && d[2] == 0x4E && d[3] == 0x47 &&
           d[4] == 0x0D && d[5] == 0x0A && d[6] == 0x1A && d[7] == 0x0A;

    private static bool IsGif(byte[] d)
        => d.Length >= 6 &&
           d[0] == 0x47 && d[1] == 0x49 && d[2] == 0x46 && d[3] == 0x38 &&
           (d[4] == 0x39 || d[4] == 0x37) && d[5] == 0x61; // GIF89a / GIF87a

    private static bool IsBmp(byte[] d)
        => d.Length >= 2 && d[0] == 0x42 && d[1] == 0x4D; // "BM"

    private static bool IsWebp(byte[] d)
        => d.Length >= 12 &&
           d[0] == 0x52 && d[1] == 0x49 && d[2] == 0x46 && d[3] == 0x46 &&   // "RIFF"
           d[8] == 0x57 && d[9] == 0x45 && d[10] == 0x42 && d[11] == 0x50;    // "WEBP"
}