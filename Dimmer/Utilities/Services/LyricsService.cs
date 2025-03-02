using System.Text.Json;
using System.Text.RegularExpressions;

namespace Dimmer_MAUI.Utilities.Services;
public class LyricsService : ILyricsService
{

    private BehaviorSubject<IList<LyricPhraseModel>> _synchronizedLyricsSubject = new([]);
    public IObservable<IList<LyricPhraseModel>> SynchronizedLyricsStream => _synchronizedLyricsSubject.AsObservable();

    private BehaviorSubject<LyricPhraseModel> _currentLyricSubject = new (new LyricPhraseModel(null));
    public IObservable<LyricPhraseModel> CurrentLyricStream => _currentLyricSubject.AsObservable();

    private BehaviorSubject<string> _unsyncedLyricsSubject = new("");

    private IDisposable? _lyricUpdateSubscription;
    public IObservable<string> UnSynchedLyricsStream => _unsyncedLyricsSubject.AsObservable();

    public IPlaybackUtilsService PlayBackService { get; }
    public ISongsManagementService SongsManagementService { get; }
    public IFileSaver FileSaver { get; }

    bool hasSyncedLyrics;
    public LyricsService(IPlaybackUtilsService songsManagerService, ISongsManagementService songsManagementService, IFileSaver fileSaver)
    {
        PlayBackService = songsManagerService;
        SongsManagementService = songsManagementService;
        FileSaver = fileSaver;
        SubscribeToPlayerStateChanges();
        _currentLyricSubject.OnNext(new LyricPhraseModel() { Text = ""});
    }
    MediaPlayerState pState;
    
    private void SubscribeToPlayerStateChanges()
    {
        PlayBackService.PlayerState.Subscribe(async state =>
        {
            switch (state)
            {
                case MediaPlayerState.Initialized:
                    pState = MediaPlayerState.Initialized;
                    await LoadLyrics(PlayBackService.CurrentlyPlayingSong);
                    break;
                case MediaPlayerState.Playing:
                    pState = MediaPlayerState.Playing;
                    await LoadLyrics(PlayBackService.CurrentlyPlayingSong);
                    //_currentLyricSubject.OnNext(new LyricPhraseModel() { Text = "No Lyrics" });
                    break;
                case MediaPlayerState.Paused:
                    pState = MediaPlayerState.Paused;
                    StopLyricIndexUpdateTimer();
                    break;
                case MediaPlayerState.Stopped:
                    pState = MediaPlayerState.Stopped;
                    StopLyricIndexUpdateTimer();
                    break;
                case MediaPlayerState.LyricsLoad:
                    pState = MediaPlayerState.LyricsLoad;
                    await LoadLyrics(PlayBackService.CurrentlyPlayingSong);
                    break;
                case MediaPlayerState.CoverImageDownload:
                    pState = MediaPlayerState.CoverImageDownload;
                    await FetchAndDownloadCoverImage(PlayBackService.CurrentlyPlayingSong.Title, PlayBackService.CurrentlyPlayingSong.ArtistName, PlayBackService.CurrentlyPlayingSong.AlbumName, PlayBackService.CurrentlyPlayingSong);

                        break;
                default:
                    break;
            }

        });
    }


    #region Manage Loadings and Initializations
    public async Task LoadLyrics(SongModelView song)
    {
        try
        {            
            if (song is null)
            {
                _synchronizedLyricsSubject.OnNext(Enumerable.Empty<LyricPhraseModel>().ToList());
                songSyncLyrics?.Clear();
                StopLyricIndexUpdateTimer();
                return;
            }
            if (songSyncLyrics?.Count > 0)
            {
                if (lastSongIDLyrics == song?.LocalDeviceId)
                {
                    if (pState == MediaPlayerState.Playing)
                    {
                        StartLyricIndexUpdateTimer();
                        return;
                    }
                    if (songSyncLyrics?.Count < 1)
                    {
                        StopLyricIndexUpdateTimer();
                    }
                }
            }
            songSyncLyrics?.Clear();
            songSyncLyrics = LoadSynchronizedAndSortedLyrics(song!.FilePath!, _synchronizedLyricsSubject.Value);

            if (songSyncLyrics?.Count < 1)
            {
                //_currentLyricSubject.OnNext(new LyricPhraseModel());
                _synchronizedLyricsSubject.OnNext(Enumerable.Empty<LyricPhraseModel>().ToList());
                song.HasLyrics = false;
                StopLyricIndexUpdateTimer();
                return;
            }
            else if (PlayBackService.CurrentQueue != 2 && pState == MediaPlayerState.Playing)
            {
                if (song.HasLyrics != true || song.HasSyncedLyrics != true)
                {
                    song.HasLyrics = true;
                    song.HasSyncedLyrics = true;
                }
            }
            lastSongIDLyrics = song.LocalDeviceId!;
            //Debug.WriteLine($"Loaded song lyrics {");
            song.HasLyrics = true;
            _synchronizedLyricsSubject.OnNext(songSyncLyrics?.Count < 1 ? Enumerable.Empty<LyricPhraseModel>().ToList() : songSyncLyrics);
            StartLyricIndexUpdateTimer();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error when Loading Lyrics", ex.Message, "OK");
        }
    }

