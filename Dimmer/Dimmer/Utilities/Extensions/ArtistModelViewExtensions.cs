using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Utilities.Extensions;

public static class ArtistModelViewExtensions
{





    public static void RefreshArtistsAndSongsFromDB(this AlbumModelView album, IRealmFactory realmFactory)
    {
        try
        {
            var realm = realmFactory.GetRealmInstance();
            var albInDb = realm.Find<AlbumModel>(album.Id);
            if (albInDb == null) return;

            // Process database updates
            ProcessAlbumDatabaseUpdates(realm, albInDb);

            // Refresh view model
            RefreshAlbumViewModel(album, albInDb);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }

    private static void ProcessAlbumDatabaseUpdates(Realm realm, AlbumModel albInDb)
    {
        var artistsInDb = albInDb.Artists.ToList();
        var songsInDb = albInDb.SongsInAlbum?.ToList() ?? new List<SongModel>();

        realm.Write(() =>
        {
            // Clean up duplicate artists in this album
            CleanupDuplicateAlbumArtists(albInDb);

            // Handle image path
            if (string.IsNullOrEmpty(albInDb.ImagePath))
            {
                var firstSongWithImg = songsInDb.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s.CoverImagePath));
                if (firstSongWithImg != null)
                {
                    albInDb.ImagePath = firstSongWithImg.CoverImagePath;
                }
            }
        });
    }

    private static void CleanupDuplicateAlbumArtists(AlbumModel album)
    {
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

    private static void RefreshAlbumViewModel(AlbumModelView album, AlbumModel albInDb)
    {
        var artistsInDb = albInDb.Artists.ToList();
        var songsInDb = albInDb.SongsInAlbum?.ToList() ?? new List<SongModel>();

        RxSchedulers.UI.ScheduleTo(() =>
        {
            // Refresh artists
            album.Artists ??= new();
            album.Artists.Clear();

            var artistViews = artistsInDb
                .Select(a => a.ToArtistModelView())
                .Where(a => a != null)
                .ToList();

            foreach (var artistView in artistViews)
            {
                album.Artists.Add(artistView);
            }

            // Refresh songs
            album.SongsInAlbum ??= new ObservableCollection<SongModelView>();
            album.SongsInAlbum.Clear();

            var songViews = songsInDb
                .Select(s => s.ToSongModelView())
                .Where(s => s != null)
                .ToList();

            foreach (var songView in songViews)
            {
                album.SongsInAlbum.Add(songView);
            }

            // Update image path if changed
            if (!string.IsNullOrEmpty(albInDb.ImagePath) && album.ImagePath != albInDb.ImagePath)
            {
                album.ImagePath = albInDb.ImagePath;
            }
        });
    }
}
