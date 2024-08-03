using System.Text.Json;
using System.Text.RegularExpressions;


namespace Dimmer.Utilities.Services;
public class LyricsService : ILyricsService
{

    private BehaviorSubject<IList<LyricPhraseModel>> _synchronizedLyricsSubject = new([]);
    public IObservable<IList<LyricPhraseModel>> SynchronizedLyricsStream => _synchronizedLyricsSubject.AsObservable();

    private BehaviorSubject<LyricPhraseModel> _currentLyricSubject = new BehaviorSubject<LyricPhraseModel>(new LyricPhraseModel(null));
    public IObservable<LyricPhraseModel> CurrentLyricStream => _currentLyricSubject.AsObservable();

    private BehaviorSubject<string> _unsyncedLyricsSubject = new("");

    private IDisposable _lyricUpdateSubscription;
    public IObservable<string> UnSynchedLyricsStream => _unsyncedLyricsSubject.AsObservable();

    public IPlayBackService PlayBackService { get; }
    public ISongsManagementService SongsManagementService { get; }

    bool hasLyrics;
    public LyricsService(IPlayBackService songsManagerService, ISongsManagementService songsManagementService)
    {
        PlayBackService = songsManagerService;
        SongsManagementService = songsManagementService;
        SubscribeToPlayerStateChanges();
    }

    private void SubscribeToPlayerStateChanges()
    {
        PlayBackService.PlayerState.Subscribe(state =>
        {
            switch (state)
            {
                case MediaPlayerState.Initialized:
                    LoadLyrics(PlayBackService.CurrentlyPlayingSong);
                    Debug.WriteLine("Initialized");
                    break;
                case MediaPlayerState.Playing:
                    LoadLyrics(PlayBackService.CurrentlyPlayingSong);
                    if (hasLyrics)
                    {
                        StartLyricIndexUpdateTimer();
                    }
                    else
                    {
                        _currentLyricSubject.OnNext(null);
                    }
                    break;
                case MediaPlayerState.Paused:
                    StopLyricIndexUpdateTimer();
                    break;
                case MediaPlayerState.Stopped:
                    StopLyricIndexUpdateTimer();
                    break;
                case MediaPlayerState.LyricsLoad:
                    LoadLyrics(PlayBackService.CurrentlyPlayingSong);
                    break;
                default:
                    break;
            }

        });
    }

    #region Manage Loadings and Initializations
    public void LoadLyrics(SongsModelView song)
    {
        if (song is null)
        {
            return;
        }
        if (lastSongIDLyrics == song?.Id && songSyncLyrics?.Count > 1)
        {
            return;
        }

        songSyncLyrics = LoadSynchronizedAndSortedLyrics(song.FilePath);
        if (songSyncLyrics?.Count > 1)
        {
            song.HasLyrics = true;
            SongsManagementService.UpdateSongDetails(song);
        }
        lastSongIDLyrics = song.Id;
        //Debug.WriteLine($"Loaded song lyrics {");
        _synchronizedLyricsSubject.OnNext(songSyncLyrics?.Count < 1 ? Enumerable.Empty<LyricPhraseModel>().ToList() : songSyncLyrics);
    }

    private static string LoadUnsyncedLyrics(string songPath)
    {
        var UnSyncedLyrics = new Track(songPath).Lyrics.UnsynchronizedLyrics;
        if (UnSyncedLyrics is not null)
        {
            return UnSyncedLyrics;
        }
        else
        {
            return "No Lyrics Found";
        }
    }

    List<LyricPhraseModel>? sortedLyrics;
    private List<LyricPhraseModel> LoadSynchronizedAndSortedLyrics(string songPath)
    {
        
        List<LyricPhraseModel> lyrr = new();
        
        IList<LyricsPhrase>? lyrics = new Track(songPath).Lyrics.SynchronizedLyrics;

        if (lyrics is not null && lyrics!.Count != 0)
        {
            hasLyrics = true;
            sortedLyrics = lyrics.Select(phrase => new LyricPhraseModel(phrase)).ToList();
            if (sortedLyrics?.Count > 1)
            {
                sortedLyrics.Sort((x, y) => x.TimeStampMs.CompareTo(y.TimeStampMs));
            }
            return sortedLyrics;
        }

        string lrcFilePath = Path.ChangeExtension(songPath, ".lrc");

        if (!File.Exists(lrcFilePath))
        {
            hasLyrics = false;
            return Enumerable.Empty<LyricPhraseModel>().ToList();
        }
        else
        {
            hasLyrics = true;
            string[] lines = File.ReadAllLines(lrcFilePath);
            lyrr = StringToLyricPhraseModel(lines);
            
        }
        sortedLyrics = lyrr;
        if (sortedLyrics.Count > 1)
        {
            sortedLyrics.Sort((x, y) => x.TimeStampMs.CompareTo(y.TimeStampMs));
            //List.Sort(sortedLyrics, (x, y) => x.TimeStampMs.CompareTo(y.TimeStampMs));
        }
        return sortedLyrics.ToList();//lyricPhrases;
    }
    
    List<LyricPhraseModel> songSyncLyrics;
    ObjectId lastSongIDLyrics;
    public void InitializeLyrics(string synclyrics)
    {
        
        if (string.IsNullOrEmpty(synclyrics))
        {
            _synchronizedLyricsSubject.OnNext(null);
            return;
        }
        hasLyrics = true;   
        List<string> rr = new();
        var lines = synclyrics.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        StartLyricIndexUpdateTimer();
        songSyncLyrics = StringToLyricPhraseModel(lines);
        _synchronizedLyricsSubject.OnNext(songSyncLyrics);
    }

    private static List<LyricPhraseModel> StringToLyricPhraseModel(string[] lines)
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
#endregion

    #region Manage Sync and timer
    public void StartLyricIndexUpdateTimer()
    {
        var sampleTime = 1000;

        _lyricUpdateSubscription = PlayBackService.CurrentPosition
            .Sample(TimeSpan.FromMilliseconds(sampleTime))
            .Subscribe(position =>
            {
                double currentTimeinsSecs = position.CurrentTimeInSeconds;
                UpdateCurrentLyricIndex(currentTimeinsSecs);
            }, error => { Debug.WriteLine($"Error in subscription: {error.Message}"); });
    }


    public void UpdateCurrentLyricIndex(double currentPositionInSeconds)
    {
        var lyrics = _synchronizedLyricsSubject.Value;
        if (lyrics?.Count < 1)
        {
            LoadLyrics(PlayBackService.CurrentlyPlayingSong);
            if (hasLyrics)
            {                    
                _synchronizedLyricsSubject.OnNext(songSyncLyrics);
            }
            return;
        }

        double currentPositionInMs = currentPositionInSeconds * 1000;
        int offsetValue = 1450;
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

    public LyricPhraseModel FindClosestLyric(double currentPositionInMs)
    {

        try
        {
            if (sortedLyrics == null || sortedLyrics.Count == 0)
            {
                return null;
            }

            // Iterate through the lyrics until we find the first one with a timestamp greater than the current position
            for (int i = 0; i < sortedLyrics.Count; i++)
            {
                if (sortedLyrics[i].TimeStampMs > currentPositionInMs)
                {
                    // Return the previous lyric if it exists, otherwise return the first lyric
                    return i > 0 ? sortedLyrics[i - 1] : sortedLyrics[0];
                }
            }

            // If we didn't find any lyric with a timestamp greater than the current position, return the last lyric
            return sortedLyrics[sortedLyrics.Count - 1];
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error when finding closest lyric: {ex.Message}");
            return null;
        }
    }

    public void StopLyricIndexUpdateTimer()
    {
        _lyricUpdateSubscription?.Dispose();
    }
#endregion

    #region Fetch Lyrics Online from Lrclib
    public async Task<(bool IsFetchSuccessul, Content[] contentData)> FetchLyricsOnlineLrcLib(SongsModelView song)
    {
        HttpClient client = HttpClientSingleton.Instance;
        try
        {
            Content[]? lyricsData = null;
            lyricsData = await SearchLyricsByTitleOnly(song, client);
            
            if (lyricsData is null || lyricsData.Length < 1)
            {
                return (false, Array.Empty<Content>());
            }
            WriteLyricsToLrcFile(lyricsData[0].syncedLyrics, song);


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
            client.Dispose(); // Dispose of HttpClient to free resources
        }
    }

    async Task<Content[]> SearchLyricsByTitleOnly(SongsModelView song, HttpClient client)
    {
        // Construct the URL with query parameters
        string artistName = Uri.EscapeDataString(song.ArtistName);
        string trackName = Uri.EscapeDataString(song.Title);
        string url = $"https://lrclib.net/api/search?track_name={trackName}";

        // Send the GET request
        HttpResponseMessage response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode(); // Throw if not a success code

        // Read the response content
        string content = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<Content[]>(content);
    }

    async Task<Content[]> SearchLyricsByTitleAndArtistName(SongsModelView song, HttpClient client)
    {
        // Construct the URL with query parameters
        string artistName = Uri.EscapeDataString(song.ArtistName);
        string trackName = Uri.EscapeDataString(song.Title);
        string url = $"https://lrclib.net/api/search?artist_name={artistName}&track_name={trackName}";

        // Send the GET request
        HttpResponseMessage response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode(); // Throw if not a success code

        // Read the response content
        string content = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<Content[]>(content);
    }
    public bool WriteLyricsToLrcFile(string syncedLyrics, SongsModelView songObj)
    {
        //string processedLyrics = syncedLyrics.Replace("\\n", Environment.NewLine);
        string songDirectory = Path.GetDirectoryName(songObj.FilePath);
        string songFileNameWithoutExtension = Path.GetFileNameWithoutExtension(songObj.FilePath);
        string lrcFilePath = Path.Combine(songDirectory, songFileNameWithoutExtension + ".lrc");
        if (File.Exists(lrcFilePath))
        {
            File.Delete(lrcFilePath);
        }
        File.WriteAllText(lrcFilePath, syncedLyrics);
        return true;
    }

    #endregion

    #region Fetch Lyrics Online from Lyrics

    public async Task<(bool IsFetchSuccessul, LyristApiResponse? contentData)> FetchLyricsOnlineLyrics(SongsModelView song)
    {
        try
        {
            LyristApiResponse? contentData = null;
            contentData = await SearchLyricsByTitleOnlyToLyrist(song, HttpClientSingleton.Instance);
            return (true, contentData);
        }
        catch (HttpRequestException e)
        {
            Debug.WriteLine($"Request error: {e.Message}");
            return (false, null);
        }
        catch (Exception e)
        {
            Debug.WriteLine($"Unexpected error: {e.Message}");
            return (false, null);
        }
    }

    private async Task<LyristApiResponse?> SearchLyricsByTitleOnlyToLyrist(SongsModelView song, HttpClient client)
    {

        string trackName = Uri.EscapeDataString(song.Title);
        string url = $"https://lyrist.vercel.app/api/{trackName}/{song.ArtistName}";

        // Send the GET request
        HttpResponseMessage response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode(); // Throw if not a success code

        // Read the response content
        string content = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<LyristApiResponse>(content);

    }

    async Task<byte[]> DownloadSongImage(string coverImageURL, HttpClient client)
    {
        // Send the GET request
        HttpResponseMessage response = await client.GetAsync(coverImageURL);
        response.EnsureSuccessStatusCode(); // Throw if not a success code

        // Read the response content
        byte[] ImageBytes = await response.Content.ReadAsByteArrayAsync();

        return ImageBytes;
    }

    public async Task FetchAndDownloadCoverImage(SongsModelView songs)
    {
        if (!string.IsNullOrEmpty(SaveCoverImageToFile(songs.FilePath)))
        {
            songs.CoverImagePath = SaveCoverImageToFile(songs.FilePath);
        }
        HttpClient client = new();
        byte[]? ImageBytes = null;
        (_, LyristApiResponse? apiResponse)= await FetchLyricsOnlineLyrics(songs);
        if (apiResponse is null)
        {
            return;
        }
        if (!string.IsNullOrEmpty(apiResponse?.image))
        {
            ImageBytes = await DownloadSongImage(apiResponse.image, client);
            songs.CoverImagePath = SaveCoverImageToFile(songs.FilePath, ImageBytes);
            SongsManagementService.UpdateSongDetails(songs);
        }
    }
    static string SaveCoverImageToFile(string fullfilePath, byte[]? imageData = null)
    {
        if (imageData is null)
        {
            return string.Empty;
        }

        // Extract the file name from the full path
        string fileNameWithExtension = Path.GetFileName(fullfilePath);

        // Sanitize the file name
        string sanitizedFileName = string.Join("_", fileNameWithExtension.Split(Path.GetInvalidFileNameChars()));

        // Define the folder path
        string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DimmerDB", "CoverImagesDimmer");

        // Ensure the directory exists
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }
        string filePath = Path.Combine(folderPath, $"{sanitizedFileName}.png");
        string filePathjpg = Path.Combine(folderPath, $"{sanitizedFileName}.jpg");

        if (File.Exists(filePath))
        {
            return filePath;
        }
        if (File.Exists(filePathjpg))
        {
            return filePathjpg;
        }

        // Write the image data to the file
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
}
