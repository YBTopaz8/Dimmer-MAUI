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
    bool hasLyrics;
    public LyricsService(IPlayBackService songsManagerService)
    {
        PlayBackService = songsManagerService;

        SubscribeToPlayerStateChanges();
    }

    private void SubscribeToPlayerStateChanges()
    {
        PlayBackService.PlayerState.Subscribe(state =>
        {
            switch (state)
            {
                case MediaPlayerState.Initialized:
                    LoadLyrics(PlayBackService.CurrentlyPlayingSong.FilePath);
                    break;
                case MediaPlayerState.Playing:
                    LoadLyrics(PlayBackService.CurrentlyPlayingSong.FilePath);
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
                default:
                    break;
            }

        });
    }

    public void LoadLyrics(string songPath)
    {
        sortedLyrics = [];
        IList<LyricPhraseModel> loadedLyrics = LoadSynchronizedLyrics(songPath);

        _synchronizedLyricsSubject.OnNext(loadedLyrics.Count < 1 ? Enumerable.Empty<LyricPhraseModel>().ToList() : loadedLyrics);
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

    LyricPhraseModel[]? sortedLyrics;
    private LyricPhraseModel[] LoadSynchronizedLyrics(string songPath)
    {
        IList<LyricsPhrase>? lyrics = new Track(songPath).Lyrics.SynchronizedLyrics;

        if (lyrics is not null && lyrics!.Count != 0)
        {
            hasLyrics = true;
            var lyricss = lyrics.Select(phrase => new LyricPhraseModel(phrase)).ToList();
            sortedLyrics = lyricss.ToArray();
            if (sortedLyrics.Length > 1)
            {
                Array.Sort(sortedLyrics, (x, y) => x.TimeStampMs.CompareTo(y.TimeStampMs));
            }
            return sortedLyrics;
        }

        string lrcFilePath = Path.ChangeExtension(songPath, ".lrc");
        List<LyricPhraseModel> lyricPhrases = new List<LyricPhraseModel>();

        if (!File.Exists(lrcFilePath))
        {
            hasLyrics = false;
            return Enumerable.Empty<LyricPhraseModel>().ToArray();
        }
        else
        {
            hasLyrics = true;
            string[] lines = File.ReadAllLines(lrcFilePath);
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
        }
        sortedLyrics = lyricPhrases.ToArray();
        if (sortedLyrics.Length > 1)
        {
            Array.Sort(sortedLyrics, (x, y) => x.TimeStampMs.CompareTo(y.TimeStampMs));
        }
        return sortedLyrics;//lyricPhrases;
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
        if (lyrics is null || lyrics.Count == 0)
        {
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
            if (sortedLyrics == null || sortedLyrics.Length == 0)
            {
                return null;
            }

            // Iterate through the lyrics until we find the first one with a timestamp greater than the current position
            for (int i = 0; i < sortedLyrics.Length; i++)
            {
                if (sortedLyrics[i].TimeStampMs > currentPositionInMs)
                {
                    // Return the previous lyric if it exists, otherwise return the first lyric
                    return i > 0 ? sortedLyrics[i - 1] : sortedLyrics[0];
                }
            }

            // If we didn't find any lyric with a timestamp greater than the current position, return the last lyric
            return sortedLyrics[sortedLyrics.Length - 1];
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

    public async Task<string> FetchLyricsOnline(SongsModelView song)
    {
        HttpClient client = new HttpClient();
        try
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

            // Optionally, parse the JSON response if needed
            var lyricsData = JsonSerializer.Deserialize<LyricsAPIReponse.Content[]>(content);

            return content; // Return the raw content or processed data
        }
        catch (HttpRequestException e)
        {
            Debug.WriteLine($"Request error: {e.Message}");
            return "Error fetching lyrics";
        }
        catch (Exception e)
        {
            Debug.WriteLine($"Unexpected error: {e.Message}");
            return "Unexpected error fetching lyrics";
        }
        finally
        {
            client.Dispose(); // Dispose of HttpClient to free resources
        }
    }

}
