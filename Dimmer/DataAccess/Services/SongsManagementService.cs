using Parse.Infrastructure;
namespace Dimmer_MAUI.DataAccess.Services;

public partial class SongsManagementService : ISongsManagementService, IDisposable
{
    Realm db;
    
    public List<SongModelView> AllSongs { get; set; }
    public List<PlayDataLink> AllPlayDataLinks { get; set; }
    public List<PlaylistSongLink> PlaylistSongsLinksCol { get; set; }
    HomePageVM MyViewModel { get; set; }

    public List<AlbumModelView> AllAlbums { get; set; }
    public List<ArtistModelView> AllArtists { get; set; }    
    public List<GenreModelView> AllGenres { get; set; }
    public List<AlbumArtistGenreSongLinkView> AllLinks { get; set; } = [];
    public IDataBaseService DataBaseService { get; }


    public SongsManagementService(IDataBaseService dataBaseService)
    {
        DataBaseService = dataBaseService;

        GetUserAccount();
        GetSongs();
    }
    bool HasOnlineSyncOn;
    public ParseUser? CurrentUserOnline { get; set; }
    public void InitApp(HomePageVM vm)
    {        
        MyViewModel = vm;
    }

    bool isSyncingOnline;

    public void RestoreAllOnlineData(List<PlayDateAndCompletionStateSongLink> playDataLinks, List<SongModel> songs,
        List<AlbumModel> albums, List<GenreModel> allGenres,
        List<PlaylistModel> allPlaylists, List<AlbumArtistGenreSongLink> otherLinks)
    {
        db = Realm.GetInstance(DataBaseService.GetRealm());
        
        db.Write(() =>
        {
            db.Add(playDataLinks, update:true);
            db.Add(songs, update: true);
            db.Add(albums, update: true);
            db.Add(allGenres, update: true);
            db.Add(allPlaylists, update: true);
            db.Add(otherLinks, update: true);
        });

        GetSongs();
    }

