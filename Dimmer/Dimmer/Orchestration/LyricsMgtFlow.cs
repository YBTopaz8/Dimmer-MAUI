using ATL;

using Dimmer.Interfaces.Services.Interfaces;
using Dimmer.Utilities.Events;

using Microsoft.Extensions.Logging.Abstractions;


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
         //.DistinctUntilChanged(song => song?.Id)
         .Subscribe(
             async song => await ProcessExistingLyricsForSong(song),
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
    /// <summary>
    /// A NEW PUBLIC method that the ViewModel will call when the user
    /// has chosen which lyrics to use from an online search.
    /// </summary>
    public void LoadLyrics(string lrcContent)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(lrcContent))
            {
                ClearLyrics();
                return;
            }
            // All the parsing and synchronizer setup logic is now here.
            var lyricsInfo = new LyricsInfo();
            lyricsInfo.Parse(lrcContent);

            if (lyricsInfo.SynchronizedLyrics.Count == 0)
            {
                ClearLyrics();
                return;
            }

            // Your existing parsing logic to create List<LyricPhraseModelView> is good.
            var phrases = lyricsInfo.SynchronizedLyrics
                .Select(p => new LyricPhraseModelView { TimestampStart = p.TimestampStart
                ,TimeStampMs=p.TimestampEnd
                , Text = p.Text, IsLyricSynced = true })
                .OrderBy(p => p.TimestampStart)
                .ToList();

            for (int i = 0; i < phrases.Count; i++)
            {
                phrases[i].DurationMs = (i + 1 < phrases.Count)
                    ? phrases[i + 1].TimeStampMs - phrases[i].TimeStampMs
                    : 3000; // Guess duration for the last line
            }

            _lyrics = phrases;
            _synchronizer = new LyricSynchronizer(_lyrics);
            _allLyricsSubject.OnNext(_lyrics);
            ResetCurrentLyricDisplay();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse and load provided LRC content.");
            ClearLyrics();
        }
    }

    /// <summary>
    /// Helper method that replaces the old automatic search logic.
    /// </summary>
    private async Task<string?> GetStoredLyricsContentAsync(SongModelView song)
    {
        var instru = song.IsInstrumental;
        if (instru )
        {
            return string.Empty;
        }
        if (!string.IsNullOrEmpty(song.SyncLyrics))
        {
            _logger.LogTrace("Found lyrics in database for {SongTitle}", song.Title);
            return song.SyncLyrics;
        }

        var localLyrics = await _lyricsMetadataService.GetLocalLyricsAsync(song);
        if (!string.IsNullOrEmpty(localLyrics))
        {
            return localLyrics;
        }
        return null; // Don't search online here.
    }

    private async Task ProcessExistingLyricsForSong(SongModelView? song)
    {
        if (song == null)
        {
            ClearLyrics();
            return;
        }

        // Try to get lyrics from DB first, then local files.
        string? lrcContent = await GetStoredLyricsContentAsync(song);
        if (!string.IsNullOrWhiteSpace(lrcContent))
        {
            // If we found content, parse and load it for synchronization.
            LoadLyrics(lrcContent);
        }
        else
        {

            //get lyrics from online source
            var res = await GetLyricsContentAsync(song);
            LoadLyrics(res);
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
        var instru = song.IsInstrumental;
        if (instru)
        {
            return string.Empty;
        }
        // Follows the hierarchy: DB -> Local Files -> Online
        if (!string.IsNullOrEmpty(song.SyncLyrics))
        {
            _logger.LogTrace("LYRICS FINDER :::::: Using lyrics from database for {SongTitle}", song.Title);
            return song.SyncLyrics;
        }

        var localLyrics = await _lyricsMetadataService.GetLocalLyricsAsync(song);
        if (!string.IsNullOrEmpty(localLyrics))
        {
            return localLyrics;
        }

        _logger.LogTrace("LYRICS FINDER :::::: No local lyrics for {SongTitle}, searching online.", song.Title);
        var onlineResults = await _lyricsMetadataService.SearchOnlineAsync(song);
        var onlineLyrics = onlineResults?.FirstOrDefault();
        if (onlineLyrics != null)
        {
            _logger.LogTrace("LYRICS FINDER :::::: Found online lyrics for {SongTitle} from {Source}", song.Title);

            // Optionally, save to DB or local storage here.
            await _lyricsMetadataService.SaveLyricsForSongAsync(onlineLyrics.Instrumental!,onlineLyrics.PlainLyrics,song,onlineLyrics.SyncedLyrics,null);
            return onlineLyrics.SyncedLyrics;

        }
        else
        {
            _logger.LogTrace("LYRICS FINDER :::::: No online lyrics found for {SongTitle}.", song.Title);
        }
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