    List<LyricPhraseModel>? sortedLyrics;
    public IList<LyricPhraseModel> GetSpecificSongLyrics(SongModelView song)
    {
        if (song is null)
        {
            return Enumerable.Empty<LyricPhraseModel>().ToList();
        }
        List<LyricPhraseModel> lyr = LoadLyrFromFile(song.FilePath);
        return lyr;
    }
    private List<LyricPhraseModel> LoadSynchronizedAndSortedLyrics(string songPath, IList<LyricPhraseModel>? syncedLyrics = null)
    {
        if (syncedLyrics is not null && songSyncLyrics?.Count > 0)
        {
            hasSyncedLyrics = true;
            //sortedLyrics = syncedLyrics
            sortedLyrics = syncedLyrics.ToList();
            return sortedLyrics;
        }
        List<LyricPhraseModel> lyrr = LoadLyrFromFile(songPath);
        if (lyrr.Count <= 0)
        {
            StopLyricIndexUpdateTimer();
            return Enumerable.Empty<LyricPhraseModel>().ToList();
        }

        //string songFileNameWithoutExtension = Path.GetFileNameWithoutExtension(songPath)!;
        //string lrcExtension = ".lrc";
        ////string txtExtension = ".txt";
        //string lrcFilePath;

        //if (!File.Exists(Path.ChangeExtension(songPath, lrcExtension)))
        //{
        //    hasSyncedLyrics = false;
        //    //lrcFilePath = Path.ChangeExtension(songPath,txtExtension);
        //    StopLyricIndexUpdateTimer();
        //    return Enumerable.Empty<LyricPhraseModel>().ToList();
        //}
        //else
        //{
        //    hasSyncedLyrics = true;
        //    lrcFilePath = Path.ChangeExtension(songPath, lrcExtension)!;
        //    string[] lines = File.ReadAllLines(lrcFilePath);
        //    lyrr = StringToLyricPhraseModel(lines);

        //    sortedLyrics = lyrr;
        //    if (sortedLyrics.Count > 1)
        //    {
        //        sortedLyrics.Sort((x, y) => x.TimeStampMs.CompareTo(y.TimeStampMs));
        //        //List.Sort(sortedLyrics, (x, y) => x.TimeStampMs.CompareTo(y.TimeStampMs));
        //    }
        //}

        return sortedLyrics.ToList();//lyricPhrases;
    }