    public void GetSongs()
    {
        try
        {
            db = Realm.GetInstance(DataBaseService.GetRealm());

            var realmLinks = db.All<AlbumArtistGenreSongLink>().ToList();
            AllLinks = [.. realmLinks.Select(link => new AlbumArtistGenreSongLinkView(link))];
            AllLinks ??= Enumerable.Empty<AlbumArtistGenreSongLinkView>().ToList();

            AllSongs = [];

            AllSongs.Clear();

            var realmSongs = db.All<SongModel>().OrderBy(x => x.DateCreated).ToList();
            AllSongs = [.. realmSongs.Select(song => new SongModelView(song))];

            GetAlbums();
            GetArtists();
            GetGenres();
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    public void GetGenres()
    {
        db = Realm.GetInstance(DataBaseService.GetRealm());
        AllGenres?.Clear();
        var realmSongs = db.All<GenreModel>().ToList();
        AllGenres = [.. realmSongs.Select(genre => new GenreModelView(genre)).OrderBy(x => x.Name)];
    }
    public void GetArtists()
    {
        try
        {
            db = Realm.GetInstance(DataBaseService.GetRealm());
            var realmArtists = db.All<ArtistModel>().ToList();

            AllArtists = [.. realmArtists.Select(artist => new ArtistModelView(artist)).OrderBy(x => x.Name)];
            AllArtists ??= Enumerable.Empty<ArtistModelView>().ToList();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting Artists: {ex.Message}");
        }
    }

    public void GetAlbums()
    {
        db = Realm.GetInstance(DataBaseService.GetRealm());
        AllAlbums?.Clear();
        var realmAlbums = db.All<AlbumModel>().ToList();
        AllAlbums = [.. realmAlbums.Select(album => new AlbumModelView(album)).OrderBy(x=>x.Name)];
    }

    public void AddPlayData(string songId, PlayDataLink playData)
    {
        // 1. Add to the specific SongModelView's PlayDates
        var songView = AllSongs.FirstOrDefault(s => s.LocalDeviceId == songId);
        if (songView != null)
        {
            songView.PlayData.Add(playData);
            songView.NumberOfTimesPlayed = songView.PlayData.Count;
            songView.NumberOfTimesPlayedCompletely = songView.PlayData.Count(p => p.WasPlayCompleted);
        }
        
        // 2. Save to the database (Realm) separately
        using (var realm = Realm.GetInstance(DataBaseService.GetRealm()))
        {
            realm.Write(() =>
            {
                realm.Add(new PlayDateAndCompletionStateSongLink
                {
                    LocalDeviceId = playData.LocalDeviceId,
                    SongId = playData.SongId,
                    DatePlayed = playData.EventDate.ToUniversalTime(),
                    //DateFinished = playData.EventDate.ToUniversalTime(),
                    WasPlayCompleted = playData.WasPlayCompleted,
                    PositionInSeconds = playData.PositionInSeconds,
                    PlayType = playData.PlayType
                });
            });
        }


    }


    List<AlbumModel>? realmAlbums { get; set; }
    List<SongModel>? realmSongs { get; set; }
    List<GenreModel>? realGenres { get; set; }
    List<ArtistModel>? realmArtists { get; set; }
    List<AlbumArtistGenreSongLink>? realmAAGSL { get; set; }
    void GetInitialValues()
    {
        db = Realm.GetInstance(DataBaseService.GetRealm());
        realmSongs = [.. db.All<SongModel>()];
        realmAlbums = [.. db.All<AlbumModel>()];
        realGenres = [.. db.All<GenreModel>()];
        realmArtists = [.. db.All<ArtistModel>()];
        realmAAGSL = [.. db.All<AlbumArtistGenreSongLink>()];
        
    }

    private Dictionary<string, ArtistModelView> artistDict = [];
    public bool AddSongBatchAsync(IEnumerable<SongModelView> songs)
    {
        try
        {
            db = Realm.GetInstance(DataBaseService.GetRealm());

            var songsToAdd = songs
                .Where(song => !AllSongs.Any(s => s.Title == song.Title && s.DurationInSeconds == song.DurationInSeconds && s.ArtistName == song.ArtistName))
                .Select(song => new SongModel(song))
                .ToList();


            db.Write(() =>
            {
                db.Add(songsToAdd);
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

    public bool UpdateSongDBDataAndSongFile(SongModelView song)
    {
        if (UpdateSongDetails(song))
        {
            Track file = new(song.FilePath)
            {
                Title = song.Title,
                Artist = song.ArtistName,
                Album = song.AlbumName,
                Genre = song.GenreName,
                TrackNumber = song.TrackNumber
            };
            file.SaveTo(song.FilePath);
            GeneralStaticUtilities.ShowNotificationInternally($"{song.Title} was updated in db ");
            return true;

        }
        return false;

    }
    public bool UpdateSongDetails(SongModelView songsModelView)
    {
        try
        {

            db = Realm.GetInstance(DataBaseService.GetRealm());
            db.Write(() =>
            {
                var existingSong = db.All<SongModel>()
                .Where(x => x.LocalDeviceId == songsModelView.LocalDeviceId)
                .ToList();

                if (existingSong.Count == 0)
                {
                    // Fallback to the longer check if no matches by LocalDeviceID
                    existingSong = [.. db.All<SongModel>()
                        .Where(x => x.Title == songsModelView.Title &&
                                    x.DurationInSeconds == songsModelView.DurationInSeconds &&
                                    x.ArtistName == songsModelView.ArtistName)];
                    if (existingSong is not null && existingSong.Count > 0)
                    {
                        return;
                    }
                }


                SongModel song = new(songsModelView)
                {
                    UserIDOnline = CurrentUserOnline?.ObjectId
                };

                var ex = existingSong.FirstOrDefault();
                if (ex is not null)
                {
                    song.LocalDeviceId = ex.LocalDeviceId;
                }
                song.IsPlaying = false;
                

                db.Add(song, update: true);
            });


            GeneralStaticUtilities.ShowNotificationInternally($"{songsModelView.Title} was updated" );
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Error when updating songss: " + ex.Message);
            return false;
        }
    }


    public bool AddArtistsBatch(IEnumerable<ArtistModelView> artistss)
    {
        try
        {
            db = Realm.GetInstance(DataBaseService.GetRealm());
            var artists = new List<ArtistModel>();
            artists.AddRange(artistss.Select(art => new ArtistModel(art)));

            db.Write(() =>
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

    public void UpdateAlbum(AlbumModelView album)
    {
        try
        {
            db = Realm.GetInstance(DataBaseService.GetRealm());
            db.Write(() =>
            {
                var existingAlbum = db.Find<AlbumModel>(album.LocalDeviceId);

                if (existingAlbum != null)
                {
                    existingAlbum.ImagePath = album.AlbumImagePath;
                    existingAlbum.NumberOfTracks = album.NumberOfTracks;
                    existingAlbum.TotalDuration = album.TotalDuration;
                    existingAlbum.Description = album.Description;
                    db.Add(existingAlbum, update: true);
                }
                else
                {
                    var newSong = new AlbumModel(album);
                    db.Add(newSong);
                }
            });

        }
        catch (Exception ex)
        {
            Debug.WriteLine("Error when updatingasd song: " + ex.Message);

        }

    }

    public bool DeleteSongFromDB(SongModelView song)
    {
        song.IsDeleted = true;

        return UpdateSongDetails(song);
    }

    public async Task<bool> MultiDeleteSongFromDB(ObservableCollection<SongModelView> songs)
    {
        try
        {
            db = Realm.GetInstance(DataBaseService.GetRealm());
            await db.WriteAsync(() =>
            {
                foreach (var song in songs)
                {
                    var songID = song.LocalDeviceId;
                    var existingSong = db.Find<SongModel>(songID);
                    if (existingSong != null)
                    {
                        var artistSongLinks = db.All<AlbumArtistGenreSongLink>()
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
                            bool isArtistLinkedToOtherSongs = db.All<AlbumArtistGenreSongLink>()
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
                            bool isAlbumLinkedToOtherSongs = db.All<AlbumArtistGenreSongLink>()
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

            GetSongs();
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            return false;
        }
    }
    public UserModelView CurrentOfflineUser { get; set; }

    // Custom class to hold all song data results
    public class LoadSongsResult
    {
        public required List<ArtistModel> Artists { get; set; }
        public required List<AlbumModel> Albums { get; set; }
        public required List<AlbumArtistGenreSongLink> Links { get; set; }
        public required List<SongModel> Songs { get; set; }
        public required List<GenreModel> Genres { get; set; }
    }

    public async Task<bool> LoadSongsFromFolderAsync(List<string> folderPaths, Subject<SongLoadProgress> loadingSubj)
    {
     
        // Run the file scan and processing on a background thread.
        var result = await Task.Run(() => LoadSongsAsync(folderPaths, loadingSubj));

        if (result == null || result.Songs == null || result.Artists == null ||
            result.Albums == null || result.Genres == null)
        {
            await Shell.Current.DisplayAlert("Error during Scan", "No Songs to Scan", "OK");
            return false;
        }

        // Remove duplicates using DistinctBy (make sure you have System.Linq)
        result.Songs = result.Songs.DistinctBy(x => new { x.Title, x.DurationInSeconds, x.AlbumName, x.ArtistName }).ToList();
        result.Artists = result.Artists.DistinctBy(x => x.Name).ToList();
        result.Albums = result.Albums.DistinctBy(x => x.Name).ToList();
        result.Genres = result.Genres.DistinctBy(x => x.Name).ToList();
        result.Links = result.Links.DistinctBy(x => new { x.ArtistId, x.AlbumId, x.SongId, x.GenreId }).ToList();

        AppSettingsService.RepeatModePreference.RepeatState = 1; // 0: OFF, 1: ALL, 2: ONE

        SyncAllDataToDatabase(db, result.Songs, result.Artists, result.Albums, result.Genres, result.Links, null);

        await Shell.Current.DisplayAlert("Scan Completed", "All Songs have been scanned", "OK");
        GetSongs();
        MyViewModel.SetPlayerState(MediaPlayerState.DoneScanningData);

        // User authentication checks
        if (CurrentUserOnline == null || await CurrentUserOnline.IsAuthenticatedAsync())
        {
            return false;
        }
        if (CurrentUserOnline == null || !await CurrentUserOnline.IsAuthenticatedAsync())
        {
            Debug.WriteLine("User authentication failed.");
            return false;
        }

        return true;
    }

    private LoadSongsResult? LoadSongsAsync(List<string> folderPaths, IObserver<SongLoadProgress> progressObserver)
    {
        var allFiles = MusicFileProcessor.GetAllFiles(folderPaths);
        Debug.WriteLine("Got All Files");

        if (allFiles.Count == 0)
        {
            return null;
        }

        GetInitialValues();

        // Use existing data or empty lists if null.
        var existingArtists = realmArtists ?? new List<ArtistModel>();
        var existingLinks = realmAAGSL ?? new List<AlbumArtistGenreSongLink>();
        var existingAlbums = realmAlbums ?? new List<AlbumModel>();
        var existingGenres = realGenres ?? new List<GenreModel>();
        var oldSongs = realmSongs ?? new List<SongModel>();

        var newArtists = new List<ArtistModel>();
        var newAlbums = new List<AlbumModel>();
        var newLinks = new List<AlbumArtistGenreSongLink>();
        var newGenres = new List<GenreModel>();
        var allSongs = new List<SongModel>();

        // Dictionaries to prevent duplicate processing.
        var artistDict = new Dictionary<string, ArtistModel>(StringComparer.OrdinalIgnoreCase);
        var albumDict = new Dictionary<string, AlbumModel>();
        var genreDict = new Dictionary<string, GenreModel>();

        int totalFiles = allFiles.Count;
        int processedFiles = 0;
        
        foreach (var file in allFiles)
        {
            processedFiles++;
            if (MusicFileProcessor.IsValidFile(file))
            {
                var songData = MusicFileProcessor.ProcessFile(
                    file,
                    existingAlbums, albumDict, newAlbums, oldSongs,
                    newArtists, artistDict, newLinks, existingLinks, existingArtists,
                    newGenres, genreDict, existingGenres);

                if (songData != null)
                {
                    allSongs.Add(songData);

                    //// Publish progress after adding a song.
                    progressObserver.OnNext(new SongLoadProgress
                    {
                        ProcessedFiles = processedFiles,
                        TotalFiles = totalFiles,
                        LatestSong = songData,
                        ProgressPercent = (double)processedFiles / totalFiles * 100.0
                    });
                }
            }
        }
        Debug.WriteLine("All files processed.");

        //progressObserver?.OnCompleted();
        return new LoadSongsResult
        {
            Artists = newArtists,
            Albums = newAlbums,
            Links = newLinks,
            Songs = allSongs,
            Genres = newGenres
        };
    }
 



    /// <summary>
    /// Syncs the provided data to the local Realm database
    /// </summary>
    /// <param name="db"></param>
    /// <param name="songs"></param>
    /// <param name="artistModels"></param>
    /// <param name="albumModels"></param>
    /// <param name="genreModels"></param>
    /// <param name="AAGSLink"></param>
    /// <param name="PDaCSLink"></param>
    /// <returns></returns>
    public bool SyncAllDataToDatabase(
    Realm db,
    IEnumerable<SongModel> songs,
    IEnumerable<ArtistModel> artistModels,
    IEnumerable<AlbumModel> albumModels,
    IEnumerable<GenreModel> genreModels,
    IEnumerable<AlbumArtistGenreSongLink> AAGSLink,
     IEnumerable<PlayDataLink>? PDaCSLink)
    {
        try
        {
            db = Realm.GetInstance(DataBaseService.GetRealm());
            // Ensure UserModel exists
            var userList = db.All<UserModel>().ToList();
            var user = userList.FirstOrDefault();

            if (user == null)
            {
                db.Write(() =>
                {
                    user = new UserModel
                    {
                        // Set other properties as needed
                    };
                    db.Add(user);
                });
            }

            // Sync Songs
            AddOrUpdateMultipleRealmItems(

                songs,
                song => db.All<SongModel>().Any(s => s.Title == song.Title && s.ArtistName == song.ArtistName)
            );

            // Sync Artists
            AddOrUpdateMultipleRealmItems(

                artistModels,
                artist => db.All<ArtistModel>().Any(a => a.Name == artist.Name)
            );

            // Sync Albums
            AddOrUpdateMultipleRealmItems(

                albumModels,
                album => db.All<AlbumModel>().Any(a => a.Name == album.Name)
            );

            // Sync Genres
            AddOrUpdateMultipleRealmItems(

                genreModels,
                genre => db.All<GenreModel>().Any(g => g.Name == genre.Name)
            );

            // Sync AlbumArtistGenreSongLinks
            AddOrUpdateMultipleRealmItems(

                AAGSLink,
                link => db.Find<AlbumArtistGenreSongLink>(link.LocalDeviceId) != null
            );


            Debug.WriteLine("All data synced to database.");
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error syncing data: {ex.Message}");
            return false;
        }
    }

   

    /// <summary>
    /// Adds or updates a collection of items in the specified Realm database.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="db"></param>
    /// <param name="items"></param>
    /// <param name="existsCondition"></param>
    /// <param name="updateAction"></param>
    public void AddOrUpdateMultipleRealmItems<T>(IEnumerable<T> items, Func<T, bool> existsCondition, Action<T>? updateAction = null) where T : RealmObject
    {
        db = Realm.GetInstance(DataBaseService.GetRealm());
        db.Write(() =>
        {
            foreach (var item in items)
            {
                if (!db.All<T>().Any(existsCondition))
                {
                    db.Add(item);

                }
                else
                {
                    updateAction?.Invoke(item); // Perform additional updates if needed
                    db.Add(item, update: true); // Update existing item

                }
            }
        });
    }

    /// <summary>
    /// Adds or updates a single item in the specified Realm database.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="db"></param>
    /// <param name="item"></param>
    /// <param name="existsCondition"></param>
    /// <param name="updateAction"></param>
    public void AddOrUpdateSingleRealmItem<T>(Realm db, T item, Func<T, bool>? existsCondition = null, Action<T>? updateAction = null) where T : RealmObject
    {

        db = Realm.GetInstance(DataBaseService.GetRealm());
        db.Write(() =>
        {
            if (existsCondition is null)
            {
                db.Add(item);
                return;
            }
            if (!db.All<T>().Any(existsCondition))
            {
                db.Add(item);
            }
            else
            {
                updateAction?.Invoke(item); // Perform additional updates if needed
                db.Add(item, update: true); // Update existing item

            }
        });
    }



    public async Task OpenConnectPopup()
    {
        ConnectOnline();

        CurrentUserOnline = await ParseClient.Instance.GetCurrentUser();
        if (CurrentUserOnline is null || !await CurrentUserOnline.IsAuthenticatedAsync())
        {
            Debug.WriteLine("User authentication failed.");
            return;
        }
        CurrentOfflineUser.UserIDOnline = CurrentUserOnline.ObjectId;
        CurrentOfflineUser.IsAuthenticated = await CurrentUserOnline.IsAuthenticatedAsync();
        ;
        CurrentOfflineUser.UserName = CurrentUserOnline.Username;
        CurrentOfflineUser.UserEmail= CurrentUserOnline.Email;


        MyViewModel.CurrentUser = CurrentOfflineUser;
        db = Realm.GetInstance(DataBaseService.GetRealm());
        db.Write(() =>
        {
            var userdb = db.All<UserModel>();
            if (userdb.Any())
            {
                var usr = userdb.FirstOrDefault()!;
                usr.UserIDOnline = CurrentUserOnline.ObjectId;
                db.Add(usr, update: true);
            }
        });

    }


    public void AddPDaCStateLink(PlayDateAndCompletionStateSongLink model)
    {

        try
        {
            AddOrUpdateSingleRealmItem(db, model, link => link.LocalDeviceId == model.LocalDeviceId);
            model.LocalDeviceId ??= MusicFileProcessor.GenerateLocalDeviceID("PDL");
            db = Realm.GetInstance(DataBaseService.GetRealm());
            db.Write(() =>
            {
                db.Add(model);
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);

        }

    }

}


public class CsvExporter
{
    /// <summary>
    /// Exports a list of SongModel to a CSV file with specified columns.
    /// </summary>
    /// <param name="songs">List of SongModel instances.</param>
    /// <param name="csvFilePath">Path to the output CSV file.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    public void ExportSongsToCsv(List<SongModel> songs, string csvFilePath)
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

                // Only proceed if there are any action entries
                if (actionEntries.Count != 0)
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
                                EscapeCsvField(song.ArtistName is null ? string.Empty : song.ArtistName),
                                EscapeCsvField(song.AlbumName is null ? string.Empty : song.AlbumName),
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

        Debug.WriteLine($"Data successfully exported to {csvFilePath}");
    }

    /// <summary>
    /// Escapes a CSV field by enclosing it in quotes if it contains special characters.
    /// Doubles any existing quotes within the field.
    /// </summary>
    /// <param name="field">The CSV field to escape.</param>
    /// <returns>The escaped CSV field.</returns>
    private string EscapeCsvField(string field)
    {
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
        {
            // Escape quotes by doubling them
            string escaped = field.Replace("\"", "\"\"");
            return $"\"{escaped}\"";
        }
        return field;
    }
}