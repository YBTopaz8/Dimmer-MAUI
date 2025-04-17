using System;
using System.Linq;
using Dimmer.Data;
using Dimmer.Utilities.Events;
using Dimmer.Utilities.Enums;
using AutoMapper;

namespace Dimmer.Orchestration
{
    public class BaseAppFlow : IDisposable
    {

        private IDisposable _songsToken;

        public readonly IPlayerStateService _state;
        private readonly IRepository<SongModel> _songRepo;
        private readonly IRepository<PlayDateAndCompletionStateSongLink> _pdlRepo;
        private readonly IRepository<PlaylistModel> _playlistRepo;
        private readonly IRepository<ArtistModel> _artistRepo;
        private readonly IRepository<AlbumModel> _albumRepo;
        private readonly ISettingsService _settings;
        private readonly IFolderMonitorService _folderMonitor;
        public readonly IMapper _mapper;
        private bool _disposed;

        public SongModelView CurrentlyPlayingSong { get; set; } = new();
        public SongModel CurrentlyPlayingSongDB { get; set; } = new();

        public bool IsShuffleOn
            => _settings.ShuffleOn;

        public RepeatMode CurrentRepeatMode
            => _settings.RepeatMode;

        public BaseAppFlow(
            IPlayerStateService state,
            IRepository<SongModel> songRepo,
            IRepository<PlayDateAndCompletionStateSongLink> pdlRepo,
            IRepository<PlaylistModel> playlistRepo,
            IRepository<ArtistModel> artistRepo,
            IRepository<AlbumModel> albumRepo,
            ISettingsService settings,
            IFolderMonitorService folderMonitor,
            IMapper mapper)
        {
            _state = state;
            _songRepo = songRepo;
            _pdlRepo = pdlRepo;
            _playlistRepo = playlistRepo;
            _artistRepo = artistRepo;
            _albumRepo = albumRepo;
            _settings = settings;
            _folderMonitor = folderMonitor;
            _mapper = mapper;

            Initialize();
        }

        private void Initialize()
        {
            // 1. Load master song list
            var allSongs = _songRepo
                .GetAll()
                .OrderBy(x => x.DateCreated);
            _state.LoadAllSongs(allSongs);

            // 2. Start folder‑watching from user prefs
            _folderMonitor.Start(_settings.UserMusicFoldersPreference);

            // Subscribe to live updates
            _songsToken = _songRepo.GetAll().SubscribeForNotifications((col, changes) =>
            {
                _state.LoadAllSongs(col.ToList());
            });

        }

        public void SetCurrentSong(SongModelView song)
        {
            // update local view + replay into your global stream
            var songDb = _mapper.Map<SongModel>(song);
            CurrentlyPlayingSongDB = songDb;
            _state.CurrentSong.OnNext(songDb);
        }

        public void PlaySong()
            => UpdatePlaybackState(CurrentlyPlayingSong, PlayType.Play);

        public void PauseSong()
            => UpdatePlaybackState(CurrentlyPlayingSong, PlayType.Pause);

        public void ResumeSong()
            => UpdatePlaybackState(CurrentlyPlayingSong, PlayType.Resume);
        
        public void PlayEnded()
            => UpdatePlaybackState(CurrentlyPlayingSong, PlayType.Resume);

        public void UpdatePlaybackState(
            SongModelView? view,
            PlayType type,
            double? position = null)
        {
            var songView = view ?? CurrentlyPlayingSong;
            var songDb = _mapper.Map<SongModel>(songView);
            CurrentlyPlayingSongDB = songDb;

            var link = new PlayDateAndCompletionStateSongLink
            {
                LocalDeviceId =
                    string.IsNullOrEmpty(songDb.LocalDeviceId)
                        ? Guid.NewGuid().ToString()
                        : songDb.LocalDeviceId,
                SongId = songDb.LocalDeviceId,
                PlayType = (int)type,
                DatePlayed = DateTime.Now,
                PositionInSeconds = position ?? 0,
                WasPlayCompleted = type == PlayType.Completed
            };

            _pdlRepo.AddOrUpdate(link);
        }

        public void UpsertPlaylist(PlaylistModel model)
        {
            if (string.IsNullOrEmpty(model.LocalDeviceId))
                model.LocalDeviceId = Guid.NewGuid().ToString();
            _playlistRepo.AddOrUpdate(model);
        }

        public void UpsertArtist(ArtistModel model)
        {
            if (string.IsNullOrEmpty(model.LocalDeviceId))
                model.LocalDeviceId = Guid.NewGuid().ToString();
            _artistRepo.AddOrUpdate(model);
        }

        public void UpsertAlbum(AlbumModel model)
        {
            if (string.IsNullOrEmpty(model.LocalDeviceId))
                model.LocalDeviceId = Guid.NewGuid().ToString();
            _albumRepo.AddOrUpdate(model);
        }

        public void ToggleShuffle(bool isOn)
            => _settings.ShuffleOn = isOn;

        public RepeatMode ToggleRepeatMode()
        {
            var next = (RepeatMode)(((int)_settings.RepeatMode + 1) % 3);
            _settings.RepeatMode = next;
            return next;
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _state.Dispose();
            _folderMonitor.Dispose();
            _disposed = true;
        }
    }
}
