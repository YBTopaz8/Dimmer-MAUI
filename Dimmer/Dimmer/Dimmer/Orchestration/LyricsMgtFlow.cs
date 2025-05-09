using Dimmer.Services;
using System.Text.RegularExpressions;

namespace Dimmer.Orchestration; 
public class LyricsMgtFlow : BaseAppFlow, IDisposable
{
    private readonly SongsMgtFlow songsMgt;
    private LyricSynchronizer? _synchronizer;

    // injected services
    private readonly IDimmerStateService _state;
    private readonly SubscriptionManager _subs;

    // the “source of truth” list, sorted by Time
    private List<LyricPhraseModel> _lyrics = new();
    private int _nextIndex;

    // emits lines exactly when they’re due
    private readonly Subject<LyricPhraseModel> _lineSubject = new();
    public IObservable<LyricPhraseModel> OnLyricLine => _lineSubject.AsObservable();

    // keep track so we can dispose/resubscribe
    private IDisposable? _positionSub;

    public LyricsMgtFlow(
        SongsMgtFlow songsMgt,
        IDimmerStateService state,
        IRepository<SongModel> songRepo,
        IRepository<GenreModel> genreRepo,
        IRepository<UserModel> userRepo,
        IRepository<AlbumArtistGenreSongLink> aagslRepo,
        IRepository<PlayDateAndCompletionStateSongLink> pdlRepo,
        IRepository<PlaylistModel> playlistRepo,
        IRepository<ArtistModel> artistRepo,
        IRepository<AlbumModel> albumRepo,
        ISettingsService settings,
        IFolderMgtService folderMonitor,
        IMapper mapper,
        SubscriptionManager subs
    ) : base(state, songRepo, genreRepo, userRepo, aagslRepo, pdlRepo, playlistRepo, artistRepo, albumRepo, settings, folderMonitor, subs, mapper)
    {
        this.songsMgt=songsMgt;
        _state = state;
        _subs  = subs;



        // 1) whenever the song changes, reload its lyrics
        _subs.Add(_state.CurrentSong
            .DistinctUntilChanged()
            .Subscribe(song =>
            {
                if (song == null)
                    return;
                LoadLyricsForSong(song);
            }));

        
        SubscribeToPosition();
    }

    private void LoadLyricsForSong(SongModelView song)
    {
        
        if (string.IsNullOrWhiteSpace(song.SyncLyrics))
        {
            _lyrics = new();
            _synchronizer = null;
            return;
        }

        List<LyricPhraseModel>? lines = song.SyncLyrics
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
                        .ToList()!;

        if(lines is not null)
        {
            _state.SetSyncLyrics(lines);
            _lyrics = lines;
            _synchronizer = new LyricSynchronizer(_lyrics);
        }

    }


    private void SubscribeToPosition()
    {
        _subs.Add(songsMgt.Position
            .Sample(TimeSpan.FromMilliseconds(700))
            .Subscribe( pos =>
            {
                UpdateCurrentLyricIndex(pos);
            }));
    }

    private void UpdateCurrentLyricIndex(double pos)
    {
        if (_synchronizer == null)
            return;

        var current = _synchronizer.GetCurrentLine(TimeSpan.FromSeconds(pos));
        if (current != null)
        {
            _state.SetCurrentLyric(current);    
        }
    }



    private void StopPositionWatch()
    {
        _positionSub?.Dispose();
        _positionSub = null;
    }


    public void Dispose()
    {
        StopPositionWatch();
        _subs.Dispose();
        _lineSubject.OnCompleted();
    }

    private class LyricSynchronizer
    {
        private readonly List<LyricPhraseModel> _lyrics;
        private int _currentIndex = -1;

        public LyricSynchronizer(List<LyricPhraseModel> lyrics)
        {
            _lyrics = [.. lyrics.OrderBy(l => l.TimeStampMs)];
        }

        public LyricPhraseModel? GetCurrentLine(TimeSpan position)
        {
            double posMs = position.TotalMilliseconds;

            if (_currentIndex >= 0 &&
                _currentIndex < _lyrics.Count &&
                posMs < _lyrics[_currentIndex].TimeStampMs)
            {
                _currentIndex = BinarySearchIndex(posMs);
            }

            while (_currentIndex + 1 < _lyrics.Count &&
                   _lyrics[_currentIndex + 1].TimeStampMs <= posMs)
            {
                _currentIndex++;
            }

            return (_currentIndex >= 0 && _currentIndex < _lyrics.Count)
                ? _lyrics[_currentIndex]
                : null;
        }

        private int BinarySearchIndex(double posMs)
        {
            int lo = 0, hi = _lyrics.Count - 1;
            while (lo <= hi)
            {
                int mid = lo + (hi - lo) / 2;
                if (_lyrics[mid].TimeStampMs == posMs)
                    return mid;
                if (_lyrics[mid].TimeStampMs < posMs)
                    lo = mid + 1;
                else
                    hi = mid - 1;
            }
            return Math.Max(lo - 1, 0);
        }
    }

}
