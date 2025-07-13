using ATL;

using Dimmer.Interfaces.Services;
using Dimmer.Interfaces.Services.Interfaces;
using Dimmer.Utilities.Events;
using Dimmer.Utilities.StatsUtils;

using Microsoft.Extensions.Logging.Abstractions;

using System.Text.RegularExpressions;


namespace Dimmer.Orchestration;

public class LyricsMgtFlow : IDisposable
{

    private readonly IDimmerStateService _stateService;
    private readonly IDimmerAudioService _audioService;
    private readonly SubscriptionManager _subsManager;
    private readonly ILyricsMetadataService _lyricsMetadataService;
    private readonly ILogger<LyricsMgtFlow> _logger;



    private IReadOnlyList<LyricPhraseModelView> _lyrics = Array.Empty<LyricPhraseModelView>();
    private LyricSynchronizer? _synchronizer;



    private readonly BehaviorSubject<IReadOnlyList<LyricPhraseModelView>> _allLyricsSubject = new(Array.Empty<LyricPhraseModelView>());
    private readonly BehaviorSubject<LyricPhraseModelView?> _previousLyricSubject = new(null);
    private readonly BehaviorSubject<LyricPhraseModelView?> _currentLyricSubject = new(null);
    private readonly BehaviorSubject<LyricPhraseModelView?> _nextLyricSubject = new(null);


    public IObservable<IReadOnlyList<LyricPhraseModelView>> AllSyncLyrics => _allLyricsSubject.AsObservable();
    public IObservable<LyricPhraseModelView?> PreviousLyric => _previousLyricSubject.AsObservable();
    public IObservable<LyricPhraseModelView?> CurrentLyric => _currentLyricSubject.AsObservable();
    public IObservable<LyricPhraseModelView?> NextLyric => _nextLyricSubject.AsObservable();

    public LyricsMgtFlow(
        IDimmerStateService stateService,
        IDimmerAudioService audioService,
        ILogger<LyricsMgtFlow> logger,
        SubscriptionManager subsManager,
        ILyricsMetadataService lyricsMetadataService

    )
    {
        _stateService = stateService;
        _audioService = audioService;
        _subsManager = subsManager;
        _lyricsMetadataService = lyricsMetadataService;
        _logger = logger ?? NullLogger<LyricsMgtFlow>.Instance;

        _subsManager.Add(
     Observable.FromEventPattern<PlaybackEventArgs>(h => _audioService.PlaybackStateChanged += h, h => _audioService.PlaybackStateChanged -= h)
         .Select(evt => evt.EventArgs)
         // We only care about the 'Playing' state, which signals a new track has begun.
         .Where(args => args.EventType == DimmerPlaybackState.Playing)
         // Get the song from the event arguments.
         .Select(args => args.MediaSong)
         // Ensure we don't re-process if the same song event fires twice.
         .DistinctUntilChanged(song => song?.Id)
         .Subscribe(
             async song => await ProcessSongForLyrics(song),
             ex => _logger.LogError(ex, "Error processing new song for lyrics from audio service event.")
         ));

        // ALSO, add a subscription to the Stop event to clear lyrics.
        _subsManager.Add(
            Observable.FromEventPattern<PlaybackEventArgs>(h => _audioService.PlayEnded += h, h => _audioService.PlayEnded -= h)
                // A simple stop/end should clear the lyrics.
                .Subscribe(
                    _ => ClearLyrics(),
                    ex => _logger.LogError(ex, "Error clearing lyrics on PlayEnded.")
                ));



        AudioEnginePositionObservable = Observable.FromEventPattern<double>(
                                             h => audioService.PositionChanged += h,
                                             h => audioService.PositionChanged -= h)
                                         .Select(evt => evt.EventArgs)
                                         .StartWith(audioService.CurrentPosition)
                                         .Replay(1).RefCount();
        SubscribeToPosition();
    }

    public IObservable<double> AudioEnginePositionObservable { get; }

    private async Task ProcessSongForLyrics(SongModelView? song)
    {
        if (song == null)
        {
            ClearLyrics();
            return;
        }

        try
        {
            // --- Step 1: Get raw LRC content from the best available source ---
            string? lrcContent = await GetLyricsContentAsync(song);
            if (string.IsNullOrWhiteSpace(lrcContent))
            {
                _logger.LogInformation("No lyrics content found for {SongTitle}", song.Title);
                ClearLyrics();
                return;
            }

            // --- Step 2: Use ATL to parse the content. This is the key change. ---
            var lyricsInfo = new LyricsInfo();
            lyricsInfo.Parse(lrcContent); // ATL does all the parsing work for us!

            if (lyricsInfo.SynchronizedLyrics.Count == 0)
            {
                _logger.LogInformation("Lyrics content for {SongTitle} was not synchronized.", song.Title);
                ClearLyrics();
                return;
            }


            var lines = lrcContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var phrases = new List<LyricPhraseModelView>();

            foreach (var line in lines)
            {
                int closingBracketIndex = line.IndexOf(']');
                if (line.StartsWith('[') && closingBracketIndex > -1)
                {
                    string timecodeStr = line.Substring(0, closingBracketIndex + 1);
                    string text = line.Substring(closingBracketIndex + 1).Trim();

                    // Use OUR utility class, which we can trust and maintain.
                    int timestampMs = LyricsParser.DecodeTimecodeToMs(timecodeStr);

                    if (timestampMs >= 0 && !string.IsNullOrWhiteSpace(text))
                    {
                        phrases.Add(new LyricPhraseModelView
                        {
                            TimestampStart = timestampMs, // We only have one timestamp from LRC
                            Text = text,
                            IsLyricSynced = true
                        });
                    }
                }
            }

            if (phrases.Count == 0)
            {
                _logger.LogInformation("Content for {SongTitle} was not a valid synchronized format.", song.Title);
                ClearLyrics();
                return;
            }

            // Sort by timestamp just in case the LRC file is out of order.
            _lyrics = phrases.OrderBy(p => p.TimestampStart).ToList();

            // Calculate durations now that the list is sorted.
            for (int i = 0; i < _lyrics.Count; i++)
            {
                int? nextTimestamp = (i + 1 < _lyrics.Count)
                    ? _lyrics[i + 1].TimestampStart
                    : null;

                // If there's a next line, duration is the gap. Otherwise, guess a duration.
                _lyrics[i].DurationMs = (nextTimestamp ?? (_lyrics[i].TimestampStart + 2000)) - _lyrics[i].TimestampStart;
            }

            _synchronizer = new LyricSynchronizer(_lyrics);

            _allLyricsSubject.OnNext(_lyrics);
            ResetCurrentLyricDisplay();

            if (song.SyncLyrics is not null || song.SyncLyrics =="")
            {
                await _lyricsMetadataService.SaveLyricsForSongAsync(song, lrcContent, lyricsInfo); // We don't have the LyricsInfo object anymore, pass null
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process lyrics for song: {SongTitle}", song.Title);
            ClearLyrics();
        }
    }

    private void ResetCurrentLyricDisplay()
    {
        _previousLyricSubject.OnNext(null);
        _currentLyricSubject.OnNext(null);
        _nextLyricSubject.OnNext(_lyrics.FirstOrDefault());
    }
    private async Task<string?> GetLyricsContentAsync(SongModelView song)
    {
        // Follows the hierarchy: DB -> Local Files -> Online
        if (!string.IsNullOrEmpty(song.SyncLyrics))
        {
            _logger.LogTrace("Using lyrics from database for {SongTitle}", song.Title);
            return song.SyncLyrics;
        }

        var localLyrics = await _lyricsMetadataService.GetLocalLyricsAsync(song);
        if (!string.IsNullOrEmpty(localLyrics))
        {
            return localLyrics;
        }

        _logger.LogTrace("No local lyrics for {SongTitle}, searching online.", song.Title);
        var onlineResults = await _lyricsMetadataService.SearchOnlineAsync(song);
        return onlineResults?.FirstOrDefault()?.SyncedLyrics;
    }

    private void UpdateLyricsForPosition(TimeSpan position)
    {
        if (_synchronizer == null)
            return;

        (int currentIndex, LyricPhraseModelView? currentLine) = _synchronizer.GetCurrentLineWithIndex(position);

        // No need to update if the line hasn't changed.
        if (currentLine?.TimestampStart == _currentLyricSubject.Value?.TimestampStart)
            return;

        // Update all three subjects at once.
        _currentLyricSubject.OnNext(currentLine);
        _previousLyricSubject.OnNext(currentIndex > 0 ? _lyrics[currentIndex - 1] : null);
        _nextLyricSubject.OnNext(currentIndex != -1 && currentIndex + 1 < _lyrics.Count ? _lyrics[currentIndex + 1] : null);
    }

    private void ClearLyrics()
    {
        _lyrics = Array.Empty<LyricPhraseModelView>();
        _synchronizer = null;
        _allLyricsSubject.OnNext(_lyrics);
        ResetCurrentLyrics();
    }

    private void ResetCurrentLyrics()
    {
        _previousLyricSubject.OnNext(null);
        _currentLyricSubject.OnNext(null);
        _nextLyricSubject.OnNext(_lyrics.FirstOrDefault());
    }

    private void SubscribeToPosition()
    {
        // Stream 1: A stream that tells us if we are currently playing or not.
        // We start with the current value and get subsequent changes.
        var isPlayingStream = Observable.FromEventPattern<PlaybackEventArgs>(h => _audioService.IsPlayingChanged += h, h => _audioService.IsPlayingChanged -= h)
            .Select(evt => evt.EventArgs.IsPlaying)
            .StartWith(_audioService.IsPlaying); // IMPORTANT: Get the initial state

        // Stream 2: Our existing stream of position updates.
        var positionStream = AudioEnginePositionObservable;

        _subsManager.Add(
            // Combine the two streams.
            // We only care about the position WHEN the isPlayingStream's latest value is 'true'.
            positionStream
                .WithLatestFrom(isPlayingStream, (position, isPlaying) => new { position, isPlaying }) // Combine into an anonymous object
                .Where(x => x.isPlaying && _synchronizer != null) // Now we filter based on the combined data
                .Select(x => x.position) // We only need the position from here on
                .Sample(TimeSpan.FromMilliseconds(100))
                .DistinctUntilChanged()
                .Subscribe(
                    positionInSeconds =>
                    {
                        UpdateLyricsForPosition(TimeSpan.FromSeconds(positionInSeconds));
                    },
                    ex => _logger.LogError(ex, "Error in position subscription.")
                )
        );
    }

    public void Dispose()
    {
        _subsManager.Dispose();


        _allLyricsSubject.OnCompleted();
        _previousLyricSubject.OnCompleted();
        _currentLyricSubject.OnCompleted();
        _nextLyricSubject.OnCompleted();
    }
    private sealed class LyricSynchronizer
    {
        private readonly IReadOnlyList<LyricPhraseModelView> _lyrics;

        public LyricSynchronizer(IReadOnlyList<LyricPhraseModelView> lyrics)
        {
            // The list is already sorted from the creation process
            _lyrics = lyrics;
        }

        public (int Index, LyricPhraseModelView? Line) GetCurrentLineWithIndex(TimeSpan position)
        {
            if (_lyrics.Count == 0)
                return (-1, null);

            int posMs = (int)position.TotalMilliseconds;

            // Binary search is the most efficient way to find the current line
            int low = 0;
            int high = _lyrics.Count - 1;
            int resultIndex = -1;

            while (low <= high)
            {
                int mid = low + (high - low) / 2;
                if (_lyrics[mid].TimestampStart <= posMs)
                {
                    resultIndex = mid;
                    low = mid + 1;
                }
                else
                {
                    high = mid - 1;
                }
            }

            if (resultIndex != -1)
            {
                return (resultIndex, _lyrics[resultIndex]);
            }

            return (-1, null); // Position is before the first lyric
        }
    }
}