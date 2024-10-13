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

            //var exp = new CsvExporter();

            //string dataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\DimmerDD", "SongssDataExport.txt");
            //exp.ExportSongsToCsv(realmSongs, dataPath);
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

    int count = 0;
    public bool UpdateSongDetails(SongsModelView songsModelView)
    {
        try
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                db.Write(() =>
                {
                    var existingSong = db.Find<SongsModel>(songsModelView.Id);

                    if (existingSong != null)
                    {
                        existingSong = new SongsModel(songsModelView);
                        existingSong.IsPlaying = false;
                    }
                    else
                    {
                        Debug.WriteLine("didn't found song");
                        var newSong = new SongsModel(songsModelView)
                        {
                            IsPlaying = false
                        };
                        db.Add(newSong, update: true);
                        Debug.WriteLine($"existing song datesplayedCount {existingSong.DatesPlayed.Count}");
                        Debug.WriteLine($"existing song DatesSkipped {existingSong.DatesSkipped.Count}");
                    }
                });
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

    public bool DeleteSongFromDB(ObjectId songID)
    {
        try
        {
            db.Write(() =>
            {
                var existingSong = db.Find<SongsModel>(songID);
                if (existingSong != null)
                {
                    db.Remove(existingSong);
                }                
            });

            GetSongs();
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

                // Add DatesPlayed as Action = 1
                foreach (var datePlayed in song.DatesPlayed)
                {
                    actionEntries.Add((1, datePlayed));
                }

                // Add DatesSkipped as Action = 0
                foreach (var dateSkipped in song.DatesSkipped)
                {
                    actionEntries.Add((0, dateSkipped));
                }

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