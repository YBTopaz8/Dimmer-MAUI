

namespace Dimmer_MAUI.DataAccess.Services;

public partial class SongsManagementService : ISongsManagementService, IDisposable
{
    Realm db;

    public IList<SongModelView> AllSongs { get; set; }
    
    public IList<AlbumArtistGenreSongLinkView> AllLinks { get; set; }    
    public IList<AlbumModelView> AllAlbums { get; set; }
    public IList<ArtistModelView> AllArtists { get; set; }
    public IList<GenreModelView> AllGenres { get; set; }
    public IDataBaseService DataBaseService { get; }
    public SongsManagementService(IDataBaseService dataBaseService)
    {
        DataBaseService = dataBaseService;
        InitApp();
    }
    bool HasOnlineSyncOn;
    ParseUser? CurrentUserOnline;
    private async Task InitApp()
    {
        //GetSongs();
        //isSyncingOnline = true;
        //if (AllSongs?.Count<1)
        //{
        //    await FetchAllInitially();
        //}
        //else
        //{
        //    await GetOnlineDBLoaded();
        //}
        //isSyncingOnline = false;
        GetSongs();

        //var vm = IPlatformApplication.Current!.Services.GetService<HomePageVM>();
        //vm!.SyncRefresh();

    }
    async Task FetchAllInitially()
    {
        GetUserAccount();
        _ = await GetUserAccountOnline();
        await GetAllDataFromOnlineAsync();
    }
    async Task GetAllDataFromOnlineAsync()
    {
        if (CurrentUserOnline is null)
            return;
        try
        {
            if (CurrentUserOnline!.IsAuthenticated)
            {
                HasOnlineSyncOn = true;
                await LoadSongsToDBFromOnline();
                await LoadArtistsToDBFromOnline();
                await LoadGenreToDBFromOnline();
                await LoadAlbumToDBFromOnline();
                await LoadPlaylistToDBFromOnline();
                await LoadAAGSLinkViewToDBFromOnline();
                await LoadPlayDateAndIsPlayCompletedModelToDBFromOnline();
                GetSongs();
                GetAlbums();

            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }
    private async Task LoadSongsToDBFromOnline()
    {
        var query = AppParseClient.GetQuery("SongModelView")
            .WhereEqualTo("UserIDOnline", CurrentUser.LocalDeviceId)
            .WhereEqualTo("DeviceID", DeviceInfo.Platform.ToString())
            .WhereEqualTo("DeviceName",DeviceInfo.Name);

        var songs = await query.FindAsync();

        if (songs != null && songs?.Count() > 0)
        {
            // Get the realm database instance.
            db = Realm.GetInstance(DataBaseService.GetRealm());
            db.Write(() =>
            {
                foreach (var item in songs)
                {
                    try
                    {
                        var itemmm = MapFromDBParseObject<SongModelView>((ParseObject)item); //duration is off
                        SongModel itemm = new(itemmm);
                        var existingSongs = db.All<SongModel>()
                                                .Where(s => s.Title == itemm.Title && s.ArtistName == itemm.ArtistName)
                                                .ToList();
                        if (existingSongs.Count < 1)
                        {
                            db.Add(itemm);
                        }
                        else
                        {
                            db.Add(itemm, update: true);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error processing artist: {ex.Message}");
                    }
                }
            });
        }
    }    
    private async Task LoadPlayDateAndIsPlayCompletedModelToDBFromOnline()
    {
        var query = AppParseClient.GetQuery("PlayDateAndCompletionStateSongLink")
            
            .WhereEqualTo("UserIDOnline", CurrentUser.LocalDeviceId)
            .WhereEqualTo("DeviceID", DeviceInfo.Platform.ToString())
            .WhereEqualTo("DeviceName",DeviceInfo.Name);

        var PlayDateDIComp = await query.FindAsync();

        if (PlayDateDIComp != null && PlayDateDIComp?.Count() > 0)
        {
            // Get the realm database instance.
            db = Realm.GetInstance(DataBaseService.GetRealm());
            db.Write(() =>
            {
                foreach (var item in PlayDateDIComp)
                {
                    try
                    {
                        var itemm = MapFromDBParseObject<PlayDateAndCompletionStateSongLink>(item); //duration is off
                        
                        var existingSongs = db.All<PlayDateAndCompletionStateSongLink>()
                                                .ToList();
                        if (existingSongs.Count < 1)
                        {
                            db.Add(itemm);
                        }
                        else
                        {
                            db.Add(itemm, update: true);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error processing artist: {ex.Message}");
                    }
                }
            });
        }
    }
    private async Task LoadArtistsToDBFromOnline()
    {
        var query = AppParseClient.GetQuery("ArtistModelView")
            .WhereEqualTo("UserIDOnline", CurrentUser.LocalDeviceId)
            .WhereEqualTo("DeviceID", DeviceInfo.Platform.ToString())
            .WhereEqualTo("DeviceName", DeviceInfo.Name);

        // Uncomment the below line if filtering by UserIDOnline is needed
        // .WhereEqualTo("UserIDOnline", CurrentUser.LocalDeviceId);
        var artistModels = await query.FindAsync();

        if (artistModels != null && artistModels.Any())
        {
            // Get the realm database instance.
            db = Realm.GetInstance(DataBaseService.GetRealm());
            db.Write(() =>
            {
                foreach (var item in artistModels)
                {
                    try
                    {
                        var itemmm = MapFromDBParseObject<ArtistModelView>((ParseObject)item);
                        ArtistModel itemm = new(itemmm);
                        var existingArtist = db.All<ArtistModel>()
                                                .Where(s => s.Name == itemm.Name || s.LocalDeviceId == itemm.LocalDeviceId)
                                                .ToList();

                        if (existingArtist == null)
                        {
                            db.Add(itemm);
                            Debug.WriteLine("Added Artist");
                            return;
                        }

                        if (existingArtist?.Count < 1)
                        {
                            db.Add(itemm);
                        }
                        else
                        {
                            db.Add(itemm, update: true);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error processing artist: {ex.Message}");
                    }
                }
            });
        }
    }
    private async Task LoadPlaylistToDBFromOnline()
    {
        var query = AppParseClient.GetQuery("PlaylistModel")
                            .WhereEqualTo("UserIDOnline", CurrentUser.LocalDeviceId)
            .WhereEqualTo("DeviceID", DeviceInfo.Platform.ToString())
            .WhereEqualTo("DeviceName", DeviceInfo.Name);
        var playlists = await query.FindAsync();

        if (playlists != null || playlists?.Count() > 0)
        {
            // Get the realm database instance.
            db = Realm.GetInstance(DataBaseService.GetRealm());
            db.Write(() =>
            {
                foreach (var item in playlists)
                {
                    try
                    {
                        var itemmm = MapFromDBParseObject<PlaylistModelView>((ParseObject)item);
                        PlaylistModel itemm = new PlaylistModel(itemmm);
                        var existingPlaylist = db.All<PlaylistModel>()
                                                .Where(s => s.Name == itemm.Name)
                                                .ToList();
                        if (existingPlaylist == null)
                        {
                            db.Add(itemm);
                            Debug.WriteLine("Added Artist");
                            return;
                        }

                        if (existingPlaylist?.Count < 1)
                        {
                            db.Add(itemm);
                        }
                        else
                        {
                            db.Add(itemm, update: true);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error processing artist: {ex.Message}");
                    }
                }
            });
        }
    }
    private async Task LoadGenreToDBFromOnline()
    {
        var query = AppParseClient.GetQuery("GenreModelView").WhereEqualTo("UserIDOnline", CurrentUser.LocalDeviceId)
            .WhereEqualTo("DeviceID", DeviceInfo.Platform.ToString())
            .WhereEqualTo("DeviceName", DeviceInfo.Name);
        var GenreModels = await query.FindAsync();

        if (GenreModels != null && GenreModels?.Count() > 0)
        {
            // Get the realm database instance.
            db = Realm.GetInstance(DataBaseService.GetRealm());
            db.Write(() =>
            {
                foreach (var item in GenreModels)
                {
                    try
                    {
                        var itemmm = MapFromDBParseObject<GenreModelView>((ParseObject)item);
                        GenreModel itemm = new(itemmm);
                        var existingGenreModel = db.All<GenreModel>()
                                                .Where(s => s.Name == itemm.Name || s.LocalDeviceId== itemm.LocalDeviceId)
                                                .ToList();

                        if (existingGenreModel == null)
                        {
                            db.Add(itemm);
                            Debug.WriteLine("Added GenreModel");
                            return;
                        }

                        if (existingGenreModel?.Count < 1)
                        {
                            db.Add(itemm);
                        }
                        else
                        {
                            db.Add(itemm, update: true);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error processing artist: {ex.Message}");
                    }
                }
            });
        }
    }
    private async Task LoadAlbumToDBFromOnline()
    {
        var query = AppParseClient.GetQuery("AlbumModelView").WhereEqualTo("UserIDOnline", CurrentUser.LocalDeviceId)
            .WhereEqualTo("DeviceID", DeviceInfo.Platform.ToString())
            .WhereEqualTo("DeviceName", DeviceInfo.Name);
        var AlbumModels = await query.FindAsync();

        if (AlbumModels != null && AlbumModels?.Count() > 0)
        {
            // Get the realm database instance.
            db = Realm.GetInstance(DataBaseService.GetRealm());
            db.Write(() =>
            {
                foreach (var item in AlbumModels)
                {
                    try
                    {
                        var itemm = MapFromDBParseObject<AlbumModel>((ParseObject)item);

                        var existingAlbumModel = db.All<AlbumModel>()
                                                .Where(s => s.Name == itemm.Name || s.LocalDeviceId == itemm.LocalDeviceId)
                                                .ToList();

                        if (existingAlbumModel == null)
                        {
                            db.Add(itemm);
                            Debug.WriteLine("Added AlbumModel");
                            return;
                        }

                        if (existingAlbumModel?.Count < 1)
                        {
                            db.Add(itemm);
                        }
                        else
                        {
                            db.Add(itemm, update: true);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error processing artist: {ex.Message}");
                    }
                }
            });
        }


    }
    private async Task LoadAAGSLinkViewToDBFromOnline()
    {
        var query = AppParseClient.GetQuery("AlbumArtistGenreSongLink")
                            .WhereEqualTo("UserIDOnline", CurrentUser.LocalDeviceId)
            .WhereEqualTo("DeviceID", DeviceInfo.Platform.ToString())
            .WhereEqualTo("DeviceName", DeviceInfo.Name);
        var AlbumArtistGenreSongLink = await query.FindAsync();

        if (AlbumArtistGenreSongLink != null || AlbumArtistGenreSongLink?.Count() > 0)
        {
            // Get the realm database instance.
            db = Realm.GetInstance(DataBaseService.GetRealm());
            db.Write(() =>
            {
                foreach (var item in AlbumArtistGenreSongLink)
                {
                    try
                    {
                        var itemmm = MapFromDBParseObject<AlbumArtistGenreSongLinkView>((ParseObject)item);
                        AlbumArtistGenreSongLink itemm = new(itemmm);
                        var AAGSLink = db.All<AlbumArtistGenreSongLink>()
                                                .Where(s => s.LocalDeviceId == itemm.LocalDeviceId)
                                                .ToList();
                        if (AAGSLink == null)
                        {
                            db.Add(itemm);
                            Debug.WriteLine("Added AAGSL");
                            return;
                        }

                        if (AAGSLink?.Count < 1)
                        {
                            db.Add(itemm);
                        }
                        else
                        {
                            db.Add(itemm, update: true);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error processing artist: {ex.Message}");
                    }
                }
            });
        }
    }        

    async Task<bool> GetOnlineDBLoaded()
    {
        
        GetUserAccount();
        _ = await GetUserAccountOnline();
        //await GetDataFromOnlineAsync();
        return true;

    }

    bool isSyncingOnline;
    public void GetSongs()
    {
        try
        {
            db = Realm.GetInstance(DataBaseService.GetRealm());
            AllSongs?.Clear();
            var realmSongs = db.All<SongModel>().OrderBy(x => x.Instance!.DateCreated).ToList();
            AllSongs = new List<SongModelView>(realmSongs.Select(song => new SongModelView(song)));
            AllSongs ??= Enumerable.Empty<SongModelView>().ToList();

            var realmLinks = db.All<AlbumArtistGenreSongLink>().ToList();
            AllLinks = new List<AlbumArtistGenreSongLinkView>(realmLinks.Select(link => new AlbumArtistGenreSongLinkView(link)));
            AllLinks ??= Enumerable.Empty<AlbumArtistGenreSongLinkView>().ToList();
            GetAlbums();
            GetArtists();
            GetGenres();
            if(!isSyncingOnline)
            {
            }

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
        AllGenres = new List<GenreModelView>(realmSongs.Select(genre => new GenreModelView(genre)));
    }
    public void GetArtists()
    {
        try
        {
            db = Realm.GetInstance(DataBaseService.GetRealm());
            var realmArtists = db.All<ArtistModel>().ToList();
            AllArtists = new List<ArtistModelView>(realmArtists.Select(artist => new ArtistModelView(artist)));
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
        AllAlbums = new List<AlbumModelView>(realmAlbums.Select(album => new AlbumModelView(album)));
        
    }

    private bool IsRealmSpecificType(Type type)
    {

        return type.IsSubclassOf(typeof(RealmObject)) || type.IsGenericType && type.GetGenericTypeDefinition() == typeof(RealmList<>) || type == typeof(DynamicObjectApi);
    }

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
                //var actionAdd = new ActionsPending (songsToAdd)
                //{
                //    ActionType = 3,
                //    TargetType = 0,
                //    DateRequested = DateTimeOffset.Now,
                //    IsRequestedByUser = true,
                //    IsBatch = true,                    
                //};
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
            db.Write(() =>
            {
                var existingSong = db.All<SongModel>().
                Where(x => x.Title == songsModelView.Title
                && x.DurationInSeconds == songsModelView.DurationInSeconds
                && x.ArtistName == songsModelView.ArtistName).ToList();
                SongModel song = new SongModel(songsModelView);

                song.Instance!.UserIDOnline = CurrentUser.UserIDOnline;

                var newAction = new ActionPending()
                {
                    Actionn=0,
                    ActionSong = song,
                    TargetType = 0,
                    ApplyToAllThisDeviceOnly = true,
                };


                if (existingSong == null && existingSong?.Count <1)
                {                    
                    db.Add(newAction);
                }


                // Handle song addition
                if (existingSong is null || existingSong.Count < 1)
                {
                    var newSong = new SongModel(songsModelView);

                    //if (newSong.DatesPlayedAndWasPlayCompleted.Last().WasPlayCompleted == true)
                    //{
                    //    var mainWindow = IPlatformApplication.Current!.Services.GetService<DimmerWindow>();
                    //    // TODO: Ask the user if they want to add the song.
                    //}
                    newSong.IsPlaying = false;
                    db.Add(newSong);


                    // Save to Parse
                    db.Add(newAction);
                    //link. = newSong.LocalDeviceId;

                    return;
                }

                // Handle song deletion
                if (songsModelView.IsDeleted)
                {
                    db.Remove(existingSong.First());

                    db.Add(newAction);
                    return;
                }

                SongModel updatedSong = new(songsModelView);
                updatedSong.LocalDeviceId = existingSong.First().LocalDeviceId;
                updatedSong.IsPlaying = false;


                db.Add(updatedSong, update: true);
               
                db.Add(newAction);

            });

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Error when updating song: " + ex.Message);
            return false;
        }
    }
   
    public bool UpdatePlayAndCompletionDetails(PlayDateAndCompletionStateSongLink link)
    {
        try
        {
            db = Realm.GetInstance(DataBaseService.GetRealm());

            db.Write(() =>
            {

                var newAction = new ActionPending()
                {
                    Actionn = 0,
                    ApplyToAllThisDeviceOnly = true,
                    TargetType = 8,
                };
                db.Add(link);
                // Save to Parse
                db.Add(newAction);
                return;
            });

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Error when updating song: " + ex.Message);
            return false;
        }
    }

    private ParseObject MapToParseObject<T>(T model, string className)
    {
        var parseObject = new ParseObject(className);

        // Get the properties of the class
        var properties = typeof(T).GetProperties();

        foreach (var property in properties)
        {
            try
            {
                var value = property.GetValue(model);

                // Skip null values or Realm-specific/unsupported types
                if (value == null || IsRealmSpecificType(property.PropertyType))
                {
                    continue;
                }

                // Handle special types like DateTimeOffset
                if (property.PropertyType == typeof(DateTimeOffset))
                {
                    var val = (DateTimeOffset)value;
                    parseObject[property.Name] = val.Date;
                    continue;
                }

                // Handle string as string (required for Parse compatibility)
                if (property.PropertyType == typeof(string))
                {
                    parseObject[property.Name] = value.ToString();
                    continue;
                }

                // Add a fallback check for unsupported complex types
                if (value.GetType().Namespace?.StartsWith("Realms") == true)
                {
                    Debug.WriteLine($"Skipped unsupported Realm type: {property.Name}");
                    continue;
                }

                // For other types, directly set the value
                parseObject[property.Name] = value;
            }
            catch (Exception ex)
            {
                // Log the exception for this particular property, but continue with the next one
                Debug.WriteLine($"Error when mapping property '{property.Name}': {ex.Message}");
            }
        }

        return parseObject;
    }

    private T MapFromDBParseObject<T>(ParseObject parseObject) where T : new()
    {
        var model = new T();
        var properties = typeof(T).GetProperties();

        foreach (var property in properties)
        {
            try
            {
                // Skip Realm-specific properties
                if (IsRealmSpecificType(property.PropertyType))
                {
                    continue;
                }

                // Check if the ParseObject contains the property name
                if (parseObject.ContainsKey(property.Name))
                {
                    var value = parseObject[property.Name];

                    if (value != null)
                    {
                        // Handle special types like DateTimeOffset
                        if (property.PropertyType == typeof(DateTimeOffset) && value is DateTime dateTime)
                        {
                            property.SetValue(model, new DateTimeOffset(dateTime));
                            continue;
                        }

                        // Handle string as string
                        if (property.PropertyType == typeof(string) && value is string objectIdStr)
                        {
                            property.SetValue(model, new string(objectIdStr));
                            continue;
                        }

                        // For other types, directly set the value if the property has a setter
                        if (property.CanWrite && property.PropertyType.IsAssignableFrom(value.GetType()))
                        {
                            property.SetValue(model, value);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log and skip the property
                Debug.WriteLine($"Error mapping property '{property.Name}': {ex.Message}");
            }
        }

        return model;
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

                //var actionAdd = new ActionsPending(artists:artists)
                //{
                //    ActionType = 3,
                //    TargetType = 1,
                //    DateRequested = DateTimeOffset.UtcNow.Date.Date,
                //    IsRequestedByUser = true,
                //    IsBatch = true,
                    
                //};
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
                }
                else
                {
                    var newSong = new AlbumModel(album);
                    db.Add(newSong, update: true);
                }
                //var actionUpdate = new ActionsPending
                //{
                //    ActionType = 1,
                //    TargetType = 2,
                    
                //    ActionAlbum = new AlbumModel(album),
                //    DateRequested = DateTimeOffset.UtcNow.Date.Date,
                //    IsRequestedByUser = true,
                //    IsBatch = false,

                //};
            });

        }
        catch (Exception ex)
        {
            Debug.WriteLine("Error when updating song: " + ex.Message);
            
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
    public UserModelView CurrentUser { get; set; }
    public UserModelView? GetUserAccount()
    {

        db = Realm.GetInstance(DataBaseService.GetRealm());
        var dbUser = db.All<UserModel>().ToList().FirstOrDefault();

        if (dbUser == null)
        {
            CurrentUser = new UserModelView() { UserName = "User",
                UserEmail = "8brunel@gmail.com",
                UserPassword = "1234",
            };
            return CurrentUser;
        }
        CurrentUser = new(dbUser);
        return CurrentUser;
    }

    ParseClient? AppParseClient;

    public async Task<UserModelView?> GetUserAccountOnline()
    {
        try
        {
            AppParseClient = new ParseClient(new ServerConnectionData
            {
                ApplicationID = APIKeys.ApplicationId,
                ServerURI = APIKeys.ServerUri,
                Key = APIKeys.ClientKey,
                MasterKey = APIKeys.MasterKey,               
            });

            AppParseClient.Publicize();
            if (CurrentUser is null)
            {
                CurrentUser = new UserModelView()
                {
                    UserName = "User",
                    UserEmail = "8brunel@gmail.com",
                    UserPassword = "1234",
                };
            }
            if(string.IsNullOrEmpty(CurrentUser.UserName) || string.IsNullOrEmpty(CurrentUser.UserPassword))
            {
                CurrentUser.UserName = "User";
                CurrentUser.UserEmail = "8brunel@gmail.com";
                CurrentUser.UserPassword = "1234";
            }
            await AppParseClient.LogInAsync(CurrentUser.UserName, CurrentUser.UserPassword);
            CurrentUserOnline= AppParseClient.GetCurrentUser();
            
            if (CurrentUserOnline.IsAuthenticated)
            {                
                return null;
            }
            Debug.WriteLine(AppParseClient.GetCurrentUser().Username);
            db = Realm.GetInstance(DataBaseService.GetRealm());
            db.Write(() =>
            {
                UserModel? user = new();
                user.UserEmail = CurrentUserOnline.Email;
                user.UserName = CurrentUserOnline.Username;
                user.UserPassword = CurrentUserOnline.Password;
                

                var userdb = db.All<UserModel>();
                if (userdb.Any())
                {
                    var usr = userdb.FirstOrDefault()!;
                    usr.UserEmail = user.UserEmail;
                    usr.UserName = user.UserName;
                    usr.UserPassword = user.UserPassword;
                    
                    db.Add(usr, update: true);
                }

            });
            return null;
         
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Error getting user account online: " + ex.Message);
            await Shell.Current.DisplayAlert("Hey!", ex.Message,"Ok");
            return null;
        }
    }

    public async Task SyncSongsAsync()
    {
        try
        {

            GetUserAccount();
            _ = await GetUserAccountOnline();

            var lastSyncTime = GetLastSyncTime(nameof(SongModelView));

        }
        catch (Exception ex)
        {

            throw;
        }
    }


    private DateTimeOffset GetLastSyncTime(string model)
    {
        var key = $"LastSyncTime_{model}";
        if (Preferences.ContainsKey(key))
        {
            var ticks = Preferences.Get(key, 0L);
            return new DateTimeOffset(ticks, TimeSpan.Zero);
        }
        return DateTimeOffset.MinValue;
    }

    private void SetLastSyncTime(string model, DateTimeOffset syncTime)
    {
        var songForExample = new SongModelView() { Title = "Soh Soh" };
        RealmActionHandler.HandleAction(new SongModel(songForExample), ActionType.Add);
    }


    public static class RealmActionHandler
    {
        public static void HandleAction<T>(T actionObject, ActionType actionType) where T : RealmObject
        {
            using (var realm = Realm.GetInstance())
            {
                realm.Write(() =>
                {
                    switch (actionType)
                    {
                        case ActionType.Add:
                            realm.Add(actionObject);
                            break;
                        case ActionType.Delete:
                            realm.Remove(actionObject);
                            break;
                        default:
                            throw new InvalidOperationException($"Unsupported action type: {actionType}");
                    }
                });
            }
        }
    }

    public enum ActionType
    {
        Add,
        Update,
        Delete
    }


    public async Task<bool> LoadSongsFromFolderAsync(List<string> folderPaths)
    {
        ViewModel = IPlatformApplication.Current!.Services.GetService<HomePageVM>();

        // Load songs from folders asynchronously without blocking the UI
        (var allArtists, var allAlbums, var allLinks, var songs, var allGenres) =
            await Task.Run(() => LoadSongsAsync(folderPaths));

        if (songs is null || allArtists is null || allAlbums is null || allGenres is null)
        {
            await Shell.Current.DisplayAlert("Error during Scan", "No Songs to Scan", "OK");
            return false;
        }

        songs = songs.DistinctBy(x => new { x.Title, x.DurationInSeconds, x.AlbumName, x.ArtistName }).ToList();
        allArtists = allArtists.DistinctBy(x => x.Name).ToList();
        allAlbums = allAlbums.DistinctBy(x => x.Name).ToList();
        allGenres = allGenres.DistinctBy(x => x.Name).ToList();
        allLinks = allLinks.DistinctBy(x => new { x.ArtistId, x.AlbumId, x.SongId, x.GenreId }).ToList();

        MainThread.BeginInvokeOnMainThread(() =>
        {
            var combinedList = new List<SongModelView>(AllSongs);
            combinedList.AddRange(songs!);
            var allSongss = combinedList.ToObservableCollection();
            AllSongs = allSongss;
        });

        List<SongModel> dbSongs = songs.Select(song => new SongModel(song)).ToList()!;
        if (!await AddSongToArtistWithArtistIDAndAlbumAndGenreAsync(allArtists, allAlbums, dbSongs, allGenres, allLinks))
        {
            await Shell.Current.DisplayAlert("Error", "Error Adding Songs to Database", "OK");
        }
        AppSettingsService.RepeatModePreference.RepeatState = 1;

        await Shell.Current.DisplayAlert("Scan Completed", "All Songs have been scanned", "OK");
        ViewModel?.SetPlayerState(MediaPlayerState.LoadingSongs);

        // Save to online database (fire-and-forget)
        _ = Task.Run(async () =>
        {
            try
            {
                await AddSongToArtistWithArtistIDAndAlbumAndGenreOnlineAsync(allArtists, allAlbums, songs, allGenres, allLinks);
            }
            catch (Exception ex)
            {
                // Log the error silently, avoid disturbing the app's flow
                Debug.WriteLine($"Error saving online: {ex.Message}");
            }
        });

        return true;
    }


    public async Task<bool> AddSongToArtistWithArtistIDAndAlbumAndGenreOnlineAsync(
     List<ArtistModelView> artistModels,
     List<AlbumModelView> albumModels,

     List<SongModelView> songs,
     List<GenreModelView> genreModels,
     List<AlbumArtistGenreSongLinkView> AAGSLink)
    {
        try
        {

            db = Realm.GetInstance(DataBaseService.GetRealm());

            var CurrentUser = db.All<UserModel>().FirstOrDefault();
            UserModelView curUsr = new UserModelView(CurrentUser);
            if (CurrentUser is not null)
            {
                curUsr = new UserModelView(CurrentUser);
            }
            curUsr.UserName = "User";
            curUsr.UserEmail = "8brunel@gmail.com";
            curUsr.UserPassword = "1234";
            CurrentUserOnline = await ParseClient.Instance.LogInAsync(curUsr.UserName, curUsr.UserPassword);
            if (CurrentUserOnline is null)
            {
                await ParseClient.Instance.SignUpAsync(curUsr.UserName, curUsr.UserPassword);
            }

            if (CurrentUserOnline is null)
            {
                return false;//to be reviewed
            }

            if (!CurrentUserOnline.IsAuthenticated)
            {

                return false;//to be reviewed
            }

            // Save actions to Parse for Songs online
            foreach (var item in songs)
            {
                try
                {

                    var parseObj = MapToParseObject(item, nameof(SongModelView));
                    await parseObj.SaveAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error saving song action: {ex.Message}");
                    ;  // Rethrow to propagate error upwards
                }
            }
            Debug.WriteLine("songsToOnline");

            // Save actions for Artists online
            foreach (ArtistModelView item in artistModels)
            {
                try
                {
                    var parseObj = MapToParseObject(item, nameof(ArtistModelView));
                    await parseObj.SaveAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error saving artist action: {ex.Message}");
                    ;  // Rethrow to propagate error upwards
                }
            }
            Debug.WriteLine("artistToOnline");

            // Save actions for Album online
            foreach (AlbumModelView item in albumModels)
            {
                try
                {
                    var parseObj = MapToParseObject(item, nameof(AlbumModelView));
                    await parseObj.SaveAsync();

                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error saving artist action: {ex.Message}");
                    ;  // Rethrow to propagate error upwards
                }
            }
            Debug.WriteLine("albumToOnline");

            // Save actions for Genre online
            foreach (GenreModelView item in genreModels)
            {
                try
                {

                    var parseObj = MapToParseObject(item, nameof(GenreModelView));
                    await parseObj.SaveAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error saving artist action: {ex.Message}");
                    ;  // Rethrow to propagate error upwards
                }
            }
            Debug.WriteLine("genreToOnline");

            // Save actions for AAGSLink online
            foreach (AlbumArtistGenreSongLinkView item in AAGSLink)
            {
                try
                {
                    var parseObj = MapToParseObject(item, nameof(AlbumArtistGenreSongLinkView));
                    await parseObj.SaveAsync();

                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error saving artist action: {ex.Message}");
                    ;  // Rethrow to propagate error upwards
                }
            }
            Debug.WriteLine("good");

            return true;
        }
        catch (Exception ex)
        {
            // Catch and log the top-level errors
            Debug.WriteLine($"Exception when adding data: {ex.Message}");
            return false;
        }
    }


    private Dictionary<string, ArtistModelView> artistDict = new Dictionary<string, ArtistModelView>();
    HomePageVM? ViewModel { get; set; }
    private (List<ArtistModelView>?, List<AlbumModelView>?, 
        List<AlbumArtistGenreSongLinkView>?, List<SongModelView>?, List<GenreModelView>?) 
        LoadSongsAsync(List<string> folderPaths)
    {
        var allFiles = GeneralStaticUtilities.GetAllFiles(folderPaths);
        Debug.WriteLine("Got All Files");
        if (allFiles.Count == 0)
        {
            return (null, null, null, null, null);
        }

        // Fetch existing data from services
        var existingArtists = AllArtists is null ? [] : AllArtists;

        var existingLinks = AllLinks is null ? [] : AllLinks;

        var existingAlbums = AllAlbums is null ? [] : AllAlbums;

        var existingGenres = AllGenres is null ? [] : AllGenres;
        var oldSongs = AllSongs ?? new List<SongModelView>();

        // Initialize collections and dictionaries
        var newArtists = new List<ArtistModelView>();
        var newAlbums = new List<AlbumModelView>();
        var newLinks = new List<AlbumArtistGenreSongLinkView>();
        var newGenres = new List<GenreModelView>();
        var allSongs = new List<SongModelView>();

        var artistDict = new Dictionary<string, ArtistModelView>(StringComparer.OrdinalIgnoreCase);
        var albumDict = new Dictionary<string, AlbumModelView>();
        var genreDict = new Dictionary<string, GenreModelView>();

        int processedFiles = 0;
        int totalFiles = allFiles.Count;

        foreach (var file in allFiles)
        {
            if (GeneralStaticUtilities.IsValidFile(file))
            {
                var songData = GeneralStaticUtilities.ProcessFile
                (file, existingAlbums.ToList(), albumDict, newAlbums, oldSongs.ToList(),
                    newArtists, artistDict, newLinks, existingLinks.ToList(), existingArtists.ToList(),
                    newGenres, genreDict, existingGenres.ToList());

                if (songData != null)
                {
                    allSongs.Add(songData);
                }
            }

            processedFiles++;
            if (processedFiles % 10 == 0) // Report progress every 10 files
            {
                int percentComplete = processedFiles * 100 / totalFiles;
                Debug.WriteLine($"{percentComplete}%");
                
            }
        }
        Debug.WriteLine("All Processed");
        ViewModel?.SetPlayerState(MediaPlayerState.LoadingSongs);

        return (newArtists, newAlbums, newLinks, allSongs.ToList(), newGenres); // Return genreLinks


    }
    int countOfScanningErrors = 0;

    public async Task<bool> AddSongToArtistWithArtistIDAndAlbumAndGenreAsync(
     List<ArtistModelView> artistModels,
     List<AlbumModelView> albumModels,
     //List<AlbumArtistGenreSongLinkView> albumArtistSongLink,
     List<SongModel> songs,
     List<GenreModelView> genreModels,
     List<AlbumArtistGenreSongLinkView> AAGSLink)
    {
        await GetUserAccountOnline();
        try
        {
            db = Realm.GetInstance(DataBaseService.GetRealm());
            var user = db.All<UserModel>().FirstOrDefault();
            if (user is null)
            {
                user = new();
                db.Write(() =>
                {
                    //user.LocalDeviceId = string.GenerateNewId();
                    db.Add(user);
                });
            }

            // Insert new songs to db
            try
            {
                db.Write(() =>
                {
                    foreach (var song in songs)
                    {
                        var existingSongs = db.All<SongModel>()
                                                .Where(s => s.Title == song.Title && s.ArtistName == song.ArtistName)
                                                .ToList();
                        if (existingSongs.Count < 1)
                        {
                            db.Add(song);
                        }
                        Debug.WriteLine("Added Song " + song.Title);
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error inserting songs: {ex.Message}");
                throw; // Rethrow to propagate error upwards
            }
            db = Realm.GetInstance(DataBaseService.GetRealm());
            Debug.WriteLine("songsToDB");

            db = Realm.GetInstance(DataBaseService.GetRealm());
            // Insert new artists
            try
            {
                db.Write(() =>
                {
                    foreach (var artistModel in artistModels)
                    {
                        var modell = new ArtistModel(artistModel);

                        var existingArtist = db.Find<ArtistModel>(modell.LocalDeviceId);
                        var existingSongs = db.All<ArtistModel>()
                                                .Where(s => s.Name == modell.Name)
                                                .ToList();

                        if (existingArtist == null)
                        {
                            db.Add(modell);
                            Debug.WriteLine("Added Artist");
                            return;
                        }

                        if (existingSongs?.Count < 1)
                        {
                            db.Add(modell);
                        }
                        else
                        {
                            db.Add(modell, update: true);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error inserting artists: {ex.Message}");
                throw; // Rethrow to propagate error upwards
            }
            Debug.WriteLine("artistToDB");

            db = Realm.GetInstance(DataBaseService.GetRealm());
            try
            {
                db.Write(() =>
                {
                    foreach (AlbumModelView item in albumModels)
                    {
                        var modell = new AlbumModel(item);

                        var existingAlbum = db.All<AlbumModel>()
                                                .Where(s => s.Name == modell.Name)
                                                .ToList();

                        if (existingAlbum == null)
                        {
                            db.Add(modell);
                            Debug.WriteLine("Added Album");
                            return;
                        }

                        if (existingAlbum?.Count < 1)
                        {
                            db.Add(modell);
                        }
                        else
                        {
                            db.Add(modell, update: true);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error inserting album: {ex.Message}");
                throw; // Rethrow to propagate error upwards
            }
            Debug.WriteLine("AlbumToOnline");


            db = Realm.GetInstance(DataBaseService.GetRealm());
            // Insert new genres
            try
            {
                db.Write(() =>
                {
                    foreach (var model in genreModels)
                    {
                        var modell = new GenreModel(model);
                        var existingGenre = db.All<GenreModel>()
                                                .Where(s => s.Name == modell.Name)
                                                .ToList();

                        if (existingGenre == null)
                        {
                            db.Add(modell);
                            Debug.WriteLine("Added Genre");
                            return;
                        }

                        if (existingGenre?.Count < 1)
                        {
                            db.Add(modell);
                            Debug.WriteLine("Added Genre");
                        }
                        else
                        {
                            db.Add(modell, update: true);

                            Debug.WriteLine("Updated Genre");
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error inserting genres: {ex.Message}");
                throw; // Rethrow to propagate error upwards
            }
            Debug.WriteLine("albumToOnline");



            db = Realm.GetInstance(DataBaseService.GetRealm());
            // Insert new albumArtistSongLink
            try
            {
                db.Write(() =>
                {
                    foreach (var item in AAGSLink)
                    {
                        var AlbumArtistGenreSongLink = db.Find<AlbumArtistGenreSongLink>(item.LocalDeviceId);

                        if (AlbumArtistGenreSongLink != null)
                        {
                            db.Add(AlbumArtistGenreSongLink);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error inserting genres: {ex.Message}");
                throw;
            }
            Debug.WriteLine("AAGSLinkToDB");


            Debug.WriteLine("good");

            return true;
        }
        catch (Exception ex)
        {
            // Catch and log the top-level errors
            Debug.WriteLine($"Exception when adding data: {ex.Message}");
            return false;
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
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
        {
            // Escape quotes by doubling them
            string escaped = field.Replace("\"", "\"\"");
            return $"\"{escaped}\"";
        }
        return field;
    }
}