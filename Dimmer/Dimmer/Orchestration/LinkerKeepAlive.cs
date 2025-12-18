using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Orchestration;

public static class LinkerKeepAlive
{
    // Call this method from App.xaml.cs or MauiProgram.cs
    // It doesn't need to do anything, just be reachable.
    public static void Keep()
    {
        // 1. Force SongModel Properties
        var s = new SongModel();
        s.OtherArtistsName = "";
        s.AlbumName = "";
        s.Genre = new GenreModel(); // Ensure nested types are kept

        // 2. Force SongModelView Properties (CRITICAL)
        // The linker is likely stripping the SETTERS on these
        var sv = new SongModelView();
        sv.ArtistName = "";
        sv.AlbumName = "";
        sv.GenreName = "";
        sv.PlayEvents = null;

        // 3. Force SyncLyrics (EmbeddedSync)
        var sync = new SyncLyrics();
        sync.Text = "";
        sync.TimestampMs = 0;

        // 4. Force LastFMUser
        var lfm = new LastFMUser();
        var img = new LastFMUser.LastImage();
        img.Url = "";

        // 5. Force DimmerPlayEvent
        var evt = new DimmerPlayEvent();
    }
}