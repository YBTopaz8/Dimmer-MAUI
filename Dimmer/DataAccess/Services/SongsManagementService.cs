﻿
namespace Dimmer_MAUI.DataAccess.Services;

public class SongsManagementService : ISongsManagementService, IDisposable
{
    Realm db;

    public IList<SongsModelView> AllSongs { get; set; }
    public IList<AlbumModelView> AllAlbums { get; set; }
    public IDataBaseService DataBaseService { get; }
    public SongsManagementService(IDataBaseService dataBaseService)
    {
        DataBaseService = dataBaseService;
        db = DataBaseService.GetRealm();
        GetSongs();
    }


    public void GetSongs()
    {
        try
        {
            AllSongs?.Clear();
            var realmSongs = db.All<SongsModel>().OrderBy(x => x.DateAdded).ToList();
            AllSongs = new List<SongsModelView>(realmSongs.Select(song => new SongsModelView(song)));
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }

    }
    public void GetAlbums()
    {
        AllAlbums?.Clear();
        var realmAlbums = db.All<AlbumModel>().ToList();
        AllAlbums = new List<AlbumModelView>(realmAlbums.Select(album => new AlbumModelView(album)));
    }
    public async Task<bool> AddSongAsync(SongsModel song)
    {
        try
        {
            return await db.WriteAsync(() =>
            {
                db.Add(song);
                return true;
            });
        }
        catch (Exception ex)
        {
            throw new Exception("Failed while inserting Song " + ex.Message);
        }
    }

    public bool AddSongBatchAsync(IEnumerable<SongsModelView> songs)
    {
        try
        {
            var songsToAdd = songs
                .Where(song => !AllSongs.Any(s => s.Title == song.Title && s.DurationInSeconds == song.DurationInSeconds && s.ArtistName == song.ArtistName))
                .Select(song => new SongsModel(song))
                .ToList();

            
            db.Write(() =>
            {
                foreach (var song in songsToAdd)
                {
                    db.Add(song);
                    Debug.WriteLine("Added Song " + song.Title);
                }
            });
            GetSongs();
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Error when batch add song " + ex.Message);
            throw new Exception("Error when adding Songs in Batch " + ex.Message);
        }
    }

    public async Task<bool> UpdateSongDetailsAsync(SongsModelView songsModelView)
    {
        try
        {
            await db.WriteAsync(() =>
            {
                var existingSong = db.Find<SongsModel>(songsModelView.Id);

                if (existingSong != null)
                {
                    Debug.WriteLine("found song");
                    existingSong.IsFavorite = songsModelView.IsFavorite;
                    existingSong.IsPlaying = false;
                    existingSong.CoverImagePath = songsModelView.CoverImagePath;
                    existingSong.LastPlayed = songsModelView.LastPlayed;

                    if(existingSong.DatesPlayed.Count > songsModelView.DatesPlayed.Count)
                    {
                        existingSong.DatesPlayed.RemoveAt(existingSong.DatesPlayed.Count - 1);
                    }

                    if (existingSong.DatesPlayed.Count < songsModelView.DatesPlayed.Count)
                    {
                        foreach (var date in songsModelView.DatesPlayed)
                        {
                            if (!existingSong.DatesPlayed.Contains(date))
                            {
                                existingSong.DatesPlayed.Add(date);
                            }
                        }
                    }

                    if (existingSong.DatesSkipped.Count < songsModelView.DatesSkipped.Count)
                    {
                        foreach (var date in songsModelView.DatesSkipped)
                        {
                            if (!existingSong.DatesSkipped.Contains(date))
                            {
                                existingSong.DatesSkipped.Add(date);
                            }
                        }
                    }

                }
                else
                {
                    Debug.WriteLine("didn't found song");
                    var newSong = new SongsModel(songsModelView)
                    {
                        IsPlaying = false
                    };
                    db.Add(newSong, update: true);
                }
            });

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Error when updating song: " + ex.Message);
            return false;
        }
    }

    public async Task<bool> AddArtistsBatchAsync(IEnumerable<ArtistModelView> artistss)
    {
        try
        {
            var artists = new List<ArtistModel>();
            artists.AddRange(artistss.Select(art => new ArtistModel(art)));

            await db.WriteAsync(() =>
            {
                db.Add(artists);
            });
            return true;
        }
        catch (Exception ex)
        {

            Debug.WriteLine("Error when batchaddArtist " + ex.Message);
            return false;
        }
    }

    public void Dispose()
    {
        db?.Dispose();
    }

    public IList<ObjectId> GetSongsIDsFromAlbumID(ObjectId albumID)
    {
        try
        {
            
            var songLinks = db
                .All<AlbumArtistSongLink>() 
                .Where(link => link.AlbumId == albumID) 
                .ToList();

            var songIDs = songLinks.Select(link => link.SongId).ToList();

            return songIDs;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting songs by album and artist: {ex.Message}");
            return Enumerable.Empty<ObjectId>().ToList();
        }
    }

    public IList<ObjectId> GetSongsIDsFromArtistID(ObjectId artistID)
    {
        try
        {

            var songLinks = db
                .All<AlbumArtistSongLink>()
                .Where(link => link.ArtistId == artistID)
                .ToList();

            var songIDs = songLinks.Select(link => link.SongId).ToList();

            return songIDs;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting songs by album and artist: {ex.Message}");
            return Enumerable.Empty<ObjectId>().ToList();
        }
    }
    public int GetSongsCountFromAlbumID(ObjectId albumID)
    {
        try
        {
            // Get the count of songs linked to the specific album
            var songCount = db
                .All<AlbumArtistSongLink>()
                .Count(link => link.AlbumId == albumID);

            return songCount;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting song count for album: {ex.Message}");
            return 0; // Return 0 if there's an error
        }
    }

    public IList<AlbumModelView> GetAlbumsFromArtistOrSongID(ObjectId artistOrSongId, bool fromSong=false)
    {
        try
        {
            List<AlbumArtistSongLink>? songLinks = new();
            if (fromSong)
            {
                songLinks= db
                    .All<AlbumArtistSongLink>()
                    .Where(link => link.SongId == artistOrSongId)
                    .ToList();
            }
            else
            {
                songLinks = db
                    .All<AlbumArtistSongLink>()
                    .Where(link => link.ArtistId == artistOrSongId)
                    .ToList();
            }

            var albumIDs = songLinks
                .Select(link => link.AlbumId) 
                .Distinct() 
                .ToList();
            
            var realmAlbums = db.All<AlbumModel>().ToList();
            AllAlbums = new List<AlbumModelView>(realmAlbums.Select(album => new AlbumModelView(album)));

            var albumsFromArtist = AllAlbums
                .Where(album => albumIDs.Contains(album.Id)) 
                .ToList();

            return albumsFromArtist;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting albums from artist: {ex.Message}");
            return Enumerable.Empty<AlbumModelView>().ToList();
        }
    }

    public void UpdateAlbum(AlbumModelView album)
    {
        try
        {
            db.Write(() =>
            {
                var existingAlbum = db.Find<AlbumModel>(album.Id);

                if (existingAlbum != null)
                {
                    existingAlbum.ImagePath = album.AlbumImagePath;
                }
                else
                {
                    var newSong = new AlbumModel(album);
                    db.Add(newSong, update: true);
                }
            });

            
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Error when updating song: " + ex.Message);
            
        }

    }

    public (ObjectId artistID, ObjectId albumID) GetArtistAndAlbumIdFromSongId(ObjectId songId)
    {
        try
        {
            // Query the database for the AlbumArtistSongLink using the songId
            var link = db.All<AlbumArtistSongLink>()
                         .Where(link => link.SongId == songId)
                         .FirstOrDefault();

            // Check if the link was found to avoid null reference
            if (link == null)
            {
                return (ObjectId.Empty, ObjectId.Empty);
            }

            // Return both artistID and albumID as a tuple
            return (link.ArtistId, link.AlbumId);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            return (ObjectId.Empty, ObjectId.Empty);  // Return empty ObjectIds in case of error
        }
    }

}
