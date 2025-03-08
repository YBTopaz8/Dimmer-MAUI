using Parse.Infrastructure;
namespace Dimmer_MAUI.DataAccess.Services;

public partial class SongsManagementService : ISongsManagementService, IDisposable
{
    Realm db;

    public List<SongModelView> AllSongs { get; set; }
    public List<PlayDataLink> AllPlayDataLinks { get; set; }
    public List<PlaylistSongLink> AllPLSongLinks { get; set; }
    HomePageVM ViewModel { get; set; }

    public List<AlbumModelView> AllAlbums { get; set; }
    public List<ArtistModelView> AllArtists { get; set; }
    
    public List<GenreModelView> AllGenres { get; set; }
    public List<AlbumArtistGenreSongLinkView> AllLinks { get; set; } = new();
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
        ViewModel = vm;
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
            AllLinks = new List<AlbumArtistGenreSongLinkView>(realmLinks.Select(link => new AlbumArtistGenreSongLinkView(link)));
            AllLinks ??= Enumerable.Empty<AlbumArtistGenreSongLinkView>().ToList();

            AllSongs = new();

            AllSongs.Clear();

            var realmSongs = db.All<SongModel>().OrderBy(x => x.DateCreated).ToList();
            AllSongs = new List<SongModelView>(realmSongs.Select(song => new SongModelView(song)).OrderBy(x => x.DateCreated));
            AllPlayDataLinks = Enumerable.Empty<PlayDataLink>().ToList();
            var realmPlayData = db.All<PlayDateAndCompletionStateSongLink>().ToList();

            LoadPlayData(realmPlayData);

            var groupedPlayData = AllPlayDataLinks
    .Where(link => link.SongId != null) // Filter out null SongIds
    .GroupBy(link => link.SongId!)  // Use the null-forgiving operator (!)
    .ToDictionary(group => group.Key, group => group.ToList());
            
            // --- 5. Create SongModelView with PlayData ---
            var tempSongViews = new List<SongModelView>(); //temp list
            foreach (var songModel in realmSongs)
            {
                if (songModel.LocalDeviceId is null)
                {
                    continue; // Skip if LocalDeviceId is null (shouldn't happen, but good practice)
                }

                SongModelView songView = new SongModelView(songModel); //create songview

                if (groupedPlayData.TryGetValue(songModel.LocalDeviceId, out var playDataForSong))
                {
                    // Associate PlayDataLink list with the SongModelView
                    songView.PlayData = playDataForSong;

                    // Calculate statistics (efficiently, using the grouped data)
                    songView.NumberOfTimesPlayed = playDataForSong.Count;
                    songView.NumberOfTimesPlayedCompletely = playDataForSong.Count(p => p.WasPlayCompleted);
                    // ... calculate other statistics (skipped, etc.) ...
                }
                //else: No PlayData, we do nothing, and the props are initialized with 0 by default.

                tempSongViews.Add(songView);

            }
            //set AllSongs here
            AllSongs = tempSongViews;