    private List<LyricPhraseModel> LoadLyrFromFile(string? songPath)
    {
        var lyrr = new List<LyricPhraseModel>();
        if (string.IsNullOrEmpty(songPath) || !File.Exists(songPath))
        {
            return Enumerable.Empty<LyricPhraseModel>().ToList();
        }


        // If no synchronized lyrics are found, look for a .lrc file
        string lrcPath = Path.ChangeExtension(songPath, ".lrc");
        if (File.Exists(lrcPath))
        {
            var lrcLines = File.ReadAllLines(lrcPath);
            var parsedLyrics = new List<LyricPhraseModel>();

            foreach (var line in lrcLines)
            {
                // Parse each line in .lrc format
                var match = Regex.Match(line, @"\[(\d{2}):(\d{2})\.(\d{2,3})\](.*)");
                if (match.Success)
                {
                    int minutes = int.Parse(match.Groups[1].Value);
                    int seconds = int.Parse(match.Groups[2].Value);
                    int milliseconds = int.Parse(match.Groups[3].Value.PadRight(3, '0')); // Ensure ms has 3 digits
                    string text = match.Groups[4].Value.Trim();

                    var timestampMs = (minutes * 60 * 1000) + (seconds * 1000) + milliseconds;

                    parsedLyrics.Add(new LyricPhraseModel
                    {
                        TimeStampMs = timestampMs,
                        TimeStampText = $"[{minutes:D2}:{seconds:D2}.{milliseconds:D3}]",
                        Text = text
                    });
                }
            }

            sortedLyrics = parsedLyrics.OrderBy(l => l.TimeStampMs).ToList(); // Ensure lyrics are sorted
            return sortedLyrics;
        }

        // Try to get synchronized lyrics using Track
        IList<LyricsPhrase>? lyrics = new Track(songPath).Lyrics.SynchronizedLyrics;

        if (lyrics != null && lyrics.Count > 0)
        {
            hasSyncedLyrics = true;
            sortedLyrics = lyrics.Select(phrase => new LyricPhraseModel(phrase)).ToList();
            return sortedLyrics;
        }
        return Enumerable.Empty<LyricPhraseModel>().ToList(); // Return empty if no lyrics are found
    }

    public static ObservableCollection<LyricPhraseModel> LoadSynchronizedAndSortedLyrics(string songPath)
    {
       
        List<LyricPhraseModel> lyrr = new();

        IList<LyricsPhrase>? lyrics = new Track(songPath).Lyrics.SynchronizedLyrics;

        if (lyrics is not null && lyrics!.Count != 0)
        {
            return Enumerable.Empty<LyricPhraseModel>().ToObservableCollection();
        }
        string songFileNameWithoutExtension = Path.GetFileNameWithoutExtension(songPath);
        string lrcExtension = ".lrc";
        //string txtExtension = ".txt";
        string lrcFilePath;

        if (!File.Exists(Path.ChangeExtension(songPath, lrcExtension)))
        {
            return Enumerable.Empty<LyricPhraseModel>().ToObservableCollection();
        }
        else
        {
            
            lrcFilePath = Path.ChangeExtension(songPath!, lrcExtension);
            string[] lines = File.ReadAllLines(lrcFilePath);
            lyrr = StringToLyricPhraseModel(lines);

            if (lyrr.Count > 1)
            {
                lyrr.Sort((x, y) => x.TimeStampMs.CompareTo(y.TimeStampMs));
                //List.Sort(sortedLyrics, (x, y) => x.TimeStampMs.CompareTo(y.TimeStampMs));
            }
        }

        return lyrr.ToObservableCollection();//lyricPhrases;
    }

    static List<LyricPhraseModel> StringToLyricPhraseModel(string[] lines)
    {
        List<LyricPhraseModel> lyricPhrases = new();
        foreach (string line in lines)
        {
            // Regular expression to match lines with timestamps and lyrics.
            Match match = Regex.Match(line, @"\[(\d{2}):(\d{2})\.(\d{2,3})\](.*)");
            if (match.Success)
            {
                int minutes = int.Parse(match.Groups[1].Value);
                int seconds = int.Parse(match.Groups[2].Value);
                int hundredths = int.Parse(match.Groups[3].Value);
                string lyricText = match.Groups[4].Value.Trim();

                // Convert timestamp to milliseconds.
                int timeStampMs = (minutes * 60 + seconds) * 1000 + hundredths * 10;

                LyricPhraseModel phrase = new(new LyricsPhrase(timeStampMs, lyricText));
                lyricPhrases.Add(phrase);
            }
        }
        return lyricPhrases;
    }
    List<LyricPhraseModel> songSyncLyrics;
    string lastSongIDLyrics;
    public void InitializeLyrics(string synclyrics)
    {
        if (string.IsNullOrEmpty(synclyrics))
        {
            _synchronizedLyricsSubject.OnNext(Enumerable.Empty<LyricPhraseModel>().ToList());
            return;
        }
        hasSyncedLyrics = true;
        songSyncLyrics?.Clear();
        var lines = synclyrics.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        //StartLyricIndexUpdateTimer();
        songSyncLyrics = StringToLyricPhraseModel(lines);
        sortedLyrics = songSyncLyrics;
        _synchronizedLyricsSubject.OnNext(songSyncLyrics);
        if (PlayBackService.CurrentlyPlayingSong is not null && PlayBackService.CurrentlyPlayingSong.IsPlaying)
        {
                StartLyricIndexUpdateTimer();
         
        }
    }

#endregion

    #region Manage Sync and timer
    public void StartLyricIndexUpdateTimer()
    {
        if (pState != MediaPlayerState.Playing)
        {
            StopLyricIndexUpdateTimer();
            return;
        }

        StopLyricIndexUpdateTimer();
        var sampleTime = 780;
        _lyricUpdateSubscription = PlayBackService.CurrentPosition
            .Sample(TimeSpan.FromMilliseconds(sampleTime))
            .Subscribe (async position =>
            {

                double currentTimeinsSecs = position.CurrentTimeInSeconds;
                await UpdateCurrentLyricIndex(currentTimeinsSecs);
                
            }, error => { Debug.WriteLine($"Error in subscription: {error.Message}"); });
    }

    public async Task UpdateCurrentLyricIndex(double currentPositionInSeconds)
    {
        var lyrics = _synchronizedLyricsSubject.Value;
        if (lyrics is null || lyrics?.Count < 1)
        {
            if (!hasSyncedLyrics)
            {
                StopLyricIndexUpdateTimer();
                return;
            }

            await LoadLyrics(PlayBackService.CurrentlyPlayingSong);
            if (hasSyncedLyrics)
            {
                _synchronizedLyricsSubject.OnNext(songSyncLyrics);
            }
        }

        double currentPositionInMs = currentPositionInSeconds * 1000;
        int offsetValue = 1050;
        var highlightedLyric = FindClosestLyric(currentPositionInMs + offsetValue);

        if (highlightedLyric == null)
        {
            return;
        }
        if (!Equals(_currentLyricSubject.Value, highlightedLyric))
        {
            _currentLyricSubject.OnNext(highlightedLyric);
        }

    }
    LyricPhraseModel FindClosestLyric(double currentPositionInMs)
    {
        try
        {
            if (sortedLyrics == null || sortedLyrics.Count == 0)
            {
                if (_synchronizedLyricsSubject?.Value?.Count > 0)
                {
                    sortedLyrics = _synchronizedLyricsSubject.Value.OrderBy(l => l.TimeStampMs).ToList();
                }
                else
                {
                    StopLyricIndexUpdateTimer();
                    return null;
                }
            }

            int index = sortedLyrics.BinarySearch(
                new LyricPhraseModel { TimeStampMs = (int)currentPositionInMs },
                new LyricTimestampComparer()
            );

            if (index >= 0)
            {
                // Exact match found
                return sortedLyrics[index];
            }
            else
            {
                // No exact match, get the previous lyric
                index = ~index; // Bitwise complement to get insertion point
                return index > 0 ? sortedLyrics[index - 1] : sortedLyrics[0];
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error when finding closest lyric: {ex.Message}");
            return null;
        }
    }

    // Comparer for BinarySearch
    class LyricTimestampComparer : IComparer<LyricPhraseModel>
    {
        public int Compare(LyricPhraseModel x, LyricPhraseModel y)
        {
            return x.TimeStampMs.CompareTo(y.TimeStampMs);
        }
    }

    public void StopLyricIndexUpdateTimer()
    {
        _lyricUpdateSubscription?.Dispose();
        _lyricUpdateSubscription = null;
    }
    #endregion

    #region Fetch Lyrics Online from Lrclib
    public async Task<(bool IsFetchSuccessful, Content[]? contentData)> FetchLyricsOnlineLrcLib(SongModelView song, bool useManualSearch = false, List<string>? manualSearchFields = null)
    {
        HttpClient client = new();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Dimmer v1.4.0 (https://github.com/YBTopaz8/Dimmer-MAUI)");

        try
        {
            Content[]? lyricsData = null;

            if (useManualSearch)
            {
                lyricsData = await SearchLyricsByTitleAndArtistNameToLrc(song, client, manualSearchFields);
            }
            else
            {

                lyricsData = await SearchLyricsByTitleAndArtistNameToLrc(song, client);
            }

            if (lyricsData is null || lyricsData.Length < 1)
            {
                return (false, Array.Empty<Content>());
            }

            //var potentialImages = await FetchLyricsOnlineLyrist(song.Title!, song.ArtistName!); ;
            //for (int i = 0; i < potentialImages.contentData?.Length; i++)
            //{
            //    lyricsData[i].LinkToCoverImage = potentialImages.contentData[i].LinkToCoverImage;
            //}
            return (true, lyricsData);
        }
        catch (HttpRequestException e)
        {
            Debug.WriteLine($"Request error: {e.Message}");
            return (false, Array.Empty<Content>());
        }
        catch (Exception e)
        {
            Debug.WriteLine($"Unexpected error: {e.Message}");
            return (false, Array.Empty<Content>());
        }
        finally
        {
            client.Dispose();
        }
    }

    async Task<Content[]?> SearchLyricsByTitleAndArtistNameToLrc(SongModelView song, HttpClient client, List<string>? manualSearchFields = null)
    {
        string url;

        // Construct the URL with query parameters
        string artistName = manualSearchFields[1].Split(',')[0].Trim();
        artistName = Uri.EscapeDataString(artistName);
        string trackName = Uri.EscapeDataString(manualSearchFields[2]);
        
        
                 
        url = $"https://lrclib.net/api/search?artist_name={artistName}&track_name={trackName}";
        
        // Send the GET request
        HttpResponseMessage response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode(); // Throw if not a success code

        // Read the response content
        string content = await response.Content.ReadAsStringAsync();
        if (content.Equals("[]") || string.IsNullOrEmpty(content))
        {
            if (manualSearchFields is null)
            {
                return JsonSerializer.Deserialize<Content[]>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
          
            // Read the response content
            content = await response.Content.ReadAsStringAsync();
        }

        return JsonSerializer.Deserialize<Content[]>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive =true});
    }

    #endregion
    #region Fetch Lyrics Online from Lyrist

    public async Task<string> FetchAndDownloadCoverImage(string songTitle, string songArtistName, string albumName, SongModelView? song =null)
    {
        var stringAlbumName = albumName;
        var stringSongTitle = songTitle;
        var stringArtistName = songArtistName;
        var localCopyOfSong = song; 
        try
        {
            if (localCopyOfSong is not null)
            {
                stringSongTitle = localCopyOfSong.Title;
                stringArtistName = localCopyOfSong.ArtistName;
                stringAlbumName = localCopyOfSong.AlbumName;
                if (localCopyOfSong.CoverImagePath == "NC")
                {
                    localCopyOfSong.CoverImagePath = "musicnoteslider.png";
                }
                localCopyOfSong.CoverImagePath = SaveOrGetCoverImageToFilePath(localCopyOfSong.FilePath);
                if (!string.IsNullOrEmpty(localCopyOfSong.CoverImagePath))
                {
                    if (File.Exists(localCopyOfSong.CoverImagePath))
                    {                        
                        SongsManagementService.UpdateSongDetails(localCopyOfSong);
                        return localCopyOfSong.CoverImagePath;
                    }                        
                }

            }
            //byte[]? ImageBytes = null;
            //(_, Content[]? apiResponse) = await FetchLyricsOnlineLyrist(stringSongTitle, songArtistName);
            //if (apiResponse is null || apiResponse.Length < 1)
            //{
            //    return string.Empty;
            //}
            //if (!string.IsNullOrEmpty(apiResponse[0]?.LinkToCoverImage))
            //{
            //    ImageBytes = await DownloadSongImage(apiResponse[0]?.LinkToCoverImage!)!;
            //    song.CoverImagePath = SaveOrGetCoverImageToFilePath(song.FilePath!, ImageBytes);
            //    SongsManagementService.UpdateSongDetails(song);
            //}

            return string.IsNullOrEmpty(song.CoverImagePath) ? "NC" : song.CoverImagePath;
        
        }
        catch (HttpRequestException e)
        {
            Debug.WriteLine($"Request error: {e.Message}");
            return string.Empty;
        }
        catch (Exception e)
        {
            Debug.WriteLine($"Unexpected error: {e.Message}");
            return string.Empty;
        }
    }
    public static string SaveOrGetCoverImageToFilePath(string fullfilePath, byte[]? imageData = null, bool isDoubleCheckingBeforeFetch = true)
    {
        if (imageData is null)
        {
            Track song = new Track(fullfilePath);
            string? mimeType = song.EmbeddedPictures?.FirstOrDefault()?.MimeType;
            if (mimeType == "image/jpg" || mimeType == "image/jpeg" || mimeType == "image/png")
            {
                imageData = song.EmbeddedPictures?.FirstOrDefault()?.PictureData;
            }
        }

        string fileNameWithExtension = Path.GetFileName(fullfilePath);

        string sanitizedFileName = string.Join("_", fileNameWithExtension.Split(Path.GetInvalidFileNameChars()));

        //TODO: SET THIS AS PREFERENCE FOR USERS
#if ANDROID && NET9_0
        string folderPath = Path.Combine(FileSystem.AppDataDirectory, "CoverImagesDimmer"); // Use AppDataDirectory for Android compatibility
#elif WINDOWS && NET9_0
        string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments), "DimmerDB", "CoverImagesDimmer");
#endif
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }
        string filePath = Path.Combine(folderPath, $"{sanitizedFileName}.png");
        string filePathjpg = Path.Combine(folderPath, $"{sanitizedFileName}.jpg");


        if (isDoubleCheckingBeforeFetch)
        {
            if (File.Exists(filePath))
            {
                return filePath;
            }
            if (File.Exists(filePathjpg))
            {
                return filePathjpg;
            }
        }
        if (imageData is null)
        {
            return string.Empty;
        }

        try
        {
            File.WriteAllBytes(filePath, imageData);
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Error saving file: " + ex.Message);
        }

        return filePath;
    }

#endregion

    public static (bool,ObservableCollection<LyricPhraseModel>?) HasLyrics(SongModelView song)
    {
        if (song is null)
        {
            return (false, null);
        }

        var track = new Track(song.FilePath);
        if (track.Lyrics.SynchronizedLyrics is null || track.Lyrics.SynchronizedLyrics.Count < 1)
        {
            return (false, null);
        }

        return (true, track.Lyrics.SynchronizedLyrics.Select(phrase => new LyricPhraseModel(phrase)).ToObservableCollection());
    }

    public bool WriteLyricsToLyricsFile(string Lyrics, SongModelView songObj, bool IsSynched)
    {
        if (Lyrics is null)
        {            
            return false;
        }
        if (!IsSynched)
        {
            songObj.UnSyncLyrics = Lyrics;
            songObj.UnSyncLyrics = string.Empty;
        }
        else
        {
            var track = new Track(songObj.FilePath);
            track.Lyrics.ParseLRC(Lyrics);
            songObj.HasSyncedLyrics = IsSynched;

            //track.Save();
            
            songObj.SyncLyrics = new ObservableCollection<LyricPhraseModel>(
                track.Lyrics.SynchronizedLyrics.Select(phrase => new LyricPhraseModel(phrase))
            );

        }
        songObj.HasLyrics = IsSynched;
        
        if (PlayBackService.CurrentQueue != 2)
        {
             SongsManagementService.UpdateSongDetails(songObj);
        }
        string songDirectory = Path.GetDirectoryName(songObj.FilePath!)!;
        string songFileNameWithoutExtension = Path.GetFileNameWithoutExtension(songObj.FilePath);
        string fileExtension = IsSynched ? ".lrc" : ".txt";
        string lrcFilePath;
        
        if (!Directory.Exists(songDirectory))
        {
            Directory.CreateDirectory(songDirectory);
        }
        lrcFilePath = Path.Combine(songDirectory, songFileNameWithoutExtension + fileExtension);
        if (File.Exists(lrcFilePath))
        {
            File.Delete(lrcFilePath);
        }

        File.WriteAllText(lrcFilePath, Lyrics); //I had a case one time where an exception was thrown because there was a folder with exact same name alreay existing
        
        return true;

    }

    public Task<(bool IsFetchSuccessful, Content[] contentData)> FetchLyricsOnlineLyrist(SongModelView songs, bool useManualSearch = false, List<string>? manualSearchFields = null)
    {
        throw new NotImplementedException();
    }

}
