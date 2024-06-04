

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

    public LyricsService(IPlayBackService songsManagerService)
    {
        PlayBackService = songsManagerService;

        SubscribeToPlayerStateChanges();
    }

    private void SubscribeToPlayerStateChanges()
    {
        PlayBackService.PlayerState.Subscribe(state =>
        {
            if (state == MediaPlayerState.Stopped)
            {
                StopLyricIndexUpdateTimer();
            }
            else if (state == MediaPlayerState.Playing)
            {
                LoadLyrics(PlayBackService.CurrentlyPlayingSong.FilePath);
                StartLyricIndexUpdateTimer();
            }
        });
    }

    public void LoadLyrics(string songPath)
    {

        sortedLyrics = [];
        IList<LyricPhraseModel> loadedLyrics = LoadSynchronizedLyrics(songPath);
        //string unsyncedLyrics = LoadUnsyncedLyrics(songPath);

        _synchronizedLyricsSubject.OnNext(loadedLyrics);

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
    private IList<LyricPhraseModel> LoadSynchronizedLyrics(string songPath)
    {
        IList<LyricsPhrase>? lyrics = new Track(songPath).Lyrics.SynchronizedLyrics;

        if (lyrics is not null && lyrics!.Count != 0)
        {
            var lyricss = lyrics.Select(phrase => new LyricPhraseModel(phrase)).ToList();
            sortedLyrics = lyricss.ToArray();
            if (sortedLyrics.Length > 1)
            {
                Array.Sort(sortedLyrics, (x, y) => x.TimeStampMs.CompareTo(y.TimeStampMs));
            }
            return sortedLyrics;//lyrics.Select(phrase => new LyricPhraseModel(phrase)).ToList();
        }

        string lrcFilePath = Path.ChangeExtension(songPath, ".lrc");
        List<LyricPhraseModel> lyricPhrases = new List<LyricPhraseModel>();

        if (File.Exists(lrcFilePath))
        {
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
         _lyricUpdateSubscription = PlayBackService.CurrentPosition
            .Sample(TimeSpan.FromMilliseconds(50))
            .Subscribe(
                async position =>
                {
                    double currentTimeinsSecs = position.CurrentTimeInSeconds;
                    await UpdateCurrentLyricIndex(currentTimeinsSecs);
                },
                error =>
                {
                    Debug.WriteLine($"Error in subscription: {error.Message}");
                }
                );
    }


    public async Task UpdateCurrentLyricIndex(double currentPositionInSeconds)
    {
        var lyrics = _synchronizedLyricsSubject.Value;  // Assuming this is already a list.
        if (lyrics is null || lyrics.Count == 0)
        {
            return;
        }
        
        double currentPositionInMs = currentPositionInSeconds * 1000;
        
        await Task.Delay(1000);
        var highlightedLyric = FindClosestLyric(currentPositionInMs + 200); //consider creating a variable for the offset

        if (highlightedLyric == null)
        {
            return;
        }

        if (!Equals(_currentLyricSubject.Value, highlightedLyric))
        {
            _currentLyricSubject.OnNext(highlightedLyric);
            //Debug.WriteLine($"Current Lyric: {_currentLyricSubject.Value.Text}");
        }
    }
    public LyricPhraseModel FindClosestLyric(double currentPositionInMs)
    {
        try
        {

            // Perform a binary search
            int left = 0;
            int right = sortedLyrics!.Length - 1;
            while (left < right)
            {
                int mid = left + (right - left) / 2;
                if (sortedLyrics[mid].TimeStampMs < currentPositionInMs)
                {
                    left = mid + 1;
                }
                else
                {
                    right = mid;
                }
            }

            // Return the closest match not greater than the current position
            return sortedLyrics[left];
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error when finding closest lyric {ex.Message}");
            return null;
        }
    }


    public void StopLyricIndexUpdateTimer()
    {
        _lyricUpdateSubscription?.Dispose();
    }
}
