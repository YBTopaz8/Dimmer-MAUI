﻿namespace Dimmer_MAUI.DataAccess.Services;

public class SongsManagementService : ISongsManagementService, IDisposable
{
    Realm db;

    public IList<SongModelView> AllSongs { get; set; }
    public IList<AlbumArtistSongLink> AllLinks { get; set; }    
    public IList<AlbumModelView> AllAlbums { get; set; }
    public IList<GenreModelView> AllGenres { get; set; }
    public IDataBaseService DataBaseService { get; }
    public SongsManagementService(IDataBaseService dataBaseService)
    {
        DataBaseService = dataBaseService;
        GetSongs();
    }


    public void GetSongs()
    {
        try
        {
            db = Realm.GetInstance(DataBaseService.GetRealm());
            AllSongs?.Clear();
            var realmSongs = db.All<SongsModel>().OrderBy(x => x.DateAdded).ToList();
            AllSongs = new List<SongModelView>(realmSongs.Select(song => new SongModelView(song)));

            AllLinks = db.All<AlbumArtistSongLink>().ToList();
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }
    public void GetAlbums()
    {
        db = Realm.GetInstance(DataBaseService.GetRealm());
        AllAlbums?.Clear();
        var realmAlbums = db.All<AlbumModel>().ToList();
        AllAlbums = new List<AlbumModelView>(realmAlbums.Select(album => new AlbumModelView(album)));
        Debug.WriteLine(AllSongs.Count);
    }
    public async Task<bool> AddSongAsync(SongsModel song)
    {
        try
        {
            db = Realm.GetInstance(DataBaseService.GetRealm());
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

    public bool AddSongBatchAsync(IEnumerable<SongModelView> songs)
    {
        try
        {
            db = Realm.GetInstance(DataBaseService.GetRealm());

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

    public bool UpdateSongDetails(SongModelView songsModelView)
    {
        try
        {
            db = Realm.GetInstance(DataBaseService.GetRealm());
            
            db.Write(() =>
            {
                var existingSong = db.Find<SongsModel>(songsModelView.Id);
                if (existingSong is not null)
                {
                    existingSong = new SongsModel(songsModelView);
                    
                    db.Add(existingSong, update: true);
                    return;
                }
                
                var newSong = new SongsModel(songsModelView)
                {
                    IsPlaying = false
                };
                db.Add(newSong, update: true);
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
            db = Realm.GetInstance(DataBaseService.GetRealm());
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

  
    public int GetSongsCountFromAlbumID(ObjectId albumID)
    {
        try
        {
            db = Realm.GetInstance(DataBaseService.GetRealm());
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

  

    public void UpdateAlbum(AlbumModelView album)
    {
        try
        {
            db = Realm.GetInstance(DataBaseService.GetRealm());
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

    public async Task<bool> DeleteSongFromDB(ObjectId songID)
    {
        try
        {
            db = Realm.GetInstance(DataBaseService.GetRealm());
            await db.WriteAsync(() =>
            {
                var existingSong = db.Find<SongsModel>(songID);
                if (existingSong != null)
                {
                    var artistSongLinks = db.All<AlbumArtistSongLink>()
                                            .Where(link => link.SongId == songID)
                                            .ToList();

                    var artistIDs = artistSongLinks.Select(link => link.ArtistId).ToList();
                    var albumIDs = artistSongLinks.Select(link => link.AlbumId).ToList();

                    foreach (var link in artistSongLinks)
                    {
                        db.Remove(link);
                    }
                    db.Remove(existingSong);

                    foreach (var artistID in artistIDs)
                    {
                        bool isArtistLinkedToOtherSongs = db.All<AlbumArtistSongLink>()
                                                            .Any(link => link.ArtistId == artistID && link.SongId != songID);
                        if (!isArtistLinkedToOtherSongs)
                        {
                            var artistToDelete = db.Find<ArtistModel>(artistID);
                            if (artistToDelete != null)
                            {
                                db.Remove(artistToDelete);
                            }
                        }
                    }

                    foreach (var albumID in albumIDs)
                    {
                        bool isAlbumLinkedToOtherSongs = db.All<AlbumArtistSongLink>()
                                                          .Any(link => link.AlbumId == albumID && link.SongId != songID);
                        if (!isAlbumLinkedToOtherSongs)
                        {
                            var albumToDelete = db.Find<AlbumModel>(albumID);
                            if (albumToDelete != null)
                            {
                                db.Remove(albumToDelete);
                            }
                        }
                    }
                }
            });

            GetSongs(); // Update the list after deletion
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            return false;
        }
    }

    public async Task<bool> MultiDeleteSongFromDB(ObservableCollection<SongModelView> songs)
    {
        try
        {
            db = Realm.GetInstance(DataBaseService.GetRealm());
            // Use a write transaction to handle multiple deletions
            await db.WriteAsync(() =>
            {
                foreach (var song in songs)
                {
                    var songID = song.Id;
                    var existingSong = db.Find<SongsModel>(songID);
                    if (existingSong != null)
                    {
                        // Find all links related to the song
                        var artistSongLinks = db.All<AlbumArtistSongLink>()
                                                .Where(link => link.SongId == songID)
                                                .ToList();

                        // Collect artist IDs to check if they are linked to other songs later
                        var artistIDs = artistSongLinks.Select(link => link.ArtistId).ToList();

                        // Collect album IDs to check if they are linked to other songs later
                        var albumIDs = artistSongLinks.Select(link => link.AlbumId).ToList();

                        // Remove all links related to the song
                        foreach (var link in artistSongLinks)
                        {
                            db.Remove(link);
                        }

                        // Remove the song itself
                        db.Remove(existingSong);

                        // Check if any artist is no longer linked to any other song, and remove them if necessary
                        foreach (var artistID in artistIDs)
                        {
                            bool isArtistLinkedToOtherSongs = db.All<AlbumArtistSongLink>()
                                                                .Any(link => link.ArtistId == artistID && link.SongId != songID);
                            if (!isArtistLinkedToOtherSongs)
                            {
                                var artistToDelete = db.Find<ArtistModel>(artistID);
                                if (artistToDelete != null)
                                {
                                    db.Remove(artistToDelete);
                                }
                            }
                        }

                        // Check if any album is no longer linked to any other song, and remove them if necessary
                        foreach (var albumID in albumIDs)
                        {
                            bool isAlbumLinkedToOtherSongs = db.All<AlbumArtistSongLink>()
                                                              .Any(link => link.AlbumId == albumID && link.SongId != songID);
                            if (!isAlbumLinkedToOtherSongs)
                            {
                                var albumToDelete = db.Find<AlbumModel>(albumID);
                                if (albumToDelete != null)
                                {
                                    db.Remove(albumToDelete);
                                }
                            }
                        }
                    }
                }
            });

            GetSongs(); // Update the list after deletion
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            return false;
        }
    }
}
public class CsvExporter
{
    /// <summary>
    /// Exports a list of SongsModel to a CSV file with specified columns.
    /// </summary>
    /// <param name="songs">List of SongsModel instances.</param>
    /// <param name="csvFilePath">Path to the output CSV file.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    public void ExportSongsToCsv(List<SongsModel> songs, string csvFilePath)
    {
        // Define the CSV headers
        string[] headers = new string[]
        {
            "Title",
            "ArtistName",
            "AlbumName",
            "Genre",
            "DurationInSeconds",
            "Action",
            "ActionDate"
        };

        // Open the file with a StreamWriter and include BOM for UTF-8
        using (var writer = new StreamWriter(csvFilePath, false, new UTF8Encoding(true)))
        {
            // Write the header line
             writer.WriteLine(string.Join(",", headers));

            // Iterate through each song
            foreach (var song in songs)
            {
                // Create a list to hold all action dates with their corresponding action
                var actionEntries = new List<(int Action, DateTimeOffset ActionDate)>();

                //// Add DatesPlayed as Action = 1
                //foreach (var datePlayed in song.DatesPlayed)
                //{
                //    actionEntries.Add((1, datePlayed));
                //}

                //// Add DatesSkipped as Action = 0
                //foreach (var dateSkipped in song.DatesSkipped)
                //{
                //    actionEntries.Add((0, dateSkipped));
                //}

                // Only proceed if there are any action entries
                if (actionEntries.Any())
                {
                    // Sort the combined list by ActionDate in ascending order
                    var sortedActions = actionEntries
                        .OrderBy(a => a.ActionDate)
                        .ToList();

                    foreach (var entry in sortedActions)
                    {
                        if (entry.Action != 1 && entry.Action != 0)
                        {
                            Debug.WriteLine("Skipped!!");
                        }

                        var row = new List<string>
                            {
                                EscapeCsvField(song.Title),
                                EscapeCsvField(song.ArtistName),
                                EscapeCsvField(song.AlbumName),
                                EscapeCsvField(string.IsNullOrWhiteSpace(song.Genre) ? "Unknown" : song.Genre),  // Handle empty genre
                                song.DurationInSeconds.ToString(CultureInfo.InvariantCulture),
                                entry.Action.ToString(),
                                entry.ActionDate.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
                            };
                         writer.WriteLine(string.Join(",", row));
                    }
                }
                // If there are no action entries, do not write any row for this song
            }
        }

        Console.WriteLine($"Data successfully exported to {csvFilePath}");
    }

    /// <summary>
    /// Escapes a CSV field by enclosing it in quotes if it contains special characters.
    /// Doubles any existing quotes within the field.
    /// </summary>
    /// <param name="field">The CSV field to escape.</param>
    /// <returns>The escaped CSV field.</returns>
    private string EscapeCsvField(string field)
    {
        if (field.Contains(",") || field.Contains("\"") || field.Contains("\n"))
        {
            // Escape quotes by doubling them
            string escaped = field.Replace("\"", "\"\"");
            return $"\"{escaped}\"";
        }
        return field;
    }
}