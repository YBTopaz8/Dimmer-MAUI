using Avalonia.Controls.Shapes;

namespace Dimmer.Utilities.Extensions;

public static class AlbumModelViewExtensions
{

    public static void RefreshAlbumAndSongsFromDB(this ArtistModelView art, IRealmFactory realmFactory,bool IncludeSongsInAlbum=false)
    {
        try
        {
            var realm = realmFactory.GetRealmInstance();
            var artInDb = realm.Find<ArtistModel>(art.Id);
            if (artInDb == null) return;

            // Step 1: Process ALL database updates in a single write transaction
           RxSchedulers.Background.ScheduleTo(()=> ProcessArtistDatabaseUpdates(realmFactory, art.Id));

            // Step 2: Refresh the view model on UI thread
            RefreshArtistViewModel(art, artInDb, IncludeSongsInAlbum: IncludeSongsInAlbum);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }

    private static void ProcessArtistDatabaseUpdates(IRealmFactory realmF, ObjectId artId)
    {
        var realm = realmF.GetRealmInstance();
        var artInDb = realm.Find<ArtistModel>(artId); 
        
        if (artInDb == null) return;
        // Materialize once
        var songsInDb = artInDb.Songs.ToList();
        var albumsInDb = artInDb.Albums.ToList();

        realm.Write(() =>
        {
            // First, clean up any existing duplicate relationships
            CleanupDuplicateArtistRelationships(realm, artInDb);

            // Update album stats
            foreach (var alb in albumsInDb)
            {
                if (alb == null) continue;

                var songsInAlbum = alb.SongsInAlbum?.ToList() ?? new List<SongModel>();

                alb.NumberOfTracks = songsInAlbum.Count;
                alb.TotalCompletedPlays = songsInAlbum.Sum(s => s.PlayCompletedCount);
                alb.TotalDuration = songsInAlbum.Sum(s => s.DurationInSeconds).ToString();
                alb.TotalSkipCount = songsInAlbum.Sum(x => x.SkipCount);
                if(string.IsNullOrEmpty(alb.ImagePath) || alb.ImagePath== "musicalbum.png")
                {
                    alb.ImagePath = songsInAlbum.FirstOrDefault(x => !string.IsNullOrEmpty(x.CoverImagePath))?.CoverImagePath;
                   realm.Add(alb,update:true);
                }
                // Ensure artist-album relationship exists exactly once
                if (!alb.Artists.AsEnumerable().Contains(artInDb))
                {
                    alb.Artists.Add(artInDb);
                }
            }

            // Handle songs without albums
            foreach (var song in songsInDb)
            {
                if (song.Album != null && !song.Album.Artists.AsEnumerable().Contains(artInDb))
                {
                    song.Album.Artists.Add(artInDb);
                }
            }
        });
    }

    private static void CleanupDuplicateArtistRelationships(Realm realm, ArtistModel artist)
    {
        // Find all albums that have this artist multiple times
        var albumsWithArtist = realm.All<AlbumModel>().AsEnumerable()
            .Where(a => a.Artists.AsEnumerable().Contains(artist))
            .ToList();

        foreach (var album in albumsWithArtist)
        {
            // Remove duplicates while keeping one instance
            var uniqueArtists = album.Artists.Distinct().ToList();
            if (uniqueArtists.Count != album.Artists.Count)
            {
                album.Artists.Clear();
                foreach (var uniqueArtist in uniqueArtists)
                {
                    album.Artists.Add(uniqueArtist);
                }
            }
        }
    }
    private static void RefreshArtistViewModel(ArtistModelView art, ArtistModel artInDb, bool IncludePlayEvents = false, bool IncludeSongsInAlbum = false)
    {
        // ========================================================
        // PHASE 1: BACKGROUND / WORKER THREAD (Heavy computations)
        // ========================================================

        // 1. Materialize Songs
        var songViews = artInDb.Songs.AsEnumerable()
            .Select(s => s.ToSongModelView(isShallow: true))
            .Where(s => s != null)
            .ToList();

        // 2. Materialize Albums & Songs in Albums
        var artistId = artInDb.Id;
        var artistName = artInDb.Name;

        var albumViews = new List<AlbumModelView>();
       
        foreach (var alb in artInDb.Albums)
        {
            var albView = alb.ToAlbumModelView(withArtist: false, withSongs: false);
            if (albView == null) continue;

            if (IncludeSongsInAlbum && alb.SongsInAlbum != null)
            {
                var songsInAlbumViews = new List<SongModelView>();

                foreach (var songDb in alb.SongsInAlbum)
                {
                    var songView = songDb.ToSongModelView(isShallow: true);
                    if (songView == null) continue;

                  
                    bool isPrimary = (songDb.Artist != null && songDb.Artist.Id == artistId) || songDb.ArtistName == artistName;
                    bool isCollab = songDb.ArtistToSong != null && songDb.ArtistToSong.Any(x => x.Id == artistId);
                    bool inOtherText = !string.IsNullOrEmpty(songDb.OtherArtistsName) &&
                                       songDb.OtherArtistsName.Contains(artistName, StringComparison.OrdinalIgnoreCase);

                    songView.IsBySelectedArtist = isPrimary || isCollab || inOtherText;
                    songsInAlbumViews.Add(songView);
                }

                albView.SongsInAlbum = songsInAlbumViews.ToObservableCollection();
                albView.Artists = alb.Artists.Select(x => x.ToArtistModelView()!).ToList();
                // Set cover if empty
                if (string.IsNullOrEmpty(albView.ImagePath))
                {
                    albView.ImagePath = alb.SongsInAlbum
                        .FirstOrDefault(s => !string.IsNullOrEmpty(s.CoverImagePath))?.CoverImagePath;
                }
            }

            albumViews.Add(albView);
        }

        // 3. Play History (ONLY read if requested!)
        List<DimmerPlayEventView>? playEventViews = null;
        if (IncludePlayEvents)
        {
            playEventViews = artInDb.Songs.AsEnumerable()
                .SelectMany(s => s.PlayHistory)
                .Select(e => e.ToDimmerPlayEventView())
                .Where(e => e != null)
                .ToList()!;
        }

        // ========================================================
        // PHASE 2: UI THREAD (Fast swap only)
        // ========================================================
        RxSchedulers.UI.ScheduleTo(() =>
        {
            // 1. Assign Songs
            art.SongsByArtist ??= new ObservableCollection<SongModelView?>();
            art.SongsByArtist.Clear();
            art.SongsByArtist.AddRange(songViews);

            // 2. Assign Albums
            art.AlbumsByArtist ??= new ObservableCollection<AlbumModelView?>();
            art.AlbumsByArtist.Clear();
            art.AlbumsByArtist.AddRange(albumViews);

            // 3. Assign Play Events
            if (IncludePlayEvents && playEventViews != null)
            {
                art.PlayEvents ??= new();
                art.PlayEvents.Clear();
                art.PlayEvents.AddRange(playEventViews);
            }
        });
    }
}