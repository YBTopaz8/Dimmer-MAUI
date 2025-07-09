using System.Text.RegularExpressions;

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
    private readonly ILyricsMetadataService lyricsMetadataService;
    private readonly ILogger<LyricsMgtFlow> _logger;



    private IReadOnlyList<LyricPhraseModelView> _lyrics = Array.Empty<LyricPhraseModelView>();
    private LyricSynchronizer? _synchronizer;
    private bool _isPlaying;



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
        this.lyricsMetadataService=lyricsMetadataService;
        _logger = logger ?? NullLogger<LyricsMgtFlow>.Instance;

        _subsManager.Add(_stateService.CurrentSong
            .DistinctUntilChanged()
            .Subscribe(
                async song => await LoadLyricsForSong(song),
                ex => _logger.LogError(ex, "Error processing new song for lyrics.")
            ));


        _subsManager.Add(
           Observable.FromEventPattern<PlaybackEventArgs>(h => _audioService.IsPlayingChanged += h, h => _audioService.IsPlayingChanged -= h)
               .Select(evt => evt.EventArgs.IsPlaying)
               .Subscribe(
                   isPlaying => _isPlaying = isPlaying,
                   ex => _logger.LogError(ex, "Error in IsPlayingChanged subscription.")
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
    private async Task LoadLyricsForSong(SongModelView? song)
    {

        if (song == null)
        {
            _lyrics = Array.Empty<LyricPhraseModelView>();
            _synchronizer = null;


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
                    lyrr= local;
                }
            }
            else
            {
                lyrr= song.SyncLyrics!;
            }
            if (lyrr is null)
            {

                _lyrics = Array.Empty<LyricPhraseModelView>();
                _synchronizer = null;


                _allLyricsSubject.OnNext(_lyrics);
                _previousLyricSubject.OnNext(null);
                _currentLyricSubject.OnNext(null);
                _nextLyricSubject.OnNext(null);
                return;


            }

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

                    return new LyricPhraseModelView
                    {
                        TimeStampMs = (min * 60 + sec) * 1000 + ms,
                        Text = text
                    };
                })
                .Where(x => x != null)
                .OrderBy(x => x!.TimeStampMs)
                .ToList()!;


            _lyrics = lines;
            _synchronizer = new LyricSynchronizer(_lyrics);


            _allLyricsSubject.OnNext(_lyrics);


            _previousLyricSubject.OnNext(null);
            _currentLyricSubject.OnNext(null);
            _nextLyricSubject.OnNext(_lyrics.FirstOrDefault());

            Track songFile = new Track(song.FilePath);
            songFile.Lyrics.ParseLRC(lyrr);
            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse lyrics for song: {SongTitle}", song.Title);

            _lyrics = Array.Empty<LyricPhraseModelView>();
            _synchronizer = null;
            _allLyricsSubject.OnNext(_lyrics);
        }
    }
    private void SubscribeToPosition()
    {

        _subsManager.Add(
            AudioEnginePositionObservable

            .Sample(TimeSpan.FromMilliseconds(100))


            .Where(_ => _isPlaying && _synchronizer != null)


            .DistinctUntilChanged()

            .Subscribe(

                positionInSeconds =>
                {

                    UpdateLyricsForPosition(TimeSpan.FromSeconds(positionInSeconds));
                },
                ex => _logger.LogError(ex, "Error in position subscription.")
            ));
    }
    private void UpdateLyricsForPosition(TimeSpan position)
    {
        if (_synchronizer == null)
            return;


        (int currentIndex, LyricPhraseModelView? currentLine) = _synchronizer.GetCurrentLineWithIndex(position);


        if (currentLine?.TimeStampMs == _currentLyricSubject.Value?.TimeStampMs)
        {
            return;
        }


        LyricPhraseModelView? previousLine = null;
        LyricPhraseModelView? nextLine = null;

        if (currentIndex != -1)
        {

            if (currentIndex > 0)
            {
                previousLine = _lyrics[currentIndex - 1];
            }

            if (currentIndex < _lyrics.Count - 1)
            {
                nextLine = _lyrics[currentIndex + 1];
            }
        }


        _previousLyricSubject.OnNext(previousLine);
        _currentLyricSubject.OnNext(currentLine);
        _nextLyricSubject.OnNext(nextLine);
    }

    private readonly SongsMgtFlow _songsMgtFlow;
    public void Dispose()
    {
        _subsManager.Dispose();


        _allLyricsSubject.OnCompleted();
        _previousLyricSubject.OnCompleted();
        _currentLyricSubject.OnCompleted();
        _nextLyricSubject.OnCompleted();
    }


    sealed class LyricSynchronizer
    {
        private readonly IReadOnlyList<LyricPhraseModelView> _lyrics;
        private int _lastFoundIndex = -1;

        public LyricSynchronizer(IReadOnlyList<LyricPhraseModelView> lyrics)
        {
            _lyrics = lyrics;
        }

        public (int Index, LyricPhraseModelView? Line) GetCurrentLineWithIndex(TimeSpan position)
        {
            if (_lyrics.Count == 0)
                return (-1, null);

            double posMs = position.TotalMilliseconds;


            int searchIndex = _lastFoundIndex;
            if (searchIndex < 0)
                searchIndex = 0;


            if (searchIndex < _lyrics.Count && posMs < _lyrics[searchIndex].TimeStampMs)
            {
                searchIndex = BinarySearchForPreviousIndex(posMs);
            }


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