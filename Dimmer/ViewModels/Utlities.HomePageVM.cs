using Parse.LiveQuery;

namespace Dimmer_MAUI.ViewModels;

public partial class HomePageVM
{

    #region HomePage MultiSelect's Logic
    public bool EnableContextMenuItems = true;
    [ObservableProperty]
    public partial ObservableCollection<SongModelView> MultiSelectSongs { get; set; } = Enumerable.Empty<SongModelView>().ToObservableCollection();
    [ObservableProperty]
    public partial string? MultiSelectText { get; set; } = string.Empty;



    [RelayCommand]
    async Task MultiSelectUtilClicked(int SelectedBtn)
    {
        switch (SelectedBtn)
        {   
            case 0: 
                await DeleteFile(TemporarilyPickedSong);
                break;
            case 1:
                break;
            case 2:
                break;
            default:
                break;
        }
    }
    #endregion

    [RelayCommand]
    void DummyFunc()
    {
        GetAllAlbums();
    }

    [RelayCommand]
    void BringAppToFront()
    {
#if WINDOWS
        MiniPlayBackControlNotif.BringAppToFront();
#endif
    }
    

    [RelayCommand]
    async Task ShowSleepTimerPopup()
    {
        await Shell.Current.ShowPopupAsync(new SleepTimerSelectionPopup(this));
    }
    private CancellationTokenSource? _sleepTimerCancellationTokenSource;

    [RelayCommand]
    async Task StartSleepTimer(double value)
    {
        // Convert value to milliseconds (e.g., value in minutes * 60 * 1000)
        var valueInMilliseconds = value * 60 * 1000;

        // Cancel any existing timer
        _sleepTimerCancellationTokenSource?.Cancel();

        // Create a new CancellationTokenSource
        _sleepTimerCancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = _sleepTimerCancellationTokenSource.Token;

        try
        {
            string min = value < 2 ? "Minute" : "Minutes";
            var toast = Toast.Make($"Started ! Song will pause after {value}{min}}}");
            await toast.Show(cts.Token);

            await Task.Delay((int)valueInMilliseconds, cancellationToken);

            // If the delay completed without cancellation, pause the song
            if (!cancellationToken.IsCancellationRequested && IsPlaying)
            {
                await PauseSong();
            }
        }
        catch (TaskCanceledException ex)
        {
            // Handle the cancellation (if needed but i'm not sure I will ngl but still)
            Debug.WriteLine(ex.Message);
        }
    }

    public async Task ExitingApp()
    {
#if WINDOWS && NET9_0
        if (IsPlaying)
        {
            _= this.PauseSong();
        }
#endif
        if (TemporarilyPickedSong is not null)
        {
            AppSettingsService.LastPlayedSongPositionPref.SetLastPosition(CurrentPositionPercentage);
            AppSettingsService.LastPlayedSongSettingPreference.SetLastPlayedSong(TemporarilyPickedSong.LocalDeviceId);
        }
        await APIKeys.LogoutDevice();
    }
    [ObservableProperty]
    bool iIsMultiSelectOn;
    [RelayCommand]
    async Task DeleteFile(SongModelView? song)
    {

        switch (IsMultiSelectOn)
        {
            case true:
                bool result = await Shell.Current.DisplayAlert("Delete Song", $"Are you sure you want to Delete Selections " +
                   $"from your Device?", "Yes", "No");
                if (result)
                {
                    PlatSpecificUtils.MultiDeleteSongFiles(MultiSelectSongs);
                    
                    // Loop through all songs in MultiSelectSongs and remove them from DisplayedSongs if they exist
                    foreach (var selectedSong in MultiSelectSongs)
                    {
                        DisplayedSongs?.Remove(selectedSong);
                    }
                    if (MultiSelectSongs.Contains(TemporarilyPickedSong!))
                    {
                        await PlayNextSong();
                    }
                    await PlayBackService.MultiDeleteSongFromHomePage(MultiSelectSongs);

                    MultiSelectSongs.Clear();
                    MultiSelectText = $"{MultiSelectSongs.Count} Song{(MultiSelectSongs.Count > 1 ? "s" : "")} Selected";
                }
                break;

            case false:

                if (!EnableContextMenuItems)
                    break;
                song ??= SelectedSongToOpenBtmSheet;
                bool res = await Shell.Current.DisplayAlert("Delete File", $"Are you sure you want to Delete Song: {song.Title} " +
                    $"by {song.ArtistName}?", "Yes", "No");
                
                if(res)
                {
                    PlatSpecificUtils.DeleteSongFile(song);
                    if (song == TemporarilyPickedSong)
                    {
                        await PlayNextSong();
                    }
                    DisplayedSongs?.Remove(song);
                    PlayBackService.DeleteSongFromHomePage(song);
                }
                break;

        }
    }

    [RelayCommand]
    void OpenSongFolder() //SongModel SelectedSong)
    {
        if(!EnableContextMenuItems) return;
#if WINDOWS
        string filePath = string.Empty;
        if (CurrentPage == PageEnum.NowPlayingPage)
        {
            filePath = SelectedSongToOpenBtmSheet.FilePath;
        }
        else
        {
            filePath = SelectedSongToOpenBtmSheet.FilePath;
        }
        var directoryPath = Path.GetDirectoryName(filePath);

        if (!string.IsNullOrEmpty(directoryPath) && Directory.Exists(directoryPath))
        {
            // Open File Explorer and select the file
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{filePath}\"",
                UseShellExecute = true
            });
        }
#endif
    }

    [RelayCommand]
    async Task OpenViewSongDetailsPopup()
    {
        
        await Shell.Current.ShowPopupAsync(new ViewSongMetadataPopupView(this));
    }

    [RelayCommand]
    async Task OpenEditSongPopup(SongModelView? song)
    {
        await Shell.Current.ShowPopupAsync(new EditSongPopup(this));
    }


#if WINDOWS
    public AppWindowPresenter AppWinPresenter { get; set; }
#endif
    [ObservableProperty]
    bool isStickToTop = false;
    [RelayCommand]
    void ToggleStickToTop()
    {       
        IsStickToTop = !IsStickToTop;

#if WINDOWS
        PlatSpecificUtils.ToggleWindowAlwaysOnTop(IsStickToTop, AppWinPresenter);
#endif
    }
    public bool IsSleek = false;
    [RelayCommand]
    void ToggleSleekMode(bool isSleek)
    {
        IsSleek = !IsSleek;
#if WINDOWS
        PlatSpecificUtils.ToggleFullScreenMode(IsSleek, AppWinPresenter);
#endif
    }

    #region Search Song On... ContextMenu Options

    [RelayCommand]
    async Task CntxtMenuSearch(int param)
    {
        if (SelectedSongToOpenBtmSheet is null)
        {
            return; // show error msg here or cue
        }
        if (CurrentPage == PageEnum.NowPlayingPage)
        {
            SelectedSongToOpenBtmSheet = TemporarilyPickedSong!;
        }
        switch (param)
        {
            case 0:
                await OpenGoogleSearch();
                break;
            case 1:
                await OpenYouTubeSearchResults();
                break;
            case 2:
                await OpenSpotifySearchResults();
                break;
            case 3:
                break;
            case 4:
                break;
            case 5:
                    break;
            default:
                break;
        }
    }
    //I want to use SelectedSongToOpenBtmSheet, which has Title, ArtistName and AlbumName
    async Task OpenGoogleSearch()
    {
        if (SelectedSongToOpenBtmSheet != null)
        {
            string query = $"{SelectedSongToOpenBtmSheet.Title} - {SelectedSongToOpenBtmSheet.ArtistName}";
            //if (!string.IsNullOrEmpty(SelectedSongToOpenBtmSheet.AlbumName))
            //{
            //    query += $" {SelectedSongToOpenBtmSheet.AlbumName}";
            //}
            string searchUrl = $"https://www.google.com/search?q={Uri.EscapeDataString(query)}";
            await Launcher.OpenAsync(new Uri(searchUrl));
        }
    }

    async Task OpenGoogleLyricsSearch()
    {
        if (SelectedSongToOpenBtmSheet != null)
        {
            string query = $"{SelectedSongToOpenBtmSheet.Title} by {SelectedSongToOpenBtmSheet.ArtistName} lyrics";
            string searchUrl = $"https://www.google.com/search?q={Uri.EscapeDataString(query)}";
            await Launcher.OpenAsync(new Uri(searchUrl));
        }
    }

    async Task OpenYouTubeSearchResults()
    {
        if (SelectedSongToOpenBtmSheet != null)
        {
            string query = $"{SelectedSongToOpenBtmSheet.ArtistName} {SelectedSongToOpenBtmSheet.Title}";
            string searchUrl = $"https://www.youtube.com/results?search_query={Uri.EscapeDataString(query)}";
            await Launcher.OpenAsync(new Uri(searchUrl));
        }
    }
    async Task OpenGoogleArtistSearch()
    {
        if (SelectedSongToOpenBtmSheet != null)
        {
            string query = $"{SelectedSongToOpenBtmSheet.ArtistName}";
            string searchUrl = $"https://www.google.com/search?q={Uri.EscapeDataString(query)}";
            await Launcher.OpenAsync(new Uri(searchUrl));
        }
    }

    async Task OpenGoogleAlbumSearch()
    {
        if (SelectedSongToOpenBtmSheet != null && !string.IsNullOrEmpty(SelectedSongToOpenBtmSheet.AlbumName))
        {
            string query = $"{SelectedSongToOpenBtmSheet.AlbumName} {SelectedSongToOpenBtmSheet.ArtistName}";
            string searchUrl = $"https://www.google.com/search?q={Uri.EscapeDataString(query)}";
            await Launcher.OpenAsync(new Uri(searchUrl));
        }
    }
    async Task OpenYouTubeArtistProfile()
    {
        if (SelectedSongToOpenBtmSheet != null)
        {
            string query = $"{SelectedSongToOpenBtmSheet.ArtistName}";
            string searchUrl = $"https://www.youtube.com/results?search_query={Uri.EscapeDataString(query)}";
            await Launcher.OpenAsync(new Uri(searchUrl));
        }
    }

    async Task OpenSpotifyArtistProfile()
    {
        if (SelectedSongToOpenBtmSheet != null && !string.IsNullOrEmpty(SelectedSongToOpenBtmSheet.ArtistName))
        {
            string searchUrl = $"https://open.spotify.com/search/{Uri.EscapeDataString(SelectedSongToOpenBtmSheet.ArtistName)}";
            await Launcher.OpenAsync(new Uri(searchUrl));
        }
    }

    async Task OpenSpotifyAlbumProfile()
    {
        if (SelectedSongToOpenBtmSheet != null && !string.IsNullOrEmpty(SelectedSongToOpenBtmSheet.AlbumName))
        {
            string searchUrl = $"https://open.spotify.com/search/{Uri.EscapeDataString(SelectedSongToOpenBtmSheet.AlbumName)} {Uri.EscapeDataString(SelectedSongToOpenBtmSheet.AlbumName)}";
            await Launcher.OpenAsync(new Uri(searchUrl));
        }
    }

    async Task OpenSpotifySearchResults()
    {
        if (SelectedSongToOpenBtmSheet != null)
        {
            string query = $"{SelectedSongToOpenBtmSheet.Title} {SelectedSongToOpenBtmSheet.ArtistName}";
            string searchUrl = $"https://open.spotify.com/search/{Uri.EscapeDataString(query)}";
            await Launcher.OpenAsync(new Uri(searchUrl));
        }
    }
    #endregion

    [ObservableProperty]
    bool isRetrievingCovers = true;
    [RelayCommand]
    async Task ReloadCoverImageAsync()
    {
        IsRetrievingCovers = true;
        // Run the work on a background thread
        await Task.Run(() =>
        {
            // Perform the operations on a background thread
            DisplayedSongs = PlaybackUtilsService.CheckCoverImage(DisplayedSongs);

            // Call the non-awaitable method directly
            SongsMgtService.AddSongBatchAsync(DisplayedSongs); // No await needed
        });

        // Return to the main thread to show the alert
        await Shell.Current.DisplayAlert("Download Complete!", "All Downloaded", "OK");
        IsRetrievingCovers = false;
    }



    [ObservableProperty]
    bool isTemporarySongNull = true;


    [ObservableProperty]
    bool isOnSearchMode = false;


    [RelayCommand]
    async Task DownloadAlbumImage(AlbumModelView album)
    {
      var firstSongOfSpectifAlbum = AllArtistsAlbumSongs.FirstOrDefault()!;
      SelectedAlbumOnArtistPage.AlbumImagePath = await LyricsManagerService.FetchAndDownloadCoverImage(firstSongOfSpectifAlbum.Title,firstSongOfSpectifAlbum.ArtistName!, firstSongOfSpectifAlbum.AlbumName!);
    }

    [ObservableProperty]
    string shareImgPath = string.Empty;
    
    public Color[] ShareColors { get; } = new Color[]{
            Color.FromArgb("#FF0000"),
            Color.FromArgb("#2365BD"),
            Color.FromArgb("#4C342F"),
            Color.FromArgb("#661D98"),
            };

    public FlyoutItem AppFlyout { get; set; }
    [RelayCommand]
    public void SwitchTab(int index)
    {
        switch (index)
        {
            case 0:
                AppFlyout.CurrentItem = AppFlyout.Items[0];
                break;
            case 1:
                AppFlyout.CurrentItem = AppFlyout.Items[1];
                break;
            case 2:
                AppFlyout.CurrentItem = AppFlyout.Items[2];
                break;
            case 3:
                AppFlyout.CurrentItem = AppFlyout.Items[3];
                break;
            case 4:
                AppFlyout.CurrentItem = AppFlyout.Items[4];
                break;
            default:
                break;
        }
    }

    public void ToggleFlyout(bool isOpenFlyout=false)
    {
        IsFlyOutPaneOpen = false;
        if (Shell.Current == null)
        {
            return;
        }
        if (isOpenFlyout)
        {
            if(Shell.Current.FlyoutBehavior != FlyoutBehavior.Locked)
            {
                IsFlyOutPaneOpen = true;
                Shell.Current.FlyoutBehavior = FlyoutBehavior.Locked;
            }
        }
        else
        {
            if (Shell.Current.FlyoutBehavior != FlyoutBehavior.Flyout)
            {
                IsFlyOutPaneOpen = false;
                Shell.Current.FlyoutBehavior = FlyoutBehavior.Flyout;
                _ = Task.Delay(500);
                Shell.Current.FlyoutIsPresented = true;
            }
        }
    }

    [RelayCommand]
    void LogOut()
    {
        ParseClient.Instance.LogOut();
    }

    [ObservableProperty]
    bool isSyncingSongs = false;    

    [ObservableProperty]
    bool isLoggedIn = false;    
    public async Task FullSync()
    {
        SongsMgtService.CurrentUserOnline = this.CurrentUserOnline;
        IsSyncingSongs = true;
        await SongsMgtService.SendAllDataToServerAsInitialSync();
        await SongsMgtService.GetAllDataFromOnlineAsync();

        SyncRefresh();

        IsSyncingSongs = false;

        await Shell.Current.DisplayAlert("Success!", "Syncing Complete", "OK");
    }

    partial void OnCurrentUserOnlineChanged(ParseUser? oldValue, ParseUser? newValue)
    {
        if (newValue is not null)
        {
            SongsMgtService.UpdateUserLoginDetails(newValue);
            IsLoggedIn = true;
        }
        else
        {
            IsLoggedIn = false; //do more here 
        }
    }


    [RelayCommand]
    public async Task BackupAllUserData()
    {
        if (!CurrentUser.IsAuthenticated)
        {
            await Shell.Current.DisplayAlert("Currently Offline", "Please Log in to Backup your data", "Ok");
            return;
        }

        List<object> allPlayDatalinks = new();
        foreach (PlayDataLink item in AllPlayDataLinks)
        {
            allPlayDatalinks.Add(ObjectHelper.ClassToDictionary(item));
        }
        
        List<object> AllAlbumModelView = new();
        foreach (AlbumModelView item in AllAlbums)
        {
            
            AllAlbumModelView.Add(ObjectHelper.ClassToDictionary(item));
        }
        List<object> AllGenresModelView = new();
        foreach (GenreModelView item in SongsMgtService.AllGenres)
        {
            AllGenresModelView.Add(ObjectHelper.ClassToDictionary(item));
        }
        
        List<object> ArtistModelV = new();
        foreach (ArtistModelView item in AllArtists)
        {
            ArtistModelV.Add(ObjectHelper.ClassToDictionary(item));
        }
        List<object> AllLinkss = new();
        foreach (AlbumArtistGenreSongLinkView item in AllLinks)
        {
            AllLinkss.Add(ObjectHelper.ClassToDictionary(item));
        }
        
        List<object> AllSongModelView = new();
        foreach (SongModelView item in DisplayedSongs)
        {
            AllSongModelView.Add(ObjectHelper.ClassToDictionary(item));
        }
        
        List<object> AllPlayLists = new();
        foreach (PlaylistModelView item in DisplayedPlaylists)
        {
            AllPlayLists.Add(ObjectHelper.ClassToDictionary(item));
        }
        

        Dictionary<string, object> dataToBackUpToParseInitially = new Dictionary<string, object>
        {
            { "PlayDataLink", allPlayDatalinks },
            { "SongModelView", AllSongModelView },
            { "ArtistModelView", ArtistModelV },
            { "AlbumModelView", AllAlbumModelView },
            { "GenresModelView", AllGenresModelView },
            { "AlbumArtistGenreSongLink", AllLinkss },
            { "AllPlayLists", AllPlayLists }
            
        };
        
        bool overallSuccess = true;
        Dictionary<string, object> allBackupErrors = new Dictionary<string, object>();

        foreach (var dataPair in dataToBackUpToParseInitially)
        {
            string className = dataPair.Key;
            object data = dataPair.Value;

            Dictionary<string, object> singleClassBackupData = new Dictionary<string, object>
            {
                { className, data }
            };

            try
            {
                var result = await ParseClient.Instance.CallCloudCodeFunctionAsync<string>("backupUserData", singleClassBackupData);

                if (result != "Back Up Complete" && result.StartsWith("Failed on class"))
                {
                    overallSuccess = false;
                    allBackupErrors[className] = result;
                    Debug.WriteLine($"Backup Errors for {className}: {result}");
                }
                else
                {
                    Debug.WriteLine($"Backup for {className} successful!");
                }
            }
            catch (Exception ex)
            {
                overallSuccess = false;
                allBackupErrors[className] = new Dictionary<string, string> 
                { 
                    { 
                        "Error", 
                        ex.Message 
                    } 
                };
                Debug.WriteLine($"Parse Exception during backup of {className}: {ex.Message}");
            }
        }

        if (overallSuccess)
        {
            Debug.WriteLine("Overall Backup Successful!");
            await Shell.Current.DisplayAlert("Success!", "Backup Complete", "Ok");
        }
        else
        {
            Debug.WriteLine("Overall Backup Failed with Errors.");
            // Display a more informative error message to the user, perhaps listing the classes with errors
            string errorMessage = "Backup Failed for some data. See debug output for details.";
            if (allBackupErrors.Count != 0)
            {
                errorMessage = "Backup Failed for the following data: " + string.Join(", ", allBackupErrors.Keys);
            }
            await Shell.Current.DisplayAlert("Error!", errorMessage, "Ok");
        }
    }

    [RelayCommand]
    public async Task RestoreUserData()
    {
        try
        {
            var response = await ParseClient.Instance.CallCloudCodeFunctionAsync<Dictionary<string, object>>("restoreUserData", new Dictionary<string, object>());

            if (response != null && response.ContainsKey("data"))
            {
                var data = response["data"] as Dictionary<string, object>;

                if (data != null)
                {
                    // Extract and convert data for each class
                    var songs = ExtractData<SongModelView>(data, "SongModelView");
                    PlayBackService.LoadSongsWithSorting(songs.ToObservableCollection());

                    var playDataLinks = ExtractData<PlayDateAndCompletionStateSongLink>(data, "PlayDataLink");

                    var albums = ExtractData<AlbumModel>(data, "AlbumModelView");
                    var allGenres = ExtractData<GenreModel>(data, "GenresModelView");
                    var allPlaylists = ExtractData<PlaylistModel>(data, "AllPlayLists");
                    var otherLinks = ExtractData<AlbumArtistGenreSongLink>(data, "AllLinks");
                    var songsData = ExtractData<SongModel>(data, "SongModelView");

                    // Call RestoreAllOnlineData with the extracted data
                    SongsMgtService.RestoreAllOnlineData(playDataLinks, songsData, albums, allGenres, allPlaylists, otherLinks);

                    RefreshPlaylists();
                }
                else
                {
                    Console.WriteLine("Data extraction failed, no valid data found.");
                }
            }
            else
            {
                Console.WriteLine("No data returned from cloud function.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"General Exception in RestoreUserData: {ex.Message}");
        }
    }

    // Helper function to extract and convert data for a specific class
    private List<T> ExtractData<T>(Dictionary<string, object> response, string className) where T : class
    {
        if (response.TryGetValue(className, out object? value))
        {
            if (value is List<object> dataList)
            {
                return dataList.Cast<IDictionary<string, object>>()
                               .Select(item => ObjectHelper.MapFromDictionary<T>(item))
                               .Where(item => item != null) // Filter out null items
                               .ToList();
            }
        }

        Console.WriteLine($"No data found for class: {className}");
        return new List<T>(); // Return an empty list if the class data is not found
    }

    [ObservableProperty]
    public partial ObservableCollection<CurrentDeviceStatus> OtherConnectedDevices { get; set; } = Enumerable.Empty<CurrentDeviceStatus>().ToObservableCollection();
    public async Task GetLoggedInDevicesForUser()
    {
        var user = CurrentUserOnline;
        if (user == null)
        {
            return;
        }
        var query = ParseClient.Instance.GetQuery("DeviceStatus")
            .WhereEqualTo("deviceOwner", user)
            .WhereEqualTo("isOnline", true); // Assuming 'isOnline' field indicates current login status

        var deviceObjects = await query.FindAsync();

        ObservableCollection<CurrentDeviceStatus> devices = new ();

        foreach (var device in deviceObjects)
        {
            var s = GeneralStaticUtilities.MapFromParseObjectToClassObject<CurrentDeviceStatus>(device);
            devices.Add(s);
        }
        OtherConnectedDevices = devices;
    }


    [RelayCommand]
    public async Task<bool> ForgottenPassword()
    {
        string userEmail = await Shell.Current.DisplayPromptAsync("Password Reset", "Enter your email address to reset your password", "OK", "Cancel",keyboard:Keyboard.Email,initialValue:CurrentUser.UserEmail );
        if (string.IsNullOrEmpty(userEmail))
        {
            return false;
        }

        await ParseClient.Instance.RequestPasswordResetAsync(CurrentUser.UserEmail);

        await Shell.Current.DisplayAlert("Confirm Passsword Reset!", "Please Verify Your Email!", "Ok");
        return true;
    }

    [RelayCommand]
    public async Task<bool> SignUpUserAsync()
    {
        if (string.IsNullOrEmpty(CurrentUser.UserEmail) || string.IsNullOrEmpty(CurrentUser.UserPassword))
        {
            await Shell.Current.DisplayAlert("Error!", "Please Enter Email and Password", "Ok");
            return false;
        }
        try
        {
            ParseUser user = new ParseUser()
            {
                Username = CurrentUser.UserEmail,
                Password = CurrentUser.UserPassword,
                Email = CurrentUser.UserEmail
            };

            var usr = await ParseClient.Instance.SignUpWithAsync(user);

            await Shell.Current.DisplayAlert("Last Step!", "Please Verify Your Email!", "Ok");
            ParseClient.Instance.LogOut();
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error when registering user: {ex.Message}");
            return false;
        }
    }

    [RelayCommand]
    public async Task<bool> LogInParseOnline(bool isSilent=true)
    {

        if (Connectivity.Current.NetworkAccess != Microsoft.Maui.Networking.NetworkAccess.Internet)
        {
            if (!isSilent)
            {
                await Shell.Current.DisplayAlert("Error!", "No Internet Connection", "Ok");

                return false;
            }
            else
            {
                return false;
            }
;
        }
        if (string.IsNullOrEmpty(CurrentUser.UserName) || string.IsNullOrEmpty(CurrentUser.UserPassword) )
        {
            if (!isSilent)
            {
                await Shell.Current.DisplayAlert("Error!", "Please Verify Email/Password", "Ok");

                return false;
            }
            else
            {
                return false;
            }

        }

        var currentParseUser = await ParseClient
            .Instance
            .LogInWithAsync(CurrentUser.UserName, CurrentUser.UserPassword);

        if (currentParseUser is null)
        {
            if (!isSilent)
            {
                await Shell.Current.DisplayAlert("Error!", "Invalid Username or Password", "Ok");
                
                return false;
            }
            else
            {
                return false;
            }
        }

        if (!isSilent)
        {
            await Shell.Current.DisplayAlert("Success !", $"Welcome Back {currentParseUser.Username}!", "Thanks");
        }
        var query = ParseClient.Instance.GetQuery("DeviceStatus")
            .WhereEqualTo("deviceOwner", currentParseUser.Email)
            .WhereEqualTo("deviceName", DeviceInfo.Name);

        var existingDevices = await query.FindAsync();
        var existingDevice = existingDevices.FirstOrDefault();

        if (existingDevice != null)
        {
            existingDevice["lastLogin"] = DateTime.Now;
            existingDevice["isOnline"] = true;

            await existingDevice.SaveAsync();
        }
        else
        {
            var newDevice = new ParseObject("DeviceStatus");
            newDevice["deviceOwner"] = currentParseUser.Email;
            newDevice["deviceName"] = DeviceInfo.Name;
            newDevice["deviceType"] = DeviceInfo.Idiom.ToString();
            newDevice["lastLogin"] = DateTime.Now;
            newDevice["isOnline"] = true;
            await newDevice.SaveAsync();
        }
        CurrentUserOnline = currentParseUser;
        CurrentUser.IsAuthenticated = true;
        CurrentUser.UserIDOnline = currentParseUser.ObjectId;

        currentParseUser.Password= CurrentUser.UserPassword;
        SongsMgtService.UpdateUserLoginDetails(currentParseUser);
        return true;
    }

    [RelayCommand]
    public async Task<bool> LogUserOut()
    {
        await APIKeys.LogoutDevice();
        CurrentUser.IsAuthenticated = false;
        CurrentUser.LastSessionDate = DateTime.Now;
        CurrentUserOnline = null;
        return true;
    }
}