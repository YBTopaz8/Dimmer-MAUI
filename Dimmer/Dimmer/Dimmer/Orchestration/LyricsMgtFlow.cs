using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;

using Dimmer.Interfaces.Services.Interfaces;
using Dimmer.Utilities.Events;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
// Add other necessary using statements for your models (SongModelView, LyricPhraseModel, etc.)

namespace Dimmer.Orchestration;

public class LyricsMgtFlow : IDisposable
{
    // --- Injected Services ---
    private readonly IDimmerStateService _stateService;
    private readonly IDimmerAudioService _audioService;
    private readonly SubscriptionManager _subsManager;
    private readonly ILyricsMetadataService lyricsMetadataService;
    private readonly ILogger<LyricsMgtFlow> _logger;

    // --- Private State ---
    // The "source of truth" list for the current song, sorted by time.
    private IReadOnlyList<LyricPhraseModel> _lyrics = Array.Empty<LyricPhraseModel>();
    private LyricSynchronizer? _synchronizer;
    private bool _isPlaying;

    // --- Reactive Subjects (The heart of our state management) ---
    // These subjects hold the current state and broadcast it to subscribers.
    private readonly BehaviorSubject<IReadOnlyList<LyricPhraseModel>> _allLyricsSubject = new(Array.Empty<LyricPhraseModel>());
    private readonly BehaviorSubject<LyricPhraseModel?> _previousLyricSubject = new(null);
    private readonly BehaviorSubject<LyricPhraseModel?> _currentLyricSubject = new(null);
    private readonly BehaviorSubject<LyricPhraseModel?> _nextLyricSubject = new(null);

    // --- Public Observables (The clean, read-only API for other classes) ---
    public IObservable<IReadOnlyList<LyricPhraseModel>> AllSyncLyrics => _allLyricsSubject.AsObservable();
    public IObservable<LyricPhraseModel?> PreviousLyric => _previousLyricSubject.AsObservable();
    public IObservable<LyricPhraseModel?> CurrentLyric => _currentLyricSubject.AsObservable();
    public IObservable<LyricPhraseModel?> NextLyric => _nextLyricSubject.AsObservable();

    public LyricsMgtFlow(
        IDimmerStateService stateService,
        IDimmerAudioService audioService,
        ILogger<LyricsMgtFlow> logger,
        SubscriptionManager subsManager,
        ILyricsMetadataService lyricsMetadataService
        , SongsMgtFlow songsMgtFlow
    // ... other dependencies are no longer needed here if they aren't directly used
    )
    {
        _stateService = stateService;
        _audioService = audioService;
        _subsManager = subsManager;
        this.lyricsMetadataService=lyricsMetadataService;
        _logger = logger ?? NullLogger<LyricsMgtFlow>.Instance;
        _songsMgtFlow=songsMgtFlow;
        // 1. When the song changes, load its lyrics.
        _subsManager.Add(_stateService.CurrentSong
            .DistinctUntilChanged()
            .Subscribe(
                async song => await LoadLyricsForSong(song),
                ex => _logger.LogError(ex, "Error processing new song for lyrics.")
            ));

        // 2. When playback state changes, update our internal flag.
        _subsManager.Add(
           Observable.FromEventPattern<PlaybackEventArgs>(h => _audioService.IsPlayingChanged += h, h => _audioService.IsPlayingChanged -= h)
               .Select(evt => evt.EventArgs.IsPlaying)
               .Subscribe(
                   isPlaying => _isPlaying = isPlaying,
                   ex => _logger.LogError(ex, "Error in IsPlayingChanged subscription.")
               ));

        // 3. When the player position changes, update the current lyric.
        SubscribeToPosition();
    }

    private async Task LoadLyricsForSong(SongModelView? song)
    {
        // If the song is null or has no lyrics, reset everything.
        if (song == null)
        {
            _lyrics = Array.Empty<LyricPhraseModel>();
            _synchronizer = null;

            // Notify subscribers that there are no lyrics.
            _allLyricsSubject.OnNext(_lyrics);
            _previousLyricSubject.OnNext(null);
            _currentLyricSubject.OnNext(null);
            _nextLyricSubject.OnNext(null);
            return;
        }

        try
        {
            bool hasSync = !string.IsNullOrEmpty(song.SyncLyrics);

            string? lyrr = string.Empty;
            if (!hasSync)
            {
                string? local = await lyricsMetadataService.GetLocalLyricsAsync(song);
                if (string.IsNullOrEmpty(local))
                {
                    var online = await lyricsMetadataService.SearchOnlineAsync(song);
                    lyrr = online.FirstOrDefault()?.SyncedLyrics;
                }
                else
                {
                    lyrr= local; // Use the local lyrics if available.
                }
            }
            else
            {
                lyrr= song.SyncLyrics!; // Use the synced lyrics directly from the song model.
            }
            if (lyrr is null)
            {
               
                    _lyrics = Array.Empty<LyricPhraseModel>();
                    _synchronizer = null;

                    // Notify subscribers that there are no lyrics.
                    _allLyricsSubject.OnNext(_lyrics);
                    _previousLyricSubject.OnNext(null);
                    _currentLyricSubject.OnNext(null);
                    _nextLyricSubject.OnNext(null);
                    return;
                

            }
            // Parse the LRC format lyrics.
            var lines = lyrr
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(l =>
                {
                    var match = Regex.Match(l, @"\[(\d{2}):(\d{2})\.(\d{2,3})\](.*)");
                    if (!match.Success)
                        return null;

                    int min = int.Parse(match.Groups[1].Value);
                    int sec = int.Parse(match.Groups[2].Value);
                    int ms = int.Parse(match.Groups[3].Value.PadRight(3, '0'));
                    string text = match.Groups[4].Value.Trim();

                    return new LyricPhraseModel
                    {
                        TimeStampMs = (min * 60 + sec) * 1000 + ms,
                        Text = text
                    };
                })
                .Where(x => x != null)
                .OrderBy(x => x!.TimeStampMs)
                .ToList()!; // We know it's not null due to the Where clause.

            // Update the internal state and the synchronizer.
            _lyrics = lines;
            _synchronizer = new LyricSynchronizer(_lyrics);

            // Notify all subscribers of the new data.
            _allLyricsSubject.OnNext(_lyrics);

            // Reset current/prev/next, they will be updated on the next position tick.
            _previousLyricSubject.OnNext(null);
            _currentLyricSubject.OnNext(null);
            _nextLyricSubject.OnNext(_lyrics.FirstOrDefault()); // Show the first line as "next".
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse lyrics for song: {SongTitle}", song.Title);
            // Ensure state is clean on failure.
            _lyrics = Array.Empty<LyricPhraseModel>();
            _synchronizer = null;
            _allLyricsSubject.OnNext(_lyrics);
        }
    }

    //private void SubscribeToPosition()
    //{
    //    // This is the main reactive pipeline for lyric synchronization.
    //    _subsManager.Add(_songsMgtFlow.AudioEnginePositionObservable // Assuming Position is an IObservable<TimeSpan>
    //        .Sample(TimeSpan.FromMilliseconds(250)) // Check the position 4 times a second.
    //        .Where(_ => _isPlaying && _synchronizer != null) // Only process if playing and lyrics are loaded.
    //        .Subscribe(
    //            position => UpdateLyricsForPosition(position),
    //            ex => _logger.LogError(ex, "Error in position subscription.")
    //        ));


    //}
    private void SubscribeToPosition()
    {
        // This is the main reactive pipeline for lyric synchronization.
        _subsManager.Add(
            _songsMgtFlow.AudioEnginePositionObservable
                // 3. Only process if playing and lyrics are loaded.
                .Where(_ => _isPlaying && _synchronizer != null)
                // 4. (Optional but recommended) Only proceed if the position has actually changed.
                //    This prevents processing when paused.
                .Sample(TimeSpan.FromMilliseconds(450))
                .DistinctUntilChanged()
                .Subscribe(
                    // The 'position' here is a double (in seconds).
                    positionInSeconds =>
                    {
                        // Convert the double to a TimeSpan before calling our update logic.
                        UpdateLyricsForPosition(TimeSpan.FromSeconds(positionInSeconds));
                    },
                    ex => _logger.LogError(ex, "Error in position subscription.")
                )
        );
    }
    private void UpdateLyricsForPosition(TimeSpan position)
    {
        if (_synchronizer == null)
            return;

        // Find the current line based on the timestamp.
        (int currentIndex, LyricPhraseModel? currentLine) = _synchronizer.GetCurrentLineWithIndex(position);

        // OPTIMIZATION: Only push updates if the current line has actually changed.
        if (currentLine?.TimeStampMs == _currentLyricSubject.Value?.TimeStampMs)
        {
            return;
        }

        // We have a new current line, let's find its neighbors.
        LyricPhraseModel? previousLine = null;
        LyricPhraseModel? nextLine = null;

        if (currentIndex != -1)
        {
            // Safely get the previous line.
            if (currentIndex > 0)
            {
                previousLine = _lyrics[currentIndex - 1];
            }
            // Safely get the next line.
            if (currentIndex < _lyrics.Count - 1)
            {
                nextLine = _lyrics[currentIndex + 1];
            }
        }

        // Push the new state to all subscribers.
        _previousLyricSubject.OnNext(previousLine);
        _currentLyricSubject.OnNext(currentLine);
        _nextLyricSubject.OnNext(nextLine);
    }

    private readonly SongsMgtFlow _songsMgtFlow;
    public void Dispose()
    {
        _subsManager.Dispose();

        // Complete all subjects to signal to subscribers that the stream has ended.
        _allLyricsSubject.OnCompleted();
        _previousLyricSubject.OnCompleted();
        _currentLyricSubject.OnCompleted();
        _nextLyricSubject.OnCompleted();
    }

    // This inner class is great, let's just add a method to get the index too.
    sealed class LyricSynchronizer
    {
        private readonly IReadOnlyList<LyricPhraseModel> _lyrics;
        private int _lastFoundIndex = -1;

        public LyricSynchronizer(IReadOnlyList<LyricPhraseModel> lyrics)
        {
            _lyrics = lyrics; // Assumes lyrics are already sorted.
        }

        public (int Index, LyricPhraseModel? Line) GetCurrentLineWithIndex(TimeSpan position)
        {
            if (_lyrics.Count == 0)
                return (-1, null);

            double posMs = position.TotalMilliseconds;

            // Start searching from the last known index for efficiency
            int searchIndex = _lastFoundIndex;
            if (searchIndex < 0)
                searchIndex = 0;

            // If we've seeked backwards, we need to reset the search
            if (searchIndex < _lyrics.Count && posMs < _lyrics[searchIndex].TimeStampMs)
            {
                searchIndex = BinarySearchForPreviousIndex(posMs);
            }

            // Search forward from the current position
            while (searchIndex + 1 < _lyrics.Count && _lyrics[searchIndex + 1].TimeStampMs <= posMs)
            {
                searchIndex++;
            }

            _lastFoundIndex = searchIndex;

            if (searchIndex >= 0 && searchIndex < _lyrics.Count)
            {
                return (searchIndex, _lyrics[searchIndex]);
            }

            return (-1, null);
        }

        private int BinarySearchForPreviousIndex(double posMs)
        {
            int low = 0;
            int high = _lyrics.Count - 1;
            int result = -1;

            while (low <= high)
            {
                int mid = low + (high - low) / 2;
                if (_lyrics[mid].TimeStampMs <= posMs)
                {
                    result = mid;
                    low = mid + 1;
                }
                else
                {
                    high = mid - 1;
                }
            }
            return result;
        }
    }
}