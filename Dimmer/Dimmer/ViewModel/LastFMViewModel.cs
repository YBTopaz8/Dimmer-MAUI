using static Dimmer.Data.ModelView.LastFMUserView;

namespace Dimmer.ViewModel;

public partial class LastFMViewModel : ObservableObject
{
    public LastFMViewModel(
        ILastfmService _lastfmService,
        ILogger<LastFMViewModel> logger,
        IRealmFactory RealmFact)
    {
        this.lastfmService = _lastfmService ?? throw new ArgumentNullException(nameof(lastfmService));
        RealmFactory = RealmFact;

        _logger = logger ?? NullLogger<LastFMViewModel>.Instance;
     

        // Use a single, large write transaction for performance.
        lastfmService.IsAuthenticatedChanged
           .ObserveOn(RxSchedulers.UI)
           .Subscribe(
               async isAuthenticated =>
               {
                   if (!isAuthenticated)
                       return;
                   LastFMUserInfo = await lastfmService.GetUserInfoAsync();
                   if (LastFMUserInfo is null)
                       return;

                   IsLastfmAuthenticated = isAuthenticated;
                   LastFMName = lastfmService.AuthenticatedUser ?? "Not Logged In";
                   //if (isAuthenticated)
                   //{
                   //    lastFMCOmpleteLoginBtnVisible = false;
                   //    LastFMLoginBtnVisible = false;

                   //    var lastFMRealm = RealmFactory.GetRealmInstance();
                   //    var currentUser = lastFMRealm
                   //    .All<UserModel>().FirstOrDefaultNullSafe();
                   //    await lastFMRealm.WriteAsync(() =>
                   //    {
                   //        if ((!string.IsNullOrEmpty(lastfmService.AuthenticatedUser)) && currentUser is not null)
                   //        {
                   //            currentUser.UserName ??= !string.IsNullOrEmpty(lastfmService.AuthenticatedUser) ?
                   //   lastfmService.AuthenticatedUser : "NewUser_" + DateTimeOffset.UtcNow.ToString();
                   //            currentUser.LastFMAccountInfo ??= LastFMUserInfo.ToLastFMUser()!;

                   //            CurrentUserLocal.Username ??= lastfmService.AuthenticatedUser;
                   //        }
                   //    });
                   //}
               
               })
           .DisposeWith(CompositeDisposables);


    }
    [ObservableProperty]
    public partial bool ScrobbleOnSkip { get; set; } = true;

    [ObservableProperty]
    public partial bool ScrobbleOnCompletion { get; set; } = true;



    public BaseViewModel? _baseViewModel;

    public void LoadBaseViewModel(BaseViewModel baseVM)
    {
        try
        {
            if (_baseViewModel is null)
            {
                _baseViewModel = baseVM;

                lastfmService.Start();
                _baseViewModel.WhenPropertyChanged(
             nameof(_baseViewModel.IsBackGrounded),
             isBG => (_baseViewModel.IsBackGrounded))
             .ObserveOn(RxSchedulers.UI)
             .Subscribe(
                 async isBg =>
                 {
                     if (!isBg)
                     {
                         if (WindowActivationRequestTypeStatic == "Confirm LastFM")
                         {
                             await CompleteLastFMLoginAsync();
                         }
                     }

                 });
                _baseViewModel.WhenPropertyChanged(
             nameof(_baseViewModel.ScrobblePreviousSongToLastFM),
             isBG => (_baseViewModel.ScrobblePreviousSongToLastFM))
             .ObserveOn(RxSchedulers.UI)
             .Subscribe(
                 async isBg =>
                 {
                     if (isBg && IsLastfmAuthenticated && _baseViewModel.SongToScrobble is not null)
                     {

                         await lastfmService.ScrobbleAsync(_baseViewModel!.SongToScrobble);
                     }

                 });
                _baseViewModel.WhenPropertyChanged(
             nameof(_baseViewModel.ScrobbleNextSongToLastFM),
             isBG => (_baseViewModel.ScrobbleNextSongToLastFM))
             .ObserveOn(RxSchedulers.UI)
             .Subscribe(
                 async scrobbleNextSongToLastFM =>
                 {
                     if (scrobbleNextSongToLastFM && IsLastfmAuthenticated && _baseViewModel.SongToScrobble is not null)
                     {

                         await lastfmService.ScrobbleAsync(_baseViewModel!.SongToScrobble);
                     }

                 });

                _ = LoadLastFMSession();
                CurrentUserLocal = _baseViewModel.CurrentUserLocal;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            
        }
    }
    [ObservableProperty]
    public partial bool ScrobbleToLastFM { get; set; }
    partial void OnScrobbleToLastFMChanging(bool oldValue, bool newValue)
    {
        ToggleLastFMScrobbling(newValue);
    }
    [RelayCommand]
    public void ToggleLastFMScrobbling(bool isOn)
    {
        ScrobbleOnCompletion = isOn;
        var realmm = RealmFactory.GetRealmInstance();
        var appModel = realmm.All<AppStateModel>().FirstOrDefaultNullSafe();
        if (appModel != null)
        {
            realmm.Write(
                () =>
                {
                    appModel.ScrobbleToLastFM = isOn;
                });
            realmm.Add(appModel, true);
        }
    }

    protected CompositeDisposable CompositeDisposables { get; } = new CompositeDisposable();
    public IRealmFactory RealmFactory;

    [ObservableProperty]
    public partial UserModelView? CurrentUserLocal { get; set; }


    protected ILastfmService lastfmService;
    public ILastfmService LastFMService => lastfmService;
    [ObservableProperty]
    public partial ObservableCollection<Hqub.Lastfm.Entities.Track>? CollectionOfUserRecentTracks { get; set; }

    [ObservableProperty]
    public partial Hqub.Lastfm.Entities.Track? LastFMTrackOnEditPage { get; set; }

   


    [ObservableProperty]
    public partial ObservableCollection<Hqub.Lastfm.Entities.Track>? ListOfSimilarTracks { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<Hqub.Lastfm.Entities.Artist>? ListOfGetTopArtistsChart { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<Hqub.Lastfm.Entities.Track>? CollectionOfUserLovedTracks { get; set; }

    [ObservableProperty]
    public partial Hqub.Lastfm.Entities.User? LastFMUserInfo { get; set; }


    protected ILogger<LastFMViewModel> _logger;
    public  SongModelView? SelectedSong => _baseViewModel.SelectedSong;
  

    #region lastfm
    [RelayCommand]
    public async Task LoadUserLastFMInfo()
    {
        try
        {

            if (!lastfmService.IsAuthenticated)
            {
                return;
            }
            Hqub.Lastfm.Entities.User? usr = await lastfmService.GetUserInfoAsync();
            if (usr is null)
            {
                _logger.LogWarning("Failed to load Last.fm user info.");
                return;
            }
            if(CurrentUserLocal is not null)
            {

            CurrentUserLocal.LastFMAccountInfo.Name = usr.Name;
            CurrentUserLocal.LastFMAccountInfo.RealName = usr.RealName;
            CurrentUserLocal.LastFMAccountInfo.Url = usr.Url;
            CurrentUserLocal.LastFMAccountInfo.Country = usr.Country;
            CurrentUserLocal.LastFMAccountInfo.Age = usr.Age;
            CurrentUserLocal.LastFMAccountInfo.Playcount = usr.Playcount;
            CurrentUserLocal.LastFMAccountInfo.Playlists = usr.Playlists;
            CurrentUserLocal.LastFMAccountInfo.Registered = usr.Registered;
            CurrentUserLocal.LastFMAccountInfo.Gender = usr.Gender;
                CurrentUserLocal.LastFMAccountInfo.Image ??= new LastImageView()
                ;
                var lastUsrImgUrl = usr.Images.LastOrDefault()?.Url;
                if ( !string.IsNullOrEmpty(lastUsrImgUrl))
                {
                    CurrentUserLocal.LastFMAccountInfo.Image.Url = lastUsrImgUrl;
                }
             
                var lastUsrImgSize= usr.Images.LastOrDefault()?.Size;
                if ( !string.IsNullOrEmpty(lastUsrImgSize))
                {
                    CurrentUserLocal.LastFMAccountInfo.Image.Size = lastUsrImgSize;
                }
             
            

            var rlm = RealmFactory.GetRealmInstance();
            await rlm.WriteAsync(
                () =>
                {
                    var usre = rlm.All<UserModel>().FirstOrDefaultNullSafe();
                    if (usre is not null)
                    {
                        var usrr = usre;
                        if (usrr is not null)
                        {
                            usrr.LastFMAccountInfo = new()
                            {
                                Name = usr.Name,
                                RealName = usr.RealName,
                                Url = usr.Url,
                                Country = usr.Country,
                                Age = usr.Age,
                                Playcount = usr.Playcount,
                                Playlists = usr.Playlists,
                                Registered = usr.Registered,
                                Gender = usr.Gender,

                                Image = new LastFMUser.LastImage()
                            };
                            usrr.LastFMAccountInfo.Image.Url = usr?.Images?.LastOrDefault()?.Url;
                            usrr.LastFMAccountInfo.Image.Size = usr?.Images?.LastOrDefault()?.Size;
                            rlm.Add(usrr, update: true);
                        }
                        else
                        {
                            usrr = new UserModel();
                            usrr.LastFMAccountInfo = new();
                            usrr.Id = new();
                            usrr.UserName = usr.Name;
                            usrr.LastFMAccountInfo.Name = usr.Name;
                            usrr.LastFMAccountInfo.RealName = usr.RealName;
                            usrr.LastFMAccountInfo.Url = usr.Url;
                            usrr.LastFMAccountInfo.Country = usr.Country;
                            usrr.LastFMAccountInfo.Age = usr.Age;
                            usrr.LastFMAccountInfo.Playcount = usr.Playcount;
                            usrr.LastFMAccountInfo.Playlists = usr.Playlists;
                            usrr.LastFMAccountInfo.Registered = usr.Registered;
                            usrr.LastFMAccountInfo.Gender = usr.Gender;
                            usrr.LastFMAccountInfo.Image = new LastFMUser.LastImage();
                            usrr.LastFMAccountInfo.Image.Url = usr.Images.LastOrDefault()?.Url;
                            usrr.LastFMAccountInfo.Image.Size = usr.Images.LastOrDefault()?.Size;

                            rlm.Add(usrr, update: true);
                        }
                    }
                });

            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
        //await LoadUserLastFMDataAsync();
    }


    [ObservableProperty]
    public partial bool IsLastfmAuthenticated { get; set; }


    [ObservableProperty]
    public partial bool LastFMLoginBtnVisible { get; set; } = true;

    [ObservableProperty]
    public partial bool lastFMCOmpleteLoginBtnVisible { get; set; }




    [ObservableProperty]
    public partial bool IsLastFMNeedsUsername { get; set; }

    [ObservableProperty]
    public partial bool IsLastFMAuthButtonClickable { get; set; }



    [RelayCommand]
    public async Task LoadLastFMSession()
    {
        lastfmService.LoadSession();
        IsLastfmAuthenticated = lastfmService.IsAuthenticated;
        if (IsLastfmAuthenticated)
        {
            LastFMLoginBtnVisible = false;
            lastFMCOmpleteLoginBtnVisible = false;
            await LoadUserLastFMInfo();
        }
    }


    public static string WindowActivationRequestTypeStatic=string.Empty;

    [ObservableProperty]
    public partial string LastFMName { get; set; } = string.Empty;

    [RelayCommand]
    public async Task LogOut()
    {
        lastfmService.Logout();
    }
    [RelayCommand]
    public async Task LoginToLastfmAsync()
    {
        if (string.IsNullOrEmpty(LastFMName))
        {
            // alert user that Username is missing and required
            await Shell.Current.DisplayAlertAsync("UserName Required", "Please Enter Your User Name", "OK");
            IsLastFMNeedsUsername = true;
            return;
        }
        IsLastFMAuthButtonClickable = false;

        try
        {
            string? webUrl = await lastfmService.GetAuthenticationUrlAsync();


            if (string.IsNullOrEmpty(webUrl)) return;


            LastFMLoginBtnVisible = false;
            lastFMCOmpleteLoginBtnVisible = true;

            WindowActivationRequestTypeStatic = "Confirm LastFM";

            await Launcher.Default.OpenAsync(new Uri(webUrl));


        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Last.fm authentication URL.");
            IsLastFMAuthButtonClickable = true;
            return;
        }
    }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }
    [RelayCommand]
    public async Task CompleteLastFMLoginAsync()
    {
        IsBusy = true;
        try
        {

            string? lastFMUName = LastFMName;
            if (string.IsNullOrEmpty(lastFMUName)) return;

            IsLastfmAuthenticated = await lastfmService.CompleteAuthenticationAsync(lastFMUName);
            if (IsLastfmAuthenticated)
            {
                lastFMCOmpleteLoginBtnVisible = false;
                await LoadUserLastFMInfo();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while completing Last.fm login.");
        }
        finally
        {

            IsBusy = false;
        }
    }


    [ObservableProperty]
    public partial Hqub.Lastfm.Entities.Track? SelectedSongLastFMData { get; set; }

    [ObservableProperty]
    public partial Hqub.Lastfm.Entities.Track? CorrectedSelectedSongLastFMData { get; set; }
    public async Task LoadSelectedSongLastFMData()
    {
        if (SelectedSong is not null)
        {
            SelectedSongLastFMData = null;
            CorrectedSelectedSongLastFMData = null;

            await LoadSongLastFMDataAsync();
        }
    }
    public async Task LoadSongLastFMDataAsync()
    {
        if (SelectedSong is null || SelectedSong.ArtistName == "Unknown Artist")
        {
            return;
        }
        if (SelectedSong.ArtistName is null) return;

        var realm = RealmFactory.GetRealmInstance();
        var songInDb = realm.Find<SongModel>(SelectedSong.Id);
        if (songInDb is null) return;
        var artistsToSong = songInDb.ArtistToSong;
        string? artistName = SelectedSong.ArtistName;
        if (artistsToSong.Count == 0 || songInDb.ArtistName is null)
        {
            var track = new ATL.Track(songInDb.FilePath);

            string tagTitle = track.Title;
            string tagArtist = track.Artist;
            string tagAlbumArtist = track.AlbumArtist;
            string tagAlbum = track.Album;
            string tagGenre = track.Genre;
            string decodedPath = Uri.UnescapeDataString(track.Path);
            var (filenameArtist, filenameTitle) = FilenameParser.Parse(track.Path);


            string bestRawTitle = !string.IsNullOrWhiteSpace(tagTitle) ? tagTitle : filenameTitle ?? Path.GetFileNameWithoutExtension(decodedPath);
            string? bestRawArtist = !string.IsNullOrWhiteSpace(tagArtist) ? tagArtist : filenameArtist;
            string bestAlbumArtist = tagAlbumArtist; // No filename equivalent for this.
            string bestAlbum = !string.IsNullOrWhiteSpace(tagAlbum) ? tagAlbum : "Unknown Album";
            string bestGenre = !string.IsNullOrWhiteSpace(tagGenre) ? tagGenre : "Unknown Genre";


            List<string> rawArtists = TaggingUtils.ExtractArtists(bestRawArtist, bestAlbumArtist);

            // 2. Run each raw artist through the cleaner and "flatten" the resulting lists
            List<string> artistNames = rawArtists
                .SelectMany(x => TaggingUtils.CleanArtist(track.Path, x, track.Title))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            string primaryArtistName = artistNames.FirstOrDefault() ?? "Unknown Artist";
            await realm.WriteAsync(() =>
            {
                songInDb.ArtistName = primaryArtistName;
                songInDb.OtherArtistsName = string.Join(", ", artistNames)!;

                var artistModel = realm.All<ArtistModel>().FirstOrDefault(a => a.Name == primaryArtistName);
                artistModel ??= new ArtistModel
                {
                    Id = ObjectId.GenerateNewId(),
                    Name = primaryArtistName,
                };
                var albumModel = realm.All<AlbumModel>().FirstOrDefault(a => a.Name == songInDb.AlbumName);
                if (albumModel is not null)
                {
                    songInDb.Album = albumModel;
                    if (albumModel.Artists.Contains(artistModel) == false)
                    {
                        albumModel.Artists.Add(artistModel);
                    }
                }
                songInDb.Artist = artistModel;
                songInDb.ArtistToSong.Add(artistModel);

                artistName = songInDb.ArtistToSong[0]!.Name;
                artistName ??= string.Empty;
            });
        }
        SelectedSongLastFMData = await lastfmService.GetTrackInfoAsync(artistName, songInDb.Title);
        if (SelectedSongLastFMData is not null)
        {

            //await UpdateSongFromLastFMDataAsync();
            SelectedSongLastFMData.Artist = await lastfmService.GetArtistInfoAsync(artistName);

            SelectedSongLastFMData.Album = await lastfmService.GetAlbumInfoAsync(artistName, songInDb.AlbumName);

        }
        //SimilarTracks = await lastfmService.GetSimilarAsync(artistName, SelectedSongLastFMData.Name);

        //await UpdateSongArtistInDbWithLastFMData();
        //await UpdateSongAlbumInDbWithLastFMData();
    }


    public async Task LoadArtistLastFMDataAsync(ArtistModelView? art)
    {
        try
        {

            if (art is null || art.Name == "Unknown Artist")
            {
                return;
            }


            var realm = RealmFactory.GetRealmInstance();
            var artInDb
                = realm.Find<ArtistModel>(art.Id);
            if (artInDb is null)
                return;
            var artistName = artInDb.Name;


            //await UpdateSongFromLastFMDataAsync();
            var lastFMArt = await lastfmService.GetArtistInfoAsync(artistName);
            if (lastFMArt is null)
                return;
            art.ImagePath = lastFMArt.Images.FirstOrDefault(x => x.Size == "mega")?.Url;
            realm.Write(
                () =>
                {
                    artInDb.ImagePath = art.ImagePath;
                });

            //SimilarTracks = await lastfmService.GetSimilarAsync(artistName, SelectedSongLastFMData.Name);

            //await UpdateSongArtistInDbWithLastFMData();
            //await UpdateSongAlbumInDbWithLastFMData();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }


    [RelayCommand]
    public async Task FetchSongOnlineOnLastFM(SongModelView song)
    {
        var cleanTitle = TaggingUtils.CleanTitle(song.FilePath, song.Title, song.AlbumName, song.ArtistName);
        var cleanArtist = TaggingUtils.CleanArtist(song.FilePath, song.ArtistName, song.Title);
        LastFMTrackOnEditPage = await lastfmService.GetTrackInfoAsync(cleanArtist.First(), cleanTitle);
    }

    [ObservableProperty]
    public partial ObservableCollection<Hqub.Lastfm.Entities.Track>? CollectionUserTopTracks { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<Hqub.Lastfm.Entities.Album>? CollectionUserTopAlbums { get; set; }

    [RelayCommand]
    public async Task LoadUserLastFMDataAsync(LastFMUserView? user)
    {
        if (user is null) return;
        if (lastfmService.AuthenticatedUser is null) return;

        // 1. Fetch all data concurrently (much faster than awaiting one by one)
        var tasks = new
        {
            Recent = lastfmService.GetUserRecentTracksAsync(lastfmService.AuthenticatedUser, 150),

            UserLibraryArtists = lastfmService.GetUserLibArtistsAsync(),
            TopTracks = lastfmService.GetUserTopTracksAsync(),
            TopAlbums = lastfmService.GetTopUserAlbumsAsync(),
            Loved = lastfmService.GetLovedTracksAsync(),

        };

        await Task.WhenAll(tasks.Recent, tasks.UserLibraryArtists,tasks.TopTracks, tasks.TopAlbums, tasks.Loved);

        // 2. Build the Lookup ONE time (O(N))
        // This creates a hash map of your local library for instant matching
        var localLibraryLookup = LastFmEnricher.BuildLocalLibraryLookup(_baseViewModel!.SearchResults);

        // 3. Process the results using the Centralized Logic
        // We pass 'SearchResults' as the second arg for the Fallback Duration check

        // Recent Tracks
        var recentTracks = await tasks.Recent;
        if (recentTracks is not null)
        {
            CollectionOfUserRecentTracks = recentTracks
                .EnrichWithLocalData(localLibraryLookup, _baseViewModel.SearchResults)
                .ToObservableCollection();
        }

        // Top Tracks
        var topTracks = await tasks.TopTracks;
        if (topTracks is not null)
        {
            CollectionUserTopTracks = topTracks
            .EnrichWithLocalData(localLibraryLookup, _baseViewModel.SearchResults)
            .ToObservableCollection();
        }
        // Loved Tracks
        var lovedTracks = await tasks.Loved;
        if (lovedTracks is not null)
        {
            CollectionOfUserLovedTracks = lovedTracks
            .EnrichWithLocalData(localLibraryLookup, _baseViewModel.SearchResults)
            .ToObservableCollection();
        }
        var topAlbums = await tasks.TopAlbums;
        TopUserArtistsInLibrary = await tasks.UserLibraryArtists;


        var realm = RealmFactory.GetRealmInstance();
        IQueryable<AlbumModel>? allRealmAlbums = realm.All<AlbumModel>();

            // 4. Run the Pipeline
            if (topAlbums is not null)
            {
                CollectionUserTopAlbums = topAlbums
            .EnrichWithLocalData(allRealmAlbums)
            .ToObservableCollection();
            }

    }
    [ObservableProperty]
    public partial ObservableCollection<Hqub.Lastfm.Entities.Artist>? TopUserArtistsInLibrary { get; set; }
    public async Task LoadAlbumLastFMDataAsync(AlbumModelView? alb)
    {
        if (alb is null || alb.Name == "Unknown Album")
        {
            return;
        }


        var realm = RealmFactory.GetRealmInstance();
        var albInDb
            = realm.Find<AlbumModel>(alb.Id);
        if (albInDb is null)
            return;
        var artistName = albInDb.Artist?.Name;
        if (artistName is null)
            return;

        //await UpdateSongFromLastFMDataAsync();
        var lastFMArt = await lastfmService.GetAlbumInfoAsync(artistName, albInDb.Name);
        if (lastFMArt is null)
            return;
        alb.ImagePath = lastFMArt.Images.FirstOrDefault(x => x.Size == "mega")?.Url;
        await realm.WriteAsync(
            () =>
            {
                albInDb.ImagePath = alb.ImagePath;
            });

        //SimilarTracks = await lastfmService.GetSimilarAsync(artistName, SelectedSongLastFMData.Name);

        //await UpdateSongArtistInDbWithLastFMData();
        //await UpdateSongAlbumInDbWithLastFMData();
        alb = albInDb.ToAlbumModelView();
    }



    public async Task UpdateSongFromLastFMDataAsync()
    {
        if (SelectedSong is null || SelectedSongLastFMData is null)
        {
            return;
        }

        var realm = RealmFactory.GetRealmInstance();
        await realm.WriteAsync
        (() =>
        {
            var songInDb = realm.Find<SongModel>(SelectedSong.Id);
            if (songInDb is null)
            {
                return;
            }
            if (SelectedSongLastFMData.Album is not null && SelectedSongLastFMData.Album.Tracks is not null)
                songInDb.TrackNumber = SelectedSongLastFMData.Album.Tracks.IndexOf(SelectedSongLastFMData) + 1;

            songInDb.Description = SelectedSongLastFMData.Wiki?.Summary ?? string.Empty;

            realm.Add(songInDb, update: true);
        });

    }

    public async Task UpdateSongArtistInDbWithLastFMData()
    {
        if (SelectedSong is null || SelectedSongLastFMData is null)
        {
            return;
        }

        var realm = RealmFactory.GetRealmInstance();

        await realm.WriteAsync
        (() =>
        {
            var songInDb = realm.Find<SongModel>(SelectedSong.Id);
            if (songInDb is null)
            {
                return;
            }
            var artist = songInDb.Artist;
            if (artist is null)
            {
                return;
            }
            artist.Url = SelectedSongLastFMData.Artist.Url;
            if (artist.Bio is null)
            {
                artist.Bio = SelectedSongLastFMData.Artist.Biography?.Summary;
            }
            if (string.IsNullOrEmpty(artist.ImagePath))
                artist.ImagePath = SelectedSongLastFMData.Artist.Images.FirstOrDefault(x => x.Size == "mega")?.Url;

            artist.Name = SelectedSongLastFMData.Artist.Name;

            realm.Add(artist, update: true);
        });


    }



    public async Task UpdateSongAlbumInDbWithLastFMData()
    {
        if (SelectedSong is null || SelectedSongLastFMData is null)
        {
            return;
        }

        var realm = RealmFactory.GetRealmInstance();


        await realm.WriteAsync(() =>
        {
            var songInDb = realm.Find<SongModel>(SelectedSong.Id);
            if (songInDb is null)
            {
                return;
            }
            var artist = songInDb.Album;
            if (artist is null)
            {
                return;
            }
            artist.Url = SelectedSongLastFMData.Album.Url;
            artist.ImagePath = SelectedSongLastFMData.Artist.Images.FirstOrDefault(x => x.Size == "mega")?.Url;
            artist.Name = SelectedSongLastFMData.Artist.Name;

            realm.Add(artist, update: true);
        });
    }


    public void LoadSongLastFMMoreData()
    {
        if (SelectedSong is null)
        {
            return;
        }
        //SimilarTracks=   await lastfmService.GetSimilarAsync(SelectedSong.ArtistName, SelectedSong.Title);

        //IEnumerable<LrcLibSearchResult>? s = await LyricsMetadataService.SearchOnlineManualParamsAsync(SelectedSong.Title, SelectedSong.ArtistName, SelectedSong.AlbumName);
        //AllLyricsResultsLrcLib = s.ToObservableCollection();
    }

    [ObservableProperty]
    public partial ObservableCollection<Hqub.Lastfm.Entities.Track>? SimilarTracks { get; set; }
    #endregion
    #region --- Last.fm Advanced Visualizations ---

   

    // 6. Milestone Progress (Distance to next 10,000 plays milestone)
    // Chart: Syncfusion CircularGauge / Radial Bar
    // Value = playcount, Maximum = next milestone
    [ObservableProperty] public partial double LastFmMilestoneProgress { get; set; }
    [ObservableProperty] public partial string LastFmMilestoneText { get; set; }
    #endregion
}