            GetAlbums();
            GetArtists();
            GetGenres();
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    private void LoadPlayData(List<PlayDateAndCompletionStateSongLink> realmPlayData)
    {
        AllPlayDataLinks = new List<PlayDataLink>(realmPlayData
        .Select(model =>
        {
            var link = new PlayDataLink()
            {
                LocalDeviceId = model.LocalDeviceId!,
                SongId = model.SongId,
                DateStarted = model.DatePlayed.LocalDateTime,
                WasPlayCompleted = model.WasPlayCompleted,
                PositionInSeconds = model.PositionInSeconds,
                PlayType = (int)model.PlayType, //cast
                EventDate = (model.EventDate ?? model.DateFinished).LocalDateTime
            };

            return link;
        })
        .ToList()); // The .ToList() is important here to materialize the results
    }
    public void GetGenres()
    {
        db = Realm.GetInstance(DataBaseService.GetRealm());
        AllGenres?.Clear();
        var realmSongs = db.All<GenreModel>().ToList();
        AllGenres = new List<GenreModelView>(realmSongs.Select(genre => new GenreModelView(genre)).OrderBy(x => x.Name));
    }
    public void GetArtists()
    {
        try
        {
            db = Realm.GetInstance(DataBaseService.GetRealm());
            var realmArtists = db.All<ArtistModel>().ToList();

            AllArtists = new List<ArtistModelView>(realmArtists.Select(artist => new ArtistModelView(artist)).OrderBy(x => x.Name));
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
        AllAlbums = new List<AlbumModelView>(realmAlbums.Select(album => new AlbumModelView(album)).OrderBy(x=>x.Name));
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
        realmSongs = db.All<SongModel>().ToList();
        realmAlbums = db.All<AlbumModel>().ToList();
        realGenres = db.All<GenreModel>().ToList();
        realmArtists = db.All<ArtistModel>().ToList();
        realmAAGSL = db.All<AlbumArtistGenreSongLink>().ToList();
        
    }

    public async Task SendAllDataToServerAsInitialSync()
    {
        
        GetUserAccount();
        GetSongs();

        if (!CurrentOfflineUser.IsAuthenticated)
        {

            try
            {
                _ = await GetUserAccountOnline();
            }
            catch (Exception ex)
            {
                // Handle GetUserAccountOnline exceptions
                Debug.WriteLine($"Error in GetUserAccountOnline: {ex.Message}");
            }
        }
        try
        {

            //GeneralStaticUtilities.RunFireAndForget(AddSongToArtistWithArtistIDAndAlbumAndGenreOnlineAsync(AllArtists, AllAlbums, AllSongs, AllGenres, AllLinks, AllPlayDataLinks), ex =>
            //{
            //    // Log or handle the exception as needed
            //    Debug.WriteLine($"Task error: {ex.Message}");
            //});
            
        }
        catch (Exception ex)
        {
            // Handle GetAllDataFromOnlineAsync exceptions
            Debug.WriteLine($"Error in GetAllDataFromOnlineAsync: {ex.Message}");
        }

    }

    public async Task GetAllDataFromOnlineAsync()
    {
        if (CurrentUserOnline is null)
            CurrentUserOnline= ViewModel.CurrentUserOnline!;

        if (CurrentUserOnline is null)
        {
            await Shell.Current.DisplayAlert("Error", "No user account found. Please log in.", "OK");
            return;
        }
        if (CurrentUserOnline.Password is null)
        {
            CurrentUserOnline.Password = CurrentOfflineUser.UserPassword;
        }
        try
        {
            if (CurrentUserOnline is null)
            {
                return;
            }
            if ( await CurrentUserOnline!.IsAuthenticatedAsync())
            {
                HasOnlineSyncOn = true;
                await LoadSongsToDBFromOnline();
                await LoadArtistsToDBFromOnline();
                await LoadGenreToDBFromOnline();
                await LoadAlbumToDBFromOnline();
                await LoadPlaylistToDBFromOnline();
                await LoadAAGSLinkViewToDBFromOnline();
                await LoadPDaPCModelToDBFromOnline();
                GetSongs();
                ViewModel.SyncRefresh();
                HasOnlineSyncOn = false;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }

    //make these return their cols and then call AddSongToArtistWithArtistIDAndAlbumAndGenreAsync() since it's already established
    private async Task LoadSongsToDBFromOnline()
    {
        var query = ParseClient.Instance.GetQuery("SongModelView")
            .WhereEqualTo("deviceName", CurrentOfflineUser.LocalDeviceId);

        var AllItems = await query.FindAsync();
        var UniqueItems = AllItems.DistinctBy(x => x["DeviceFormFactor"]).ToList();

        //var UniqueItems = AllItems.DistinctBy(x => x["DeviceFormFactor"]).ToList();
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
                        var duration = ((ParseObject)item)["DurationInSeconds"];
                        double dur = Convert.ToDouble(duration);
                        var itemmm = GeneralStaticUtilities.MapFromParseObjectToClassObject<SongModelView>((ParseObject)item); //duration is off

                        
                        //check if itemmm.Title != string.IsNullOrEmpty
                        if (string.IsNullOrEmpty(itemmm.Title))
                        {
                            continue;                            
                        }
                        itemmm.DurationInSeconds = dur;
                        SongModel itemm = new SongModel(itemmm);
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
    private async Task LoadPDaPCModelToDBFromOnline()
    {
        var query = ParseClient.Instance.GetQuery("PlayDateAndCompletionStateSongLink")
            .WhereEqualTo("deviceName", CurrentOfflineUser.LocalDeviceId);


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
                        var itemm = GeneralStaticUtilities.MapFromParseObjectToClassObject<PlayDateAndCompletionStateSongLink>(item); //duration is off

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
        var query = ParseClient.Instance.GetQuery("ArtistModelView")
            .WhereEqualTo("UserIDOnline", CurrentOfflineUser.LocalDeviceId);
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
                        var itemmm = GeneralStaticUtilities.MapFromParseObjectToClassObject<ArtistModelView>((ParseObject)item);
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
        var query = ParseClient.Instance.GetQuery("PlaylistModel")
                            .WhereEqualTo("UserIDOnline", CurrentOfflineUser.LocalDeviceId);
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
                        var itemmm = GeneralStaticUtilities.MapFromParseObjectToClassObject<PlaylistModelView>((ParseObject)item);
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
        var query = ParseClient.Instance.GetQuery("GenreModelView")
            .WhereEqualTo("UserIDOnline", CurrentOfflineUser.LocalDeviceId);


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
                        var itemmm = GeneralStaticUtilities.MapFromParseObjectToClassObject<GenreModelView>((ParseObject)item);
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
        var query = ParseClient.Instance.GetQuery("AlbumModelView")
            .WhereEqualTo("UserIDOnline", CurrentOfflineUser.LocalDeviceId).WhereNotEqualTo("DeviceFormFactor", DeviceInfo.Current.Idiom.ToString());


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
                        var itemm = GeneralStaticUtilities.MapFromParseObjectToClassObject<AlbumModel>((ParseObject)item);

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
        var query = ParseClient.Instance.GetQuery("AlbumArtistGenreSongLink")
                            .WhereEqualTo("UserIDOnline", CurrentOfflineUser.LocalDeviceId)
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
                        var itemmm = GeneralStaticUtilities.MapFromParseObjectToClassObject<AlbumArtistGenreSongLinkView>((ParseObject)item);
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
            Track file = new(song.FilePath);
            file.Title = song.Title;
            file.Artist = song.ArtistName;
            file.Album = song.AlbumName;
            file.Genre = song.GenreName;
            file.TrackNumber = song.TrackNumber;
            file.SaveTo(song.FilePath);
        }
        return true;

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
                    if (existingSong is not null && existingSong.Count > 0)
                    {
                        return;
                    }
                }


                SongModel song = new(songsModelView);

                song.UserIDOnline = CurrentUserOnline?.ObjectId;

                var ex = existingSong.FirstOrDefault();
                if (ex is not null)
                {
                    song.LocalDeviceId = ex.LocalDeviceId;
                }
                song.IsPlaying = false;
                

                db.Add(song, update: true);
            });

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Error when updating songss: " + ex.Message);
            return false;
        }
    }

    public async Task AddPlayAndCompletionLinkAsync(PlayDateAndCompletionStateSongLink link, bool SyncSave = false)
    {
        try
        {
            db = Realm.GetInstance(DataBaseService.GetRealm());
            var listofLinks = db.All<AlbumArtistGenreSongLink>();
            var anyThing = listofLinks
            .FirstOrDefault(x => x.LocalDeviceId != null);
                
            if (anyThing != null)
            {
                AddOrUpdateSingleRealmItem(db,
                    link);
            }
            if (!SyncSave)
            {
                return;
            }

            if (string.IsNullOrEmpty(CurrentOfflineUser.UserIDOnline))
            {
                return; //no online id exists, we won't even bother saving this as pending.
            }

            if (CurrentUserOnline is null)
            {
                return; //no account
            }

            if (!CurrentOfflineUser.IsAuthenticated)
            {
                return; //no not authenticated lol
            }

            //copy code from chat to add as normal (not pending) then clear this comment and test

            await SendSingleObjectToParse("PlayDataLink", link);


            
            return;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Error when updating songasa: " + ex.Message);
            return;
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
    public UserModelView? GetUserAccount(ParseUser? usr=null)
    {
        if (CurrentOfflineUser is not null && CurrentOfflineUser.IsAuthenticated && usr == null)
        {
            return CurrentOfflineUser;
        }
        db = Realm.GetInstance(DataBaseService.GetRealm());
        var dbUser = db.All<UserModel>().ToList();
        if (dbUser is null)
        {
            return null;
        }

        if (dbUser == null)
        {
            if (usr is not null)
            {
                CurrentOfflineUser = new UserModelView(usr);
                db.Write(() =>
                {
                    UserModel user = new(CurrentOfflineUser);
                    
                    db.Add(user,true);
                });
                return CurrentOfflineUser;
                
            };

            CurrentOfflineUser = new UserModelView()
            {
                UserName = string.Empty,
                UserEmail = string.Empty,
                UserPassword = string.Empty,
              
            };
            db = Realm.GetInstance(DataBaseService.GetRealm());
            db.Write(() =>
            {
                UserModel user = new(CurrentOfflineUser);
                db.Add(user);
            });
            return CurrentOfflineUser;
        }
        //CurrentOfflineUser = new(dbUser);
        return CurrentOfflineUser;
    }
       
    // THIS IS PRODUCTION READY
    //private bool InitializeParseClient(string ApplicationId, string ServerUri, string ClientKey, string MasterKey, bool PublicizedAfterInitializing)


    //ParseClient ParseClient.Instance;
    //testing only


    public static bool InitializeParseClient()
    {
        
        try
        {
            // Check for internet connection
            if (Connectivity.NetworkAccess != NetworkAccess.Internet)
            {
                Debug.WriteLine("No Internet Connection: Unable to initialize ParseClient.");
                return false;
            }

            // Validate API Keys
            if (string.IsNullOrEmpty(APIKeys.ApplicationId) ||
                string.IsNullOrEmpty(APIKeys.ServerUri) ||
                string.IsNullOrEmpty(APIKeys.DotNetKEY))
            {
                Debug.WriteLine("Invalid API Keys: Unable to initialize ParseClient.");
                return false;
            }

            // Create ParseClient
            ParseClient client = new ParseClient(new ServerConnectionData()
            {
                ApplicationID = APIKeys.ApplicationId,
                ServerURI = APIKeys.ServerUri,
                Key = APIKeys.DotNetKEY,
            }
            );

            HostManifestData manifest = new HostManifestData()
            {
                Version = "1.4.0",
                Identifier = "com.yvanbrunel.dimmer",
                Name = "Dimmer",
            };
            client.Publicize();

            Debug.WriteLine("ParseClient initialized successfully.");
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error initializing ParseClient: {ex.Message}");
            return false;
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

        AppSettingsService.RepeatModePreference.RepeatState = 1; //0 for repeat OFF, 1 for repeat ALL, 2 for repeat ONE
                
        await AddSongToArtistWithArtistIDAndAlbumAndGenreAsync(allArtists, allAlbums, songs, allGenres, allLinks, null);
        AppSettingsService.RepeatModePreference.RepeatState = 1; //0 for repeat OFF, 1 for repeat ALL, 2 for repeat ONE

        await Shell.Current.DisplayAlert("Scan Completed", "All Songs have been scanned", "OK");
        ViewModel.SetPlayerState(MediaPlayerState.DoneScanningData);



        if (CurrentUserOnline is null || await CurrentUserOnline.IsAuthenticatedAsync())
        {
                //await Shell.Current.DisplayAlert("Hey!", "Please login to save your songs", "OK");
                return false;           
        }


        if (CurrentUserOnline == null || !await CurrentUserOnline.IsAuthenticatedAsync())
        {
            Debug.WriteLine("User authentication failed."); //todo to be reviewed, we can aske the user to login
            return false;
        }
        //GeneralStaticUtilities.RunFireAndForget(AddSongToArtistWithArtistIDAndAlbumAndGenreOnlineAsync(allArtists, allAlbums, songs, allGenres, allLinks, null), ex => { Debug.WriteLine($"Task error: {ex.Message}"); });
       

        return true;
    }

    public async Task OpenConnectPopup()
    {
        ConnectOnline();

        CurrentUserOnline = await ParseClient.Instance.GetCurrentUser();
        if(CurrentUserOnline is null || ! await CurrentUserOnline.IsAuthenticatedAsync())
        {
            Debug.WriteLine("User authentication failed.");
            return;
        }
        CurrentOfflineUser.UserIDOnline = CurrentUserOnline.ObjectId;
        CurrentOfflineUser.IsAuthenticated = await CurrentUserOnline.IsAuthenticatedAsync();
        ;
        CurrentOfflineUser.UserName = CurrentUserOnline.Username;
        CurrentOfflineUser.UserEmail= CurrentUserOnline.Email;
        

        ViewModel.CurrentUser = CurrentOfflineUser;
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



    private Dictionary<string, ArtistModelView> artistDict = new Dictionary<string, ArtistModelView>();

    private
    (List<ArtistModel>?, List<AlbumModel>?, List<AlbumArtistGenreSongLink>?, List<SongModel>?, List<GenreModel>?)
    LoadSongsAsync(List<string> folderPaths)
    {
        var allFiles = GeneralStaticUtilities.GetAllFiles(folderPaths);
        Debug.WriteLine("Got All Files");
        if (allFiles.Count == 0)
        {
            return (null, null, null, null, null);
        }
        GetInitialValues();
        // Fetch existing data from services
        var existingArtists = realmArtists is null ? [] : realmArtists;

        var existingLinks = realmAAGSL is null ? [] : realmAAGSL;

        var existingAlbums = realmAlbums is null ? [] : realmAlbums;

        var existingGenres = realGenres is null ? [] : realGenres;
        var oldSongs = realmSongs is null ? [] : realmSongs;

        // Initialize collections and dictionaries
        var newArtists = new List<ArtistModel>();
        var newAlbums = new List<AlbumModel>();
        var newLinks = new List<AlbumArtistGenreSongLink>();
        var newGenres = new List<GenreModel>();
        var allSongs = new List<SongModel>();

        var artistDict = new Dictionary<string, ArtistModel>(StringComparer.OrdinalIgnoreCase);
        var albumDict = new Dictionary<string, AlbumModel>();
        var genreDict = new Dictionary<string, GenreModel>();

        int totalFiles = allFiles.Count;

        foreach (var file in allFiles)
        {
            if (GeneralStaticUtilities.IsValidFile(file))
            {
                var songData = GeneralStaticUtilities.ProcessFile(file, existingAlbums, albumDict, newAlbums, oldSongs,
                    newArtists, artistDict, newLinks, existingLinks, existingArtists,
                    newGenres, genreDict, existingGenres);

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

    public void AddPDaCStateLink(PlayDateAndCompletionStateSongLink model)
    {

        try
        {
            if (model.LocalDeviceId is null)
            {
                model.LocalDeviceId = GeneralStaticUtilities.GenerateLocalDeviceID("PDL");
            }
            db = Realm.GetInstance(DataBaseService.GetRealm());
            db.Write(() =>
            {                
                db.Add(model);
            });
        }
        catch(Exception ex)
        {
            Debug.WriteLine(ex.Message);
            
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

    public async Task<bool> SyncPlayDataAndCompletionData()
    {
        await SendMultipleObjectsToParse(AllPlayDataLinks, nameof(PlayDateAndCompletionStateSongLink));
        return true;
    }
    public async Task<bool> SyncAllDataToOnlineAsync( //TRY NOT TO USE THIS FIRST. USE ADDSONG.....OnlineAsyc
        IEnumerable<SongModelView> songs,
        IEnumerable<ArtistModelView> artistModels,
        IEnumerable<AlbumModelView> albumModels,
        IEnumerable<GenreModelView> genreModels,
        IEnumerable<AlbumArtistGenreSongLinkView> AAGSLink,
        IEnumerable<PlayDataLink>? PDaCSLink)
    {
        try
        {
            // Sync each collection to Parse
            await SendMultipleObjectsToParse(songs, nameof(SongModelView));
            
            //await SendMultipleObjectsToParse(artistModels, nameof(ArtistModelView));
            
            //await SendMultipleObjectsToParse(albumModels, nameof(AlbumModelView));
            

            //await SendMultipleObjectsToParse(genreModels, nameof(GenreModelView));
            
            //await SendMultipleObjectsToParse(AAGSLink, nameof(AlbumArtistGenreSongLinkView));
            

            //if (PDaCSLink is not null)
            //{
            //    await SendMultipleObjectsToParse(PDaCSLink, nameof(PlayDateAndCompletionStateSongLink));
            //}
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
            foreach (var item in items.Take(500))
            {
                
                var parseObj = GeneralStaticUtilities.MapToParseObject(item, modelName);
                // Map and save each item to Parse
                await SendSingleObjectToParse(modelName, item);
                
            }
            Debug.WriteLine($"{modelName}sToOnline saved!");
            await Shell.Current.DisplayAlert("Success!", "Synced Songs!","Ok");
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
        var parseObj = GeneralStaticUtilities.MapToParseObject(item, modelName);
        await parseObj.SaveAsync();
    }

    /// <summary>
    /// Syncs the provided data to the Online database 
    /// </summary>
    /// <param name="artistModels"></param>
    /// <param name="albumModels"></param>
    /// <param name="songs"></param>
    /// <param name="genreModels"></param>
    /// <param name="AAGSLink"></param>
    /// <param name="PDaCSLink"></param>
    /// <returns></returns>

    //public async Task<bool> AddSongToArtistWithArtistIDAndAlbumAndGenreOnlineAsync(
    // IEnumerable<ArtistModelView> artistModels,
    // IEnumerable<AlbumModelView> albumModels,
    // IEnumerable<SongModelView> songs,
    // IEnumerable<GenreModelView> genreModels,
    // IEnumerable<AlbumArtistGenreSongLinkView> AAGSLink,
    // IEnumerable<PlayDataLink>? PDaCSLink)
    //{
    //    try
    //    {
    //        await SyncAllDataToOnlineAsync(songs, artistModels, albumModels, genreModels, AAGSLink, PDaCSLink);
    //        return true;
    //    }
    //    catch (Exception ex)
    //    {
    //        // Catch and log the top-level errors
    //        Debug.WriteLine($"Exception when adding data: {ex.Message}");
    //        return false;
    //    }
    //}


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
     IEnumerable<ArtistModel> artistModels,
     IEnumerable<AlbumModel> albumModels,
     
     IEnumerable<SongModel> songs,
     IEnumerable<GenreModel> genreModels,
     IEnumerable<AlbumArtistGenreSongLink> AAGSLink,
     IEnumerable<PlayDataLink>? PDaCSLink)
    {
        await GetUserAccountOnline();
        try
        {
            SyncAllDataToDatabase(db, songs, artistModels, albumModels, genreModels, AAGSLink, null);

            GetSongs();

            ViewModel.SyncRefresh();

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
    public void AddOrUpdateSingleRealmItem<T>(Realm db, T item, Func<T, bool> existsCondition=null, Action<T>? updateAction = null) where T : RealmObject
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