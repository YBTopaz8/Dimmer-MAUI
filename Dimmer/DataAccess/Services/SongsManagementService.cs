namespace Dimmer_MAUI.DataAccess.Services;

public partial class SongsManagementService : ISongsManagementService, IDisposable
{
    Realm db;

    public IList<SongModelView> AllSongs { get; set; }
    Lazy<HomePageVM> ViewModel { get; set; }
    public IList<AlbumArtistGenreSongLinkView> AllLinks { get; set; }
    public IList<AlbumModelView> AllAlbums { get; set; }
    public IList<ArtistModelView> AllArtists { get; set; }
    public IList<GenreModelView> AllGenres { get; set; }
    public IDataBaseService DataBaseService { get; }
    public SongsManagementService(IDataBaseService dataBaseService, Lazy<HomePageVM> viewModel)
    {
        DataBaseService = dataBaseService;
        InitApp();
        ViewModel = viewModel;
    }
    bool HasOnlineSyncOn;
    ParseUser? CurrentUserOnline;
    private void InitApp()
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
        GetUserAccount();
        GetSongs();



    }

    public async Task<bool> LogUserOnlineAsync(string username, string password)
    {

    }
    public async Task<bool> LogUserOnlineAsync(string email, string password)
    {

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
            .WhereEqualTo("UserIDOnline", CurrentUser.LocalDeviceId);
        //TODO : Add DeviceID and DeviceModel to the query
        var AllItems = await query.FindAsync();
        var UniqueItems = AllItems.DistinctBy(x => x["DeviceFormFactor"]).ToList();
        if (UniqueItems != null && UniqueItems.Count != 0)
        {
            // Get the realm database instance.
            db = Realm.GetInstance(DataBaseService.GetRealm());
            db.Write(() =>
            {
                foreach (var item in UniqueItems)
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
            .WhereEqualTo("UserIDOnline", CurrentUser.LocalDeviceId);


        var AllItems = await query.FindAsync();
        var UniqueItems = AllItems.DistinctBy(x => x["DeviceFormFactor"]).ToList();
        if (UniqueItems != null && UniqueItems.Count != 0)
        {
            // Get the realm database instance.
            db = Realm.GetInstance(DataBaseService.GetRealm());
            db.Write(() =>
            {
                foreach (var item in UniqueItems)
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
            .WhereEqualTo("UserIDOnline", CurrentUser.LocalDeviceId);
        var AllItems = await query.FindAsync();
        var UniqueItems = AllItems.DistinctBy(x => x["DeviceFormFactor"]).ToList();
        if (UniqueItems != null && UniqueItems.Count != 0)
        {
            // Get the realm database instance.
            db = Realm.GetInstance(DataBaseService.GetRealm());
            db.Write(() =>
            {
                foreach (var item in UniqueItems)
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
                            .WhereEqualTo("UserIDOnline", CurrentUser.LocalDeviceId);
        var AllItems = await query.FindAsync();
        var UniqueItems = AllItems.DistinctBy(x => x["DeviceFormFactor"]).ToList();
        if (UniqueItems != null && UniqueItems.Count != 0)
        {
            // Get the realm database instance.
            db = Realm.GetInstance(DataBaseService.GetRealm());
            db.Write(() =>
            {
                foreach (var item in UniqueItems)
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
        var query = AppParseClient.GetQuery("GenreModelView")
            .WhereEqualTo("UserIDOnline", CurrentUser.LocalDeviceId);


        var AllItems = await query.FindAsync();
        var UniqueItems = AllItems.DistinctBy(x => x["DeviceFormFactor"]).ToList();
        if (UniqueItems != null && UniqueItems.Count != 0)
        {
            // Get the realm database instance.
            db = Realm.GetInstance(DataBaseService.GetRealm());
            db.Write(() =>
            {
                foreach (var item in UniqueItems)
                {
                    try
                    {
                        var itemmm = MapFromDBParseObject<GenreModelView>((ParseObject)item);
                        GenreModel itemm = new(itemmm);
                        var existingGenreModel = db.All<GenreModel>()
                                                .Where(s => s.Name == itemm.Name || s.LocalDeviceId == itemm.LocalDeviceId)
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
        var query = AppParseClient.GetQuery("AlbumModelView")
            .WhereEqualTo("UserIDOnline", CurrentUser.LocalDeviceId).WhereNotEqualTo("DeviceFormFactor", DeviceInfo.Current.Idiom.ToString());


        var UniqueItems = await query.FindAsync();

        if (UniqueItems != null && UniqueItems?.Count() > 0)
        {
            // Get the realm database instance.
            db = Realm.GetInstance(DataBaseService.GetRealm());
            db.Write(() =>
            {
                foreach (var item in UniqueItems)
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
                            .WhereNotEqualTo("DeviceFormFactor", DeviceInfo.Current.Idiom.ToString());

        var UniqueItems = await query.FindAsync();

        if (UniqueItems != null && UniqueItems.Any())
        {
            var AlbumArtistGenreSongLink = await query.FindAsync();

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


    bool isSyncingOnline;
    public void GetSongs()
    {
        try
        {
            db = Realm.GetInstance(DataBaseService.GetRealm());
            AllSongs?.Clear();
            var realmSongs = db.All<SongModel>().OrderBy(x => x.DateCreated).ToList();
            AllSongs = new List<SongModelView>(realmSongs.Select(song => new SongModelView(song)));
            AllSongs ??= Enumerable.Empty<SongModelView>().ToList();

            var realmLinks = db.All<AlbumArtistGenreSongLink>().ToList();
            AllLinks = new List<AlbumArtistGenreSongLinkView>(realmLinks.Select(link => new AlbumArtistGenreSongLinkView(link)));
            AllLinks ??= Enumerable.Empty<AlbumArtistGenreSongLinkView>().ToList();
            GetAlbums();
            GetArtists();
            GetGenres();
            if (!isSyncingOnline)
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
            db = Realm.GetInstance(DataBaseService.GetRealm());
            db.Write(() =>
            {
                var existingSong = db.All<SongModel>()
                .Where(x => x.LocalDeviceId == songsModelView.LocalDeviceId)
                .ToList();

                if (existingSong.Count == 0)
                {
                    // Fallback to the longer check if no matches by LocalDeviceID
                    existingSong = db.All<SongModel>()
                        .Where(x => x.Title == songsModelView.Title &&
                                    x.DurationInSeconds == songsModelView.DurationInSeconds &&
                                    x.ArtistName == songsModelView.ArtistName)
                        .ToList();
                }


                SongModel song = new(songsModelView);

                song.UserIDOnline = CurrentUser.UserIDOnline;

                var newAction = new ActionPending()
                {
                    Actionn = 0,
                    ActionSong = song,
                    TargetType = 0,
                    ApplyToAllThisDeviceOnly = true,
                };
                var existingAction = db.All<ActionPending>()
                    .Where(x => x.LocalDeviceId == songsModelView.LocalDeviceId)
                    .ToList();
                bool actionExists = existingAction.Count > 0;

                if (existingSong == null && existingSong?.Count < 1)
                {
                    if (!actionExists)
                    {
                        db.Add(newAction);
                    }
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

                    if (actionExists)
                    {
                        db.Add(newAction, true);
                    }
                    else
                    {
                        //db.Add(newAction);
                    }
                    //link. = newSong.LocalDeviceId;

                    return;
                }

                // Handle song deletion
                if (songsModelView.IsDeleted)
                {
                    db.Remove(existingSong.First());
                    if (!actionExists)
                    {
                        db.Add(newAction);
                    }
                    return;
                }

                SongModel updatedSong = new(songsModelView);
                updatedSong.LocalDeviceId = existingSong.First().LocalDeviceId;
                updatedSong.IsPlaying = false;


                db.Add(updatedSong, update: true);



            });

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Error when updating song: " + ex.Message);
            return false;
        }
    }

    public async void AddPlayAndCompletionLink(PlayDateAndCompletionStateSongLink link)
    {
        try
        {
            AddOrUpdateSingleRealmItem(db,
                link,
                l => db.Find<AlbumArtistGenreSongLink>(link.LocalDeviceId) != null);

            if (string.IsNullOrEmpty(CurrentUser.UserIDOnline))
            {
                return; //no online id exists, we won't even bother saving this as pending.
            }

            if (CurrentUserOnline is null)
            {
                return; //no account
            }
            
            if (!CurrentUserOnline.IsAuthenticated)
            {
                return; //no not authenticated lol
            }

            //copy code from chat to add as normal (not pending) then clear this comment and test

            await SendSingleObjectToParse(nameof(PlayDateAndCompletionStateSongLink),link);

            
            return;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Error when updating song: " + ex.Message);
            return;
        }
    }


    /* Will need these when I'll be doing some fine tuning/granular control on cross device working ;)
     * for now I'll just take methods here one by one lol
     * 
        
AddOrUpdateSingleRealmItem(
    db,
    song,
    s => db.All<SongModel>().Any(existing => existing.Title == song.Title && existing.ArtistName == song.ArtistName)
);

AddOrUpdateSingleRealmItem(
    db,
    artist,
    a => db.All<ArtistModel>().Any(existing => existing.Name == artist.Name)
);
    
AddOrUpdateSingleRealmItem(
    db,
    album,
    a => db.All<AlbumModel>().Any(existing => existing.Name == album.Name)
);
    AddOrUpdateSingleRealmItem(
    db,
    genre,
    g => db.All<GenreModel>().Any(existing => existing.Name == genre.Name)
);

AddOrUpdateSingleRealmItem(
    db,
    link,
    l => db.Find<AlbumArtistGenreSongLink>(link.LocalDeviceId) != null
);


*/
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
            if (string.IsNullOrEmpty(CurrentUser.UserName) || string.IsNullOrEmpty(CurrentUser.UserPassword))
            {
                CurrentUser.UserName = "User";
                CurrentUser.UserEmail = "8brunel@gmail.com";
                CurrentUser.UserPassword = "1234";
            }
            await AppParseClient.LogInAsync(CurrentUser.UserName, CurrentUser.UserPassword);
            CurrentUserOnline = AppParseClient.GetCurrentUser();

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
            await Shell.Current.DisplayAlert("Hey!", ex.Message, "Ok");
            return null;
        }
    }

    public async Task<bool> LoadSongsFromFolderAsync(List<string> folderPaths)
    {
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
            AllArtists = allArtists.ToList();
            AllAlbums = allAlbums.ToList();
            AllGenres = allGenres.ToList();
            AllLinks = allLinks.ToList();
            ViewModel.Value.SyncRefresh();
        });

        List<SongModel> dbSongs = songs.Select(song => new SongModel(song)).ToList()!;

        AppSettingsService.RepeatModePreference.RepeatState = 1; //0 for repeat OFF, 1 for repeat ALL, 2 for repeat ONE

        await Shell.Current.DisplayAlert("Scan Completed", "All Songs have been scanned", "OK");
        ViewModel.Value.SetPlayerState(MediaPlayerState.DoneScanningData);

        return true;

        if (!await AddSongToArtistWithArtistIDAndAlbumAndGenreAsync(allArtists, allAlbums, dbSongs, allGenres, allLinks))
        {
            await Shell.Current.DisplayAlert("Error", "Error Adding Songs to Database", "OK");
        }
        // Save to online database (fire-and-forget)
        _ = Task.Run(async () =>
        {
            try
            {
                await AddSongToArtistWithArtistIDAndAlbumAndGenreOnlineAsync(allArtists, allAlbums, songs, allGenres, allLinks, null);
            }
            catch (Exception ex)
            {
                // Log the error silently, avoid disturbing the app's flow
                Debug.WriteLine($"Error saving online: {ex.Message}");
            }
        });

        return true;
    }

    private Dictionary<string, ArtistModelView> artistDict = new Dictionary<string, ArtistModelView>();

    private
    (List<ArtistModelView>?, List<AlbumModelView>?, List<AlbumArtistGenreSongLinkView>?, List<SongModelView>?, List<GenreModelView>?)
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

            //processedFiles++;

            //{
            //    percentComplete = ((double)processedFiles / totalFiles);
            //var ss = ((double)processedFiles / totalFiles); // here i have 2/10
            //    progressLoading.Report(percentComplete);

        }
        Debug.WriteLine("All Processed on device");

        return (newArtists, newAlbums, newLinks, allSongs.ToList(), newGenres); // Return genreLinks


    }
    double percentComplete;

    /// <summary>
    /// Syncs the provided data to the Online database
    /// </summary>
    /// <param name="songs"></param>
    /// <param name="artistModels"></param>
    /// <param name="albumModels"></param>
    /// <param name="genreModels"></param>
    /// <param name="AAGSLink"></param>
    /// <returns></returns>
    public async Task<bool> SyncAllDataToOnlineAsync(
        IEnumerable<SongModelView> songs,
        IEnumerable<ArtistModelView> artistModels,
        IEnumerable<AlbumModelView> albumModels,
        IEnumerable<GenreModelView> genreModels,
        IEnumerable<AlbumArtistGenreSongLinkView> AAGSLink,
        IEnumerable<PlayDateAndCompletionStateSongLink>? PDaCSLink)
    {
        try
        {
            // Log in or Sign up the user
            var currentUser = db.All<UserModel>().FirstOrDefault();
            var curUsr = currentUser is not null
                ? new UserModelView(currentUser)
                : new UserModelView
                {
                    UserName = "User",
                    UserEmail = "8brunel@gmail.com",
                    UserPassword = "1234"
                };


            if (CurrentUserOnline == null || !CurrentUserOnline.IsAuthenticated)
            {
                Debug.WriteLine("User authentication failed."); //todo to be reviewed, we can aske the user to login
                return false;
            }

            // Sync each collection to Parse
            await SendMultipleObjectsToParse(songs, nameof(SongModelView));
            Debug.WriteLine("songsToOnline");
            await SendMultipleObjectsToParse(artistModels, nameof(ArtistModelView));
            Debug.WriteLine("artistToOnline");
            await SendMultipleObjectsToParse(albumModels, nameof(AlbumModelView));
            Debug.WriteLine("albumToOnline");

            await SendMultipleObjectsToParse(genreModels, nameof(GenreModelView));
            Debug.WriteLine("genreToOnline");
            await SendMultipleObjectsToParse(AAGSLink, nameof(AlbumArtistGenreSongLinkView));
            Debug.WriteLine("AAGSLinkToOnline");

            if (PDaCSLink is not null)
            {
                await SendMultipleObjectsToParse(PDaCSLink, nameof(PlayDateAndCompletionStateSongLink));
            }
            Debug.WriteLine("PDaCSLinkToOnline");

            Debug.WriteLine("good");

            Debug.WriteLine("All data synced successfully.");
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Exception during overall sync: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Maps Collection of ModelView objects to ParseObjects then Sends to Online DB
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="items"></param>
    /// <param name="modelName"></param>
    /// <returns></returns>
    public async Task<bool> SendMultipleObjectsToParse<T>(IEnumerable<T> items, string modelName)
    {
        try
        {
            foreach (var item in items)
            {
                try
                {
                    // Map and save each item to Parse
                    await SendSingleObjectToParse(modelName, item);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error saving {modelName} action: {ex.Message}");
                }
            }
            Debug.WriteLine($"{modelName}sToOnline");
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Exception during sync for {modelName}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Maps Single ModelView object to ParseObject then Sends to Online DB
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="modelName"></param>
    /// <param name="item"></param>
    /// <returns></returns>
    private async Task SendSingleObjectToParse<T>(string modelName, T? item)
    {
        var parseObj = MapToParseObject(item, modelName);
        await parseObj.SaveAsync();
    }

    public async Task<bool> AddSongToArtistWithArtistIDAndAlbumAndGenreOnlineAsync(
     List<ArtistModelView> artistModels,
     List<AlbumModelView> albumModels,
     List<SongModelView> songs,
     List<GenreModelView> genreModels,
     List<AlbumArtistGenreSongLinkView> AAGSLink,
     List<PlayDateAndCompletionStateSongLink>? PDaCSLink)
    {
        try
        {

            await SyncAllDataToOnlineAsync(songs, artistModels, albumModels, genreModels, AAGSLink, PDaCSLink);

            return true;
        }
        catch (Exception ex)
        {
            // Catch and log the top-level errors
            Debug.WriteLine($"Exception when adding data: {ex.Message}");
            return false;
        }
    }


    /// <summary>
    /// Syncs the provided data to the local Realm database
    /// </summary>
    /// <param name="artistModels"></param>
    /// <param name="albumModels"></param>
    /// <param name="songs"></param>
    /// <param name="genreModels"></param>
    /// <param name="AAGSLink"></param>
    /// <returns></returns>
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
            SyncDataToDatabaseAsync(db, songs, artistModels, albumModels, genreModels, AAGSLink);
            
            return true;
        }
        catch (Exception ex)
        {
            // Catch and log the top-level errors
            Debug.WriteLine($"Exception when adding data: {ex.Message}");
            return false;
        }
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
    /// <returns></returns>
    public bool SyncDataToDatabaseAsync(
    Realm db,
    IEnumerable<SongModel> songs,
    IEnumerable<ArtistModelView> artistModels,
    IEnumerable<AlbumModelView> albumModels,
    IEnumerable<GenreModelView> genreModels,
    IEnumerable<AlbumArtistGenreSongLinkView> AAGSLink)
    {
        try
        {
            // Ensure UserModel exists
            var user = db.All<UserModel>().FirstOrDefault();
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
                
                artistModels.Select(a => new ArtistModel(a)),
                artist => db.All<ArtistModel>().Any(a => a.Name == artist.Name)
            );

            // Sync Albums
            AddOrUpdateMultipleRealmItems(
                
                albumModels.Select(a => new AlbumModel(a)),
                album => db.All<AlbumModel>().Any(a => a.Name == album.Name)
            );

            // Sync Genres
            AddOrUpdateMultipleRealmItems(
                
                genreModels.Select(g => new GenreModel(g)),
                genre => db.All<GenreModel>().Any(g => g.Name == genre.Name)
            );

            // Sync AlbumArtistGenreSongLinks
            AddOrUpdateMultipleRealmItems(
                
                AAGSLink.Select(l => new AlbumArtistGenreSongLink(l)),
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
                    Debug.WriteLine($"Added {typeof(T).Name}");
                }
                else
                {
                    updateAction?.Invoke(item); // Perform additional updates if needed
                    db.Add(item, update: true); // Update existing item
                    Debug.WriteLine($"Updated {typeof(T).Name}");
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
    public void AddOrUpdateSingleRealmItem<T>(Realm db, T item, Func<T, bool> existsCondition, Action<T>? updateAction = null) where T : RealmObject
    {

        db = Realm.GetInstance(DataBaseService.GetRealm());
        db.Write(() =>
        {
            if (!db.All<T>().Any(existsCondition))
            {
                db.Add(item);
                Debug.WriteLine($"Added {typeof(T).Name}");
            }
            else
            {
                updateAction?.Invoke(item); // Perform additional updates if needed
                db.Add(item, update: true); // Update existing item
                Debug.WriteLine($"Updated {typeof(T).Name}");
            }
        });
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