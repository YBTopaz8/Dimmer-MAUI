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

    public void LoadLyrics(SongsModelView song)
    {        
        if(songSyncLyrics?.Count > 1)
        {
            return;
        }

        IList<LyricPhraseModel> loadedLyrics = LoadSynchronizedLyrics(song.FilePath);
        if (loadedLyrics?.Count > 1)
        {
            song.HasLyrics = true;
            SongsManagementService.UpdateSongDetails(song);
        }
        //Debug.WriteLine($"Loaded song lyrics {");
        _synchronizedLyricsSubject.OnNext(loadedLyrics?.Count < 1 ? Enumerable.Empty<LyricPhraseModel>().ToList() : loadedLyrics);
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
    private List<LyricPhraseModel> LoadSynchronizedLyrics(string songPath)
    {
        
        List<LyricPhraseModel> lyrr = new();
        if(songSyncLyrics?.Count > 0)
        {
            return songSyncLyrics;
        }
        IList<LyricsPhrase>? lyrics = new Track(songPath).Lyrics.SynchronizedLyrics;

        if (lyrics is not null && lyrics!.Count != 0)
        {
            hasLyrics = true;
            var lyricss = lyrics.Select(phrase => new LyricPhraseModel(phrase)).ToList();
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
                Debug.WriteLine(songSyncLyrics[0].Text);
                Debug.WriteLine(songSyncLyrics[1].Text);
                Debug.WriteLine(songSyncLyrics[2].Text);
                    
                _synchronizedLyricsSubject.OnNext(songSyncLyrics);
            }
            Debug.WriteLineIf(lyrics is null, "Lyrics is null");
            Debug.WriteLineIf(lyrics?.Count < 1, "Lyrics Count isss 0");
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

    public async Task<Content[]> FetchLyricsOnline(SongsModelView song)
    {
        HttpClient client = new HttpClient();
        try
        {
            Content[] lyricsData = null;
            lyricsData = await SearchLyricsByTitleOnly(song, client);
            
            Debug.WriteLineIf(condition: lyricsData.Length < 1, "No Lyrics Found");
            if (lyricsData is null || lyricsData.Length < 1)
            {
                return Enumerable.Empty<Content>().ToArray();                
            }
            WriteLyricsToLrcFile(lyricsData[0].syncedLyrics, song);


            return lyricsData;
        }
        catch (HttpRequestException e)
        {
            Debug.WriteLine($"Request error: {e.Message}");
            return Enumerable.Empty<Content>().ToArray();
        }
        catch (Exception e)
        {
            Debug.WriteLine($"Unexpected error: {e.Message}");
            return Enumerable.Empty<Content>().ToArray();
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
}
