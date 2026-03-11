using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Utilities.Extensions;

public static class AlbumModelViewExtensions
{

    public static void RefreshAlbumAndSongsFromDB(this ArtistModelView art, IRealmFactory realmFactory)
    {
        try
        {
            var realm = realmFactory.GetRealmInstance();
            var artInDb = realm.Find<ArtistModel>(art.Id);
            if (artInDb == null) return;

            // Step 1: Process ALL database updates in a single write transaction
            ProcessArtistDatabaseUpdates(realm, artInDb);

            // Step 2: Refresh the view model on UI thread
            RefreshArtistViewModel(art, artInDb);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }

    private static void ProcessArtistDatabaseUpdates(Realm realm, ArtistModel artInDb)
    {
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

                // Ensure artist-album relationship exists exactly once
                if (!alb.Artists.Contains(artInDb))
                {
                    alb.Artists.Add(artInDb);
                }
            }

            // Handle songs without albums
            foreach (var song in songsInDb)
            {
                if (song.Album != null && !song.Album.Artists.Contains(artInDb))
                {
                    song.Album.Artists.Add(artInDb);
                }
            }
        });
    }

    private static void CleanupDuplicateArtistRelationships(Realm realm, ArtistModel artist)
    {
        // Find all albums that have this artist multiple times
        var albumsWithArtist = realm.All<AlbumModel>()
            .Where(a => a.Artists.Contains(artist))
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

    private static void RefreshArtistViewModel(ArtistModelView art, ArtistModel artInDb)
    {
        // Materialize fresh data
        var songsInDb = artInDb.Songs.AsEnumerable();
        var albumsInDb = artInDb.Albums.AsEnumerable();

        RxSchedulers.UI.ScheduleTo(() =>
        {
            // Refresh songs
            art.SongsByArtist ??= new ObservableCollection<SongModelView?>();
            art.SongsByArtist.Clear();

            var songViews = songsInDb
                .Select(s => s.ToSongModelView())
                .Where(s => s != null)
                .ToList();

            foreach (var songView in songViews)
            {
                art.SongsByArtist.Add(songView);
            }

            // Refresh albums
            art.AlbumsByArtist ??= new ObservableCollection<AlbumModelView?>();
            art.AlbumsByArtist.Clear();

            var albumViews = albumsInDb
                .Select(a => a.ToAlbumModelView())
                .Where(a => a != null)
                .ToList();

            foreach (var albumView in albumViews)
            {
                art.AlbumsByArtist.Add(albumView);
            }
        });
    }
}