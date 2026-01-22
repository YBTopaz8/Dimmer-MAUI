using System.Threading.Tasks;

using Dimmer.Interfaces;

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
    private readonly BehaviorSubject<int> _currentLyricIndexSubject = new(-1);
    private readonly BehaviorSubject<LyricPhraseModelView?> _nextLyricSubject = new(null);
    private readonly BehaviorSubject<bool> isSearchingLyrics = new(false);
    private readonly BehaviorSubject<bool> isLoadingLyrics = new(false);

    public IObservable<bool> IsSearchingLyrics => isSearchingLyrics.AsObservable();
    public IObservable<bool> IsLoadingLyrics => isLoadingLyrics.AsObservable();

    public IObservable<IReadOnlyList<LyricPhraseModelView>> AllSyncLyrics => _allLyricsSubject.AsObservable();
    public IObservable<LyricPhraseModelView?> PreviousLyric => _previousLyricSubject.AsObservable();
    public IObservable<int> CurrentLyricIndex => _currentLyricIndexSubject.AsObservable();
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
             async song =>
             {
                 await ProcessExistingLyricsForSong(song);
             },
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


        _subsManager.Add(Observable.FromEventPattern<double>(
                h => _audioService.PositionChanged += h,
                h => _audioService.PositionChanged -= h)
                .Select(evt => evt.EventArgs)
                .ObserveOn(RxSchedulers.UI)
                .Subscribe(posInSec =>
                {
                    if (_audioService.IsPlaying && _synchronizer != null)
                    {
                        UpdateLyricsForPosition(TimeSpan.FromSeconds(posInSec));
                    }
                }, ex =>
                {
                    _logger.LogError(ex, "Error in PositionChanged subscription");
                }));


    
    }

    public IObservable<double> AudioEnginePositionObservable { get; }
    /// <summary>
    /// A NEW PUBLIC method that the ViewModel will call when the user
    /// has chosen which lyrics to use from an online search.
    /// </summary>
    public void LoadLyrics(string? lrcContent)
    {
        try
        {
            
            _lyrics= GetListLyricsCol(lrcContent);
            
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

    public async Task<IEnumerable<LyricPhraseModelView>> GetLyrics(SongModelView songConcerned)
    {
        var res = await GetStoredLyricsContentAsync(songConcerned);
        if (res is null)
        {
            return Enumerable.Empty<LyricPhraseModelView>();
        }
        var _lyrics = GetListLyricsCol(res);
        if(_lyrics is null)
        {
            var OnlineLyrics = await GetLyricsContentAsync(songConcerned);
            var collectionfOfLyricModelViewsFromOnlineLyrics
                = new List<LyricPhraseModelView > { };
            
            foreach (var onlineLyric in OnlineLyrics ?? Enumerable.Empty<LrcLibLyrics>())
            {
                var lyricModelViews = GetListLyricsCol(onlineLyric.SyncedLyrics);
                collectionfOfLyricModelViewsFromOnlineLyrics.Add(lyricModelViews);
             
            }

            return collectionfOfLyricModelViewsFromOnlineLyrics;

        }
        else
        {
            return _lyrics;
        }
    }
    public static List<LyricPhraseModelView> GetListLyricsCol(string? lrcContent)
    {
        if (string.IsNullOrWhiteSpace(lrcContent))
        {
            
            return Enumerable.Empty<LyricPhraseModelView>().ToList();
        }
        // All the parsing and synchronizer setup logic is now here.
        var lyricsInfo = new LyricsInfo();
        lyricsInfo.Parse(lrcContent);


        if (lyricsInfo.SynchronizedLyrics.Count == 0)
        {
            return Enumerable.Empty<LyricPhraseModelView>().ToList();
        }

        // Your existing parsing logic to create List<LyricPhraseModelView> is good.
       var phrases = lyricsInfo.SynchronizedLyrics
            .Select(p => new LyricPhraseModelView
            {
                TimestampStart = p.TimestampStart
            ,
                TimeStampMs = p.TimestampEnd
            ,
                Text = p.Text,
                IsLyricSynced = true
            })
            .OrderBy(p => p.TimestampStart)
            .ToList();
        for (int i = 0; i < phrases.Count; i++)
        {
            phrases[i].DurationMs = (i + 1 < phrases.Count)
                ? phrases[i + 1].TimeStampMs - phrases[i].TimeStampMs
                : 3000; // Guess duration for the last line
        }

        return phrases;
    }

    /// <summary>
    /// Helper method that replaces the old automatic search logic.
    /// </summary>
    private async Task<string?> GetStoredLyricsContentAsync(SongModelView song)
    {
        var instruNullable = song.IsInstrumental;
        if ((instruNullable is bool instru))
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

    SongModelView? currentSong;
    private async Task ProcessExistingLyricsForSong(SongModelView? song)
    {
        if (song == null || currentSong?.TitleDurationKey == song.TitleDurationKey)
        {
            ClearLyrics();
            return;
        }

        isLoadingLyrics.OnNext(true);
        // Try to get lyrics from DB first, then local files.
        string? lrcContent = await GetStoredLyricsContentAsync(song);
        if (!string.IsNullOrWhiteSpace(lrcContent))
        {
            // If we found content, parse and load it for synchronization.
            LoadLyrics(lrcContent);
            isLoadingLyrics.OnNext(false);
        }
        else
        {

            ClearLyrics();
            isSearchingLyrics.OnNext(true);
            isLoadingLyrics.OnNext(false);
            var res = await GetLyricsContentAsync(song);

            if (res is not null && res.Any())
            {
                var lyrics = res.Where(x=>!string.IsNullOrEmpty(x.SyncedLyrics)).FirstOrDefault()?.SyncedLyrics;
                if (lyrics is null) 
                { 
                    return; 
                }
                LoadLyrics(lyrics);
                isSearchingLyrics.OnNext(false);
                isLoadingLyrics.OnNext(false);
            }
        }
    }



    private void ResetCurrentLyricDisplay()
    {
        _previousLyricSubject.OnNext(null);
        _currentLyricSubject.OnNext(null);
        _nextLyricSubject.OnNext(_lyrics.FirstOrDefault());
    }
    private async Task<IEnumerable<LrcLibLyrics>?> GetLyricsContentAsync(SongModelView song)
    {
        CancellationTokenSource cts = new();
        var instru = song.IsInstrumental is null || song.IsInstrumental is false && song.SyncLyrics.Length < 1;
        if (instru)
        {
            return null;
        }
        // Follows the hierarchy: DB -> Local Files -> Online
        if (!string.IsNullOrEmpty(song.SyncLyrics))
        {
            _logger.LogTrace("LYRICS FINDER :::::: Using lyrics from database for {SongTitle}", song.Title);
            LrcLibLyrics lyricsProps = new LrcLibLyrics()
            {
                AlbumName = song.AlbumName,
                ArtistName = song.OtherArtistsName,
                Duration = song.DurationInSeconds,
                TrackName = song.Title,
                SyncedLyrics = song.SyncLyrics,
                Instrumental = song.IsInstrumental is not null && (bool)song.IsInstrumental,
                PlainLyrics = song.UnSyncLyrics
            };
            List<LrcLibLyrics> list = new List<LrcLibLyrics>();
            list.Add(lyricsProps);
            return list;
        }


        _logger.LogTrace("LYRICS FINDER :::::: No local lyrics for {SongTitle}, searching online.", song.Title);
        IEnumerable<LrcLibLyrics>? onlineResults = await _lyricsMetadataService.GetAllLyricsPropsOnlineAsync(song.ToSongModel(), cts.Token);
        var onlineLyrics = onlineResults?.FirstOrDefault();

        if (onlineLyrics != null)
        {

            _logger.LogTrace("LYRICS FINDER :::::: Found online lyrics for {SongTitle} from {Source}", song.Title);

            // Optionally, save to DB or local storage here.
            await _lyricsMetadataService.SaveLyricsForSongAsync(song.Id, false,onlineLyrics.PlainLyrics, onlineLyrics.SyncedLyrics, false
                );
            return onlineResults;

        }
        else
        {
            _logger.LogTrace("LYRICS FINDER :::::: No online lyrics found for {SongTitle}.", song.Title);
        }
        return onlineResults;
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
        _currentLyricIndexSubject.OnNext(currentIndex);
        _previousLyricSubject.OnNext(currentIndex > 0 ? _lyrics[currentIndex - 1] : null);
        _nextLyricSubject.OnNext(currentIndex != -1 && currentIndex + 1 < _lyrics.Count ? _lyrics[currentIndex + 1] : null);
    }

    private void ClearLyrics()
    {
        _lyrics = Array.Empty<LyricPhraseModelView>();
        _synchronizer = null;
        _allLyricsSubject.OnNext(_lyrics);
        ResetCurrentLyrics();

        //should i do currentSong == null here? i'll leave for now
    }

    private void ResetCurrentLyrics()
    {
        _previousLyricSubject.OnNext(null);
        _currentLyricSubject.OnNext(null);
        _nextLyricSubject.OnNext(_lyrics.FirstOrDefault());
    }


    public void Dispose()
    {
        _subsManager.Dispose();


        _allLyricsSubject.OnCompleted();
        _previousLyricSubject.OnCompleted();
        _currentLyricSubject.OnCompleted();
        _nextLyricSubject.OnCompleted();
        _currentLyricIndexSubject.OnCompleted();
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
