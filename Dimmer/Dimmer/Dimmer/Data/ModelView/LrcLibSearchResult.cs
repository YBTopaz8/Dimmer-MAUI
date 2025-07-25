using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Data.ModelView;

public class LrcLibSearchResult
{
    public int Id { get; set; }
    public string TrackName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ArtistName { get; set; } = string.Empty;
    public string AlbumName { get; set; } = string.Empty;
    public double Duration { get; set; }
    public string? SyncedLyrics { get; set; }
    public string? PlainLyrics { get; set; }
    public bool? Instrumental { get; set; }
}