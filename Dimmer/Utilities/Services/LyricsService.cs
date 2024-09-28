using System.Text.Json;
using System.Text.RegularExpressions;

namespace Dimmer_MAUI.Utilities.Services;
public class LyricsService : ILyricsService
{

    private BehaviorSubject<IList<LyricPhraseModel>> _synchronizedLyricsSubject = new([]);
    public IObservable<IList<LyricPhraseModel>> SynchronizedLyricsStream => _synchronizedLyricsSubject.AsObservable();

    private BehaviorSubject<LyricPhraseModel> _currentLyricSubject = new BehaviorSubject<LyricPhraseModel>(new LyricPhraseModel(null));
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
                    LoadLyrics(PlayBackService.CurrentlyPlayingSong);
                    break;
                case MediaPlayerState.Playing:
                    pState = MediaPlayerState.Playing;
                    LoadLyrics(PlayBackService.CurrentlyPlayingSong);
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
                    LoadLyrics(PlayBackService.CurrentlyPlayingSong);
                    break;
                case MediaPlayerState.CoverImageDownload:
                    pState = MediaPlayerState.CoverImageDownload;
                    await FetchAndDownloadCoverImage(PlayBackService.CurrentlyPlayingSong);
                    break;
                default:
                    break;
            }

        });
    }

    #region Manage Loadings and Initializations
    public async void LoadLyrics(SongsModelView song)
    {
        try
        {            
            if (song is null)
            {
                _synchronizedLyricsSubject.OnNext(null);
                songSyncLyrics?.Clear();
                StopLyricIndexUpdateTimer();
                return;
            }
            if (songSyncLyrics?.Count > 0)
            {
                if (lastSongIDLyrics == song?.Id)
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
            songSyncLyrics = LoadSynchronizedAndSortedLyrics(song.FilePath, _synchronizedLyricsSubject.Value);

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
            lastSongIDLyrics = song.Id;
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
    private List<LyricPhraseModel> LoadSynchronizedAndSortedLyrics(string? songPath = null, IList<LyricPhraseModel>? syncedLyrics = null)
    {
        if (syncedLyrics is not null && songSyncLyrics?.Count > 0)
        {
            hasSyncedLyrics = true;
            //sortedLyrics = syncedLyrics
            sortedLyrics = syncedLyrics.ToList();
            return sortedLyrics;
        }
        List<LyricPhraseModel> lyrr = new();

        IList<LyricsPhrase>? lyrics = new Track(songPath).Lyrics.SynchronizedLyrics;

        if (lyrics is not null && lyrics!.Count != 0)
        {
            hasSyncedLyrics = true;
            sortedLyrics = lyrics.Select(phrase => new LyricPhraseModel(phrase)).ToList();
            if (sortedLyrics?.Count > 1)
            {
                sortedLyrics.Sort((x, y) => x.TimeStampMs.CompareTo(y.TimeStampMs));
            }
            return sortedLyrics;
        }
        string songFileNameWithoutExtension = Path.GetFileNameWithoutExtension(songPath);
        string lrcExtension = ".lrc";
        string txtExtension = ".txt";
        string lrcFilePath;

        if (!File.Exists(Path.ChangeExtension(songPath, lrcExtension)))
        {
            hasSyncedLyrics = false;
            //lrcFilePath = Path.ChangeExtension(songPath,txtExtension);
            StopLyricIndexUpdateTimer();
            return Enumerable.Empty<LyricPhraseModel>().ToList();
        }
        else
        {
            hasSyncedLyrics = true;
            lrcFilePath = Path.ChangeExtension(songPath, lrcExtension);
            string[] lines = File.ReadAllLines(lrcFilePath);
            lyrr = StringToLyricPhraseModel(lines);

            sortedLyrics = lyrr;
            if (sortedLyrics.Count > 1)
            {
                sortedLyrics.Sort((x, y) => x.TimeStampMs.CompareTo(y.TimeStampMs));
                //List.Sort(sortedLyrics, (x, y) => x.TimeStampMs.CompareTo(y.TimeStampMs));
            }
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
        hasSyncedLyrics = true;
        songSyncLyrics?.Clear();
        var lines = synclyrics.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        //StartLyricIndexUpdateTimer();
        songSyncLyrics = StringToLyricPhraseModel(lines);
        sortedLyrics = songSyncLyrics;
        _synchronizedLyricsSubject.OnNext(songSyncLyrics);
        if (PlayBackService.CurrentlyPlayingSong.IsPlaying)
        {
            StartLyricIndexUpdateTimer();
        }
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
        if (pState != MediaPlayerState.Playing)
        {
            StopLyricIndexUpdateTimer();
            return;
        }

        StopLyricIndexUpdateTimer();
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
        if (lyrics is null || lyrics?.Count < 1)
        {
            if (!hasSyncedLyrics)
            {
                StopLyricIndexUpdateTimer();
                return;
            }

            LoadLyrics(PlayBackService.CurrentlyPlayingSong);
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
                    sortedLyrics?.Clear();
                    sortedLyrics = _synchronizedLyricsSubject.Value.ToList();
                }
                else
                {
                    StopLyricIndexUpdateTimer();
                    return null;
                }

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
        _lyricUpdateSubscription = null;
    }
    #endregion

    #region Fetch Lyrics Online from Lrclib
    public async Task<(bool IsFetchSuccessful, Content[] contentData)> FetchLyricsOnlineLrcLib(SongsModelView song, bool useManualSearch = false, List<string>? manualSearchFields = null)
    {
        HttpClient client = new();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Dimmer v0.8 (https://github.com/YBTopaz8/Dimmer-MAUI)");

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
            //WriteLyricsToLrcFile(lyricsData[0].syncedLyrics, song);

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

    async Task<Content[]> SearchLyricsByTitleOnlyToLrc(SongsModelView song, HttpClient client)
    {
        // Construct the URL with query parameters
        string artistName = Uri.EscapeDataString(song.ArtistName);
        string trackName = Uri.EscapeDataString(song.Title);
        string url = $"https://lrclib.net/api/search?track_name={trackName}&artist_name{artistName}";

        // Send the GET request
        HttpResponseMessage response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode(); // Throw if not a success code

        // Read the response content
        string content = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<Content[]>(content);
    }

    async Task<Content[]>? SearchLyricsByTitleAndArtistNameToLrc(SongsModelView song, HttpClient client, List<string>? manualSearchFields = null)
    {
        string url;
        if (manualSearchFields is null || manualSearchFields.Count < 1)
        {
            // Construct the URL with query parameters
            string artistName = Uri.EscapeDataString(song.ArtistName);
            string trackName = Uri.EscapeDataString(song.Title);
            url = $"https://lrclib.net/api/search?artist_name={artistName}&track_name={trackName}";
        }
        else
        {
            url = $"https://lrclib.net/api/search?artist_name={Uri.EscapeDataString(manualSearchFields[1])}&track_name={Uri.EscapeDataString(manualSearchFields[0])}";
        }

        // Send the GET request
        HttpResponseMessage response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode(); // Throw if not a success code

        // Read the response content
        string content = await response.Content.ReadAsStringAsync();
        if (content.Equals("[]") || string.IsNullOrEmpty(content))
        {
            if (manualSearchFields is null)
            {
                return JsonSerializer.Deserialize<Content[]>(content);
            }
            url = $"https://lrclib.net/api/search?q={Uri.EscapeDataString(manualSearchFields[1])}";
            response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            // Read the response content
            content = await response.Content.ReadAsStringAsync();
        }

        return JsonSerializer.Deserialize<Content[]>(content);
    }

    #endregion

    #region Fetch Lyrics Online from Lyrist

    public async Task<(bool IsFetchSuccessful, Content[]? contentData)> FetchLyricsOnlineLyrist(SongsModelView song, bool useManualSearch = false, List<string>? manualSearchFields = null)
    {
        HttpClient client = new HttpClient();
        try
        {
            Content[]? contentData = null;
            contentData = await SearchLyricsByTitleAndArtistNameToLyrist(song, client);
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
        finally
        {
            client.Dispose();
        }
    }

    private async Task<Content[]> SearchLyricsByTitleAndArtistNameToLyrist(SongsModelView song, HttpClient client)
    {

        string trackName = Uri.EscapeDataString(song.Title);
        string url = $"https://lyrist.vercel.app/api/{trackName}/{song.ArtistName}";

        // Send the GET request
        HttpResponseMessage response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode(); // Throw if not a success code

        // Read the response content
        string content = await response.Content.ReadAsStringAsync();

        var e = JsonSerializer.Deserialize<LyristApiResponse>(content);
        var lyrics = new Content();
        if (string.IsNullOrEmpty(e.lyrics))
        {
            return Array.Empty<Content>();
        }
        lyrics.trackName = e.title;
        lyrics.artistName = e.artist;
        lyrics.plainLyrics = e.lyrics;
        lyrics.linkToCoverImage = e.image;
        lyrics.id = 1;
        List<Content> contentList = new();
        contentList.Add(lyrics);
        return contentList.ToArray();

    }

    static async Task<byte[]>? DownloadSongImage(string coverImageURL)//, HttpClient client)
    {
        HttpClient client = new HttpClient();
        HttpResponseMessage response = await client.GetAsync(coverImageURL);
        response.EnsureSuccessStatusCode(); // Throw if not a success code

        // Read the response content
        byte[] ImageBytes = await response.Content.ReadAsByteArrayAsync();
        client.Dispose();
        return ImageBytes;
    }

    public async Task<string> FetchAndDownloadCoverImage(SongsModelView songs)
    {
        try
        {
       
            if (!string.IsNullOrEmpty(songs.CoverImagePath = SaveOrGetCoverImageToFilePath(songs.FilePath)))
            {
                return songs.CoverImagePath;
            }
            byte[]? ImageBytes = null;
            (_, Content[]? apiResponse) = await FetchLyricsOnlineLyrist(songs);
            if (apiResponse is null || apiResponse.Length < 1)
            {
                return string.Empty;
            }
            if (!string.IsNullOrEmpty(apiResponse[0]?.linkToCoverImage))
            {
                ImageBytes = await DownloadSongImage(apiResponse[0]?.linkToCoverImage);
                songs.CoverImagePath = SaveOrGetCoverImageToFilePath(songs.FilePath, ImageBytes);
                 SongsManagementService.UpdateSongDetails(songs);
            }

            return string.IsNullOrEmpty(songs.CoverImagePath) ? string.Empty : songs.CoverImagePath;
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
    public static string SaveOrGetCoverImageToFilePath(string fullfilePath, byte[]? imageData = null, bool isDoubleCheckingBeforeFetch = false)
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
        string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DimmerDB", "CoverImagesDimmer");

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


    public bool WriteLyricsToLyricsFile(string Lyrics, SongsModelView songObj, bool IsSynched)
    {
        if (Lyrics is null)
        {            
            return false;
        }
        songObj.UnSyncLyrics = Lyrics;
        songObj.HasLyrics = !IsSynched;
        songObj.HasSyncedLyrics = IsSynched;
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

        File.WriteAllText(lrcFilePath, Lyrics);

        return true;

    }
}
