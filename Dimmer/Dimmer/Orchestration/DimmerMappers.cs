using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Dimmer.Interfaces.Services.Interfaces.FileProcessing.FileProcessorUtils;

using static Dimmer.Data.Models.LastFMUser;
using static Dimmer.Data.ModelView.LastFMUserView;

namespace Dimmer.Orchestration;



public static class DimmerMappers
{
    // ==============================================================================
    // 🎵 SONG MAPPERS
    // ==============================================================================

    public static SongModelView? ToSongModelView(this SongModel? src)
    {
        if (src is null) return null;

        var dest = new SongModelView
        {
            // --- Scalar Properties ---
            Id = src.Id,
            Title = src.Title,
            FilePath = src.FilePath,
            DurationInSeconds = src.DurationInSeconds,
            IsHidden = src.IsHidden,
            ReleaseYear = src.ReleaseYear,
            NumberOfTimesFaved = src.NumberOfTimesFaved,
            ManualFavoriteCount = src.ManualFavoriteCount,
            TrackNumber = src.TrackNumber,
            FileFormat = src.FileFormat,
            Lyricist = src.Lyricist,
            Composer = src.Composer,
            Conductor = src.Conductor,
            Description = src.Description,
            Language = src.Language,
            DiscNumber = src.DiscNumber,
            DiscTotal = src.DiscTotal,
            FileSize = src.FileSize,
            BitRate = src.BitRate,
            Rating = src.Rating,
            HasLyrics = src.HasLyrics,
            HasSyncedLyrics = src.HasSyncedLyrics,
            IsInstrumental = src.IsInstrumental,
            SyncLyrics = src.SyncLyrics,
            CoverImagePath = src.CoverImagePath,
            TrackTotal = src.TrackTotal,
            SampleRate = src.SampleRate,
            Encoder = src.Encoder,
            BitDepth = src.BitDepth,
            NbOfChannels = src.NbOfChannels,
            UnSyncLyrics = src.UnSyncLyrics,
            IsFavorite = src.IsFavorite,
            Achievement = src.Achievement,
            IsFileExists = src.IsFileExists,
            LastDateUpdated = src.LastDateUpdated,
            DateCreated = src.DateCreated,
            DeviceName = src.DeviceName,
            DeviceFormFactor = src.DeviceFormFactor,
            DeviceModel = src.DeviceModel,
            DeviceManufacturer = src.DeviceManufacturer,
            DeviceVersion = src.DeviceVersion,
            UserIDOnline = src.UserIDOnline,
            IsNew = src.IsNew,
            BPM = src.BPM,
            TitleDurationKey = src.TitleDurationKey,
            SongTypeValue = src.SongTypeValue,
            ParentSongId = src.ParentSongId,
            SegmentStartTime = src.SegmentStartTime,
            SegmentEndTime = src.SegmentEndTime,
            SegmentEndBehaviorValue = src.SegmentEndBehaviorValue,
            CoverArtHash = src.CoverArtHash,
            // SearchableText is computed in ViewModel usually, or mapped here if saved
            // UserNoteAggregatedText is computed

            // --- Statistics ---
            PlayCount = src.PlayCount,
            PlayCompletedCount = src.PlayCompletedCount,
            SkipCount = src.SkipCount,
            LastPlayed = src.LastPlayed,
            ListenThroughRate = src.ListenThroughRate,
            SkipRate = src.SkipRate,
            FirstPlayed = src.FirstPlayed,
            PopularityScore = src.PopularityScore,
            GlobalRank = src.GlobalRank,
            RankInAlbum = src.RankInAlbum,
            RankInArtist = src.RankInArtist,
            PauseCount = src.PauseCount,
            ResumeCount = src.ResumeCount,
            SeekCount = src.SeekCount,
            LastPlayEventType = src.LastPlayEventType,
            PlayStreakDays = src.PlayStreakDays,
            EddingtonNumber = src.EddingtonNumber,
            EngagementScore = src.EngagementScore,
            TotalPlayDurationSeconds = src.TotalPlayDurationSeconds,
            RepeatCount = src.RepeatCount,
            PreviousCount = src.PreviousCount,
            RestartCount = src.RestartCount,
            DiscoveryDate = src.DiscoveryDate,

            // --- Custom Mappings (From your AutoMapper config) ---
            ArtistName = src.ArtistName,
            OtherArtistsName = src.OtherArtistsName,
            AlbumName = src.AlbumName,
            GenreName = src.Genre?.Name ?? string.Empty,


            // --- Nested Objects ---
            // Note: We use ToModelView() recursively. 
            // Warning: Your AutoMapper config IGNORED ArtistToSong list to prevent cycles. We do the same.
            ArtistToSong = src.ArtistToSong.AsEnumerable().Select(x => x.ToArtistModelView()).ToObservableCollection(),


            Artist = src.Artist?.ToArtistModelView(),
            Album = src.Album?.ToAlbumModelView(),
            Genre = src.Genre?.ToGenreModelView() ?? new GenreModelView(),

            // --- Collections ---
            // Mapping RealmLists to ObservableCollections
            PlayEvents = src.PlayHistory?.Select(x => x.ToDimmerPlayEventView()).ToObservableCollection() ?? new(),
            UserNoteAggregatedCol = src.UserNotes?.Select(x => x.ToUserNoteModelView()).ToObservableCollection() ?? new(),
            EmbeddedSync = src.EmbeddedSync?.Select(x => x.ToLyricPhraseModelView()).ToObservableCollection() ?? new(),

            // Explicit Ignores from Config:
            // PlaylistsHavingSong -> Ignored
            // ArtistToSong -> Ignored
            // HasLyricsColumnIsFiltered -> Ignored
            // IsCurrentPlayingHighlight -> Ignored
            // CurrentPlaySongDominantColor -> Ignored
        };

        // Equivalent to AfterMap logic
        // dest.RefreshDenormalizedProperties(); 

        return dest;
    }

    public static SongModel? ToSongModel(this SongModelView? src)
    {
        if (src is null) return null;

        var dest = new SongModel
        {
            Id = src.Id,
            Title = src.Title,
            OtherArtistsName = src.OtherArtistsName, // Reverse Mapping
            AlbumName = src.AlbumName,
            GenreName = src.GenreName,
            FilePath = src.FilePath,
            DurationInSeconds = src.DurationInSeconds,
            IsHidden = src.IsHidden,
            ReleaseYear = src.ReleaseYear,
            NumberOfTimesFaved = src.NumberOfTimesFaved,
            ManualFavoriteCount = src.ManualFavoriteCount,
            TrackNumber = src.TrackNumber,
            FileFormat = src.FileFormat,
            Lyricist = src.Lyricist,
            Composer = src.Composer,
            Conductor = src.Conductor,
            Description = src.Description,
            Language = src.Language,
            DiscNumber = src.DiscNumber,
            DiscTotal = src.DiscTotal,
            FileSize = src.FileSize,
            BitRate = src.BitRate,
            Rating = src.Rating,
            HasLyrics = src.HasLyrics,
            HasSyncedLyrics = src.HasSyncedLyrics,
            IsInstrumental = src.IsInstrumental,
            SyncLyrics = src.SyncLyrics,
            CoverImagePath = src.CoverImagePath,
            TrackTotal = src.TrackTotal,
            SampleRate = src.SampleRate,
            Encoder = src.Encoder,
            BitDepth = src.BitDepth,
            NbOfChannels = src.NbOfChannels,
            UnSyncLyrics = src.UnSyncLyrics,
            IsFavorite = src.IsFavorite,
            Achievement = src.Achievement,
            IsFileExists = src.IsFileExists,
            LastDateUpdated = src.LastDateUpdated,
            DateCreated = src.DateCreated,
            DeviceName = src.DeviceName,
            DeviceFormFactor = src.DeviceFormFactor,
            DeviceModel = src.DeviceModel,
            DeviceManufacturer = src.DeviceManufacturer,
            DeviceVersion = src.DeviceVersion,
            UserIDOnline = src.UserIDOnline,
            IsNew = src.IsNew,
            BPM = src.BPM,
            SongTypeValue = src.SongTypeValue,
            ParentSongId = src.ParentSongId,
            SegmentStartTime = src.SegmentStartTime,
            SegmentEndTime = src.SegmentEndTime,
            SegmentEndBehaviorValue = src.SegmentEndBehaviorValue,
            CoverArtHash = src.CoverArtHash,

            // Statistics
            PlayCount = src.PlayCount,
            PlayCompletedCount = src.PlayCompletedCount,
            SkipCount = src.SkipCount,
            LastPlayed = src.LastPlayed ?? DateTimeOffset.MinValue,
            ListenThroughRate = src.ListenThroughRate,
            SkipRate = src.SkipRate,
            FirstPlayed = src.FirstPlayed,
            PopularityScore = src.PopularityScore,
            GlobalRank = src.GlobalRank,
            RankInAlbum = src.RankInAlbum,
            RankInArtist = src.RankInArtist,
            PauseCount = src.PauseCount,
            ResumeCount = src.ResumeCount,
            SeekCount = src.SeekCount,
            LastPlayEventType = src.LastPlayEventType,
            PlayStreakDays = src.PlayStreakDays,
            EddingtonNumber = src.EddingtonNumber,
            EngagementScore = src.EngagementScore,
            TotalPlayDurationSeconds = src.TotalPlayDurationSeconds,
            RepeatCount = src.RepeatCount,
            PreviousCount = src.PreviousCount,
            RestartCount = src.RestartCount,
            DiscoveryDate = src.DiscoveryDate,

            // Explicitly Ignored Relationships (To prevent overwrite/cycle)
            // Album, Artist, Genre, PlayHistory, UserNotes, EmbeddedSync, etc. are ignored as per your Config.
        };

        // Custom Logic from AfterMap
        dest.SetTitleAndDuration(src.Title, src.DurationInSeconds);

        return dest;
    }

    // ==============================================================================
    // 📀 ALBUM MAPPERS
    // ==============================================================================

    public static AlbumModelView? ToAlbumModelView(this AlbumModel? src)
    {
        if (src is null) return null;

        return new AlbumModelView
        {
            Id = src.Id,
            Name = src.Name,
            Url = src.Url,
            ReleaseYear = src.ReleaseYear,
            IsNew = src.IsNew,
            NumberOfTracks = src.NumberOfTracks,
            TotalDuration = src.TotalDuration,
            Description = src.Description,
            ImagePath = src.ImagePath,
            DateCreated = src.DateCreated,
            DeviceName = src.DeviceName,
            DeviceFormFactor = src.DeviceFormFactor,
            DeviceModel = src.DeviceModel,
            TrackTotal = src.TrackTotal,
            DiscTotal = src.DiscTotal,
            DiscNumber = src.DiscNumber,
            DeviceManufacturer = src.DeviceManufacturer,
            DeviceVersion = src.DeviceVersion,
            AverageSongListenThroughRate = src.AverageSongListenThroughRate,
            DiscoveryDate = src.DiscoveryDate,
            CompletionPercentage = src.CompletionPercentage,
            OverallRank = src.OverallRank,
            TotalCompletedPlays = src.TotalCompletedPlays,
            EddingtonNumber = src.EddingtonNumber,
            ParetoTopSongsCount = src.ParetoTopSongsCount,
            ParetoPercentage = src.ParetoPercentage,
            TotalSkipCount = src.TotalSkipCount,
            TotalPlayDurationSeconds = src.TotalPlayDurationSeconds,
            IsFavorite = src.IsFavorite,

            // Ignores from Config
            // ImageBytes -> Ignored
            // SongsInAlbum -> Ignored
            // Artists -> Ignored
            // IsCurrentlySelected -> Ignored
        };
    }

    public static AlbumModel? ToAlbumModel(this AlbumModelView? src)
    {
        if (src is null) return null;
        return new AlbumModel
        {
            Id = src.Id,
            Name = src.Name,
            Url = src.Url,
            ReleaseYear = src.ReleaseYear,
            IsNew = src.IsNew,
            NumberOfTracks = src.NumberOfTracks,
            TotalDuration = src.TotalDuration,
            Description = src.Description,
            ImagePath = src.ImagePath,
            DateCreated = src.DateCreated,
            DeviceName = src.DeviceName,
            DeviceFormFactor = src.DeviceFormFactor,
            DeviceModel = src.DeviceModel,
            TrackTotal = src.TrackTotal,
            DiscTotal = src.DiscTotal,
            DiscNumber = src.DiscNumber,
            DeviceManufacturer = src.DeviceManufacturer,
            DeviceVersion = src.DeviceVersion,
            AverageSongListenThroughRate = src.AverageSongListenThroughRate,
            DiscoveryDate = src.DiscoveryDate,
            CompletionPercentage = src.CompletionPercentage,
            OverallRank = (int)src.OverallRank,
            TotalCompletedPlays = src.TotalCompletedPlays,
            EddingtonNumber = src.EddingtonNumber,
            ParetoTopSongsCount = src.ParetoTopSongsCount,
            ParetoPercentage = src.ParetoPercentage,
            TotalSkipCount = src.TotalSkipCount,
            TotalPlayDurationSeconds = src.TotalPlayDurationSeconds,
            IsFavorite = src.IsFavorite,

            // Relationships Ignored as per config
        };
    }

    // ==============================================================================
    // 🎤 ARTIST MAPPERS
    // ==============================================================================

    public static ArtistModelView? ToArtistModelView(this ArtistModel? src)
    {
        if (src is null) return null;

        return new ArtistModelView
        {
            Id = src.Id,
            Url = src.Url,
            Name = src.Name,
            ImagePath = src.ImagePath,
            Bio = src.Bio,
            IsNew = src.IsNew,
            IsFavorite = src.IsFavorite,
            DateCreated = src.DateCreated,
            DeviceName = src.DeviceName,
            DeviceFormFactor = src.DeviceFormFactor,
            DeviceModel = src.DeviceModel,
            DeviceManufacturer = src.DeviceManufacturer,
            DeviceVersion = src.DeviceVersion,
            CompletionPercentage = src.CompletionPercentage,
            TotalCompletedPlays = src.TotalCompletedPlays,
            AverageSongListenThroughRate = src.AverageSongListenThroughRate,
            OverallRank = src.OverallRank,
            TotalSkipCount = src.TotalSkipCount,
            DiscoveryDate = src.DiscoveryDate,
            EddingtonNumber = src.EddingtonNumber,
            ParetoTopSongsCount = src.ParetoTopSongsCount,
            ParetoPercentage = src.ParetoPercentage,
            TotalSongsByArtist = src.Songs.Count(),
            TotalAlbumsByArtist = src.Albums.Count(),

            // Ignores
            // ImageBytes, ListOfSimilarArtists, SongsByArtist, IsCurrentlySelected, IsVisible
        };
    }

    public static ArtistModel? ToArtistModel(this ArtistModelView? src)
    {
        if (src is null) return null;
        return new ArtistModel
        {
            Id = src.Id,
            Url = src.Url,
            Name = src.Name,
            ImagePath = src.ImagePath,
            Bio = src.Bio,
            IsNew = src.IsNew,
            IsFavorite = src.IsFavorite,
            DateCreated = src.DateCreated,
            DeviceName = src.DeviceName,
            DeviceFormFactor = src.DeviceFormFactor,
            DeviceModel = src.DeviceModel,
            DeviceManufacturer = src.DeviceManufacturer,
            DeviceVersion = src.DeviceVersion,
            CompletionPercentage = src.CompletionPercentage,
            TotalCompletedPlays = src.TotalCompletedPlays,
            AverageSongListenThroughRate = src.AverageSongListenThroughRate,
            OverallRank = src.OverallRank,
            TotalSkipCount = src.TotalSkipCount,
            DiscoveryDate = src.DiscoveryDate,
            EddingtonNumber = src.EddingtonNumber,
            ParetoTopSongsCount = src.ParetoTopSongsCount,
            ParetoPercentage = src.ParetoPercentage,
            TotalSongsByArtist = src.TotalSongsByArtist,
            TotalAlbumsByArtist = src.TotalAlbumsByArtist,
            
            // Relationships Ignored
        };
    }

    // ==============================================================================
    // 🏷️ GENRE MAPPERS
    // ==============================================================================

    public static GenreModelView? ToGenreModelView(this GenreModel? src)
    {
        if (src is null) return null;
        return new GenreModelView
        {
            Id = src.Id,
            Name = src.Name,
            IsNew = src.IsNew,
            DateCreated = src.DateCreated,
            DeviceName = src.DeviceName,
            DeviceFormFactor = src.DeviceFormFactor,
            DeviceModel = src.DeviceModel,
            DeviceManufacturer = src.DeviceManufacturer,
            DeviceVersion = src.DeviceVersion,
            TotalCompletedPlays = src.TotalCompletedPlays,
            AverageSongListenThroughRate = src.AverageSongListenThroughRate,
            AffinityScore = src.AffinityScore,
            OverallRank = src.OverallRank,

            // Ignores: IsCurrentlySelected
        };
    }

    public static GenreModel? ToGenreModel(this GenreModelView? src)
    {
        if (src is null) return null;
        return new GenreModel
        {
            Id = src.Id,
            Name = src.Name,
            IsNew = src.IsNew,
            DateCreated = src.DateCreated,
            DeviceName = src.DeviceName,
            DeviceFormFactor = src.DeviceFormFactor,
            DeviceModel = src.DeviceModel,
            DeviceManufacturer = src.DeviceManufacturer,
            DeviceVersion = src.DeviceVersion,
            TotalCompletedPlays = src.TotalCompletedPlays,
            AverageSongListenThroughRate = src.AverageSongListenThroughRate,
            AffinityScore = src.AffinityScore,
            OverallRank = src.OverallRank,

            // Relationships Ignored
        };
    }

    // ==============================================================================
    // 📃 PLAYLIST MAPPERS
    // ==============================================================================

    public static PlaylistModelView? ToPlaylistModelView(this PlaylistModel? src)
    {
        if (src is null) return null;
        return new PlaylistModelView
        {
            Id = src.Id,
            PlaylistName = src.PlaylistName,
            IsSmartPlaylist = src.IsSmartPlaylist,
            DateCreated = src.DateCreated.ToString("o"),
            QueryText = src.QueryText,
            IsNew = src.IsNew,
            Description = src.Description,
            CoverImagePath = src.CoverImagePath,
            User = src.User?.ToUserModelView(),

            // Collections
            SongsIdsInPlaylist = src.ManualSongIds != null ? new ObservableCollection<MongoDB.Bson.ObjectId>(src.ManualSongIds) : new(),

            // Ignores: CurrentSong, Color, PlaylistType, DeviceName (Source doesn't have it mapped explicitly in old config)
        };
    }

    public static PlaylistModel? ToPlaylistModel(this PlaylistModelView? src)
    {
        if (src is null) return null;
        return new PlaylistModel
        {
            Id = src.Id,
            PlaylistName = src.PlaylistName,
            IsSmartPlaylist = src.IsSmartPlaylist,
            QueryText = src.QueryText,
            IsNew = src.IsNew,
            Description = src.Description,
            CoverImagePath = src.CoverImagePath,
            User = src.User?.ToUserModel(),
            // DateCreated parsing might be needed if you want exact roundtrip

            // Relationships Ignored
        };
    }

    public static PlaylistEventView? ToPlaylistEventView(this PlaylistEvent? src)
    {
        if (src is null) return null;
        return new PlaylistEventView
        {
            PlayType = src.PlayType,
            // DateCreated in VM is DateTimeOffset?, in Model is string?
            DateCreated = DateTimeOffset.TryParse(src.DateCreated, out var dt) ? dt : DateTimeOffset.UtcNow,
            // EventSong -> Ignored
        };
    }

    public static PlaylistEvent? ToPlaylistEvent(this PlaylistEventView? src)
    {
        if (src is null) return null;
        return new PlaylistEvent
        {
            PlayType = src.PlayType,
            DateCreated = src.DateCreated?.ToString("o"),
            // EventSongId -> Not explicitly mapped in View
        };
    }

    // ==============================================================================
    // 👤 USER / APP STATE MAPPERS
    // ==============================================================================

    public static UserModelView? ToUserModelView(this UserModel? src)
    {
        if (src is null) return null;
        return new UserModelView
        {
            Id = src.Id,
            Username = src.UserName,
            Email = src.UserEmail,
            IsNew = src.IsNew,
            Password = src.UserPassword,
            UserProfileImage = src.UserProfileImage,
            UserBio = src.UserBio,
            UserCountry = src.UserCountry,
            UserLanguage = src.UserLanguage,
            UserTheme = src.UserTheme,
            UserDateCreated = src.UserDateCreated,
            DeviceName = src.DeviceName,
            DeviceFormFactor = src.DeviceFormFactor,
            DeviceModel = src.DeviceModel,
            DeviceManufacturer = src.DeviceManufacturer,
            DeviceVersion = src.DeviceVersion,
            LastFMAccountInfo = src.LastFMAccountInfo?.ToLastFMUserView() ?? new LastFMUserView()
        };
    }

    public static UserModel? ToUserModel(this UserModelView? src)
    {
        if (src is null) return null;
        return new UserModel
        {
            Id = src.Id,
            UserName = src.Username,
            UserEmail = src.Email,
            UserPassword = src.Password,
            IsNew = src.IsNew,
            UserProfileImage = src.UserProfileImage,
            UserBio = src.UserBio,
            UserCountry = src.UserCountry,
            UserLanguage = src.UserLanguage,
            UserTheme = src.UserTheme,
            UserDateCreated = src.UserDateCreated,
            DeviceName = src.DeviceName,
            DeviceFormFactor = src.DeviceFormFactor,
            DeviceModel = src.DeviceModel,
            DeviceManufacturer = src.DeviceManufacturer,
            DeviceVersion = src.DeviceVersion,
            LastFMAccountInfo = src.LastFMAccountInfo?.ToLastFMUser() ?? new LastFMUser()
        };
    }

    public static AppStateModelView? ToAppStateModelView(this AppStateModel? src)
    {
        if (src is null) return null;
        // Using your existing constructor logic if available, or manual map
        return new AppStateModelView(src);
    }

    public static AppStateModel? ToAppStateModel(this AppStateModelView? src)
    {
        if (src is null) return null;
        var dest = new AppStateModel
        {
            Id = src.Id,
            CurrentSongId = src.CurrentSongId,
            CurrentAlbumId = src.CurrentAlbumId,
            CurrentArtistId = src.CurrentArtistId,
            CurrentGenreId = src.CurrentGenreId,
            CurrentPlaylistId = src.CurrentPlaylistId,
            CurrentUserId = src.CurrentUserId,
            CurrentTheme = src.CurrentTheme,
            CurrentLanguage = src.CurrentLanguage,
            CurrentCountry = src.CurrentCountry,
            RepeatModePreference = src.RepeatModePreference,
            ShuffleStatePreference = src.ShuffleStatePreference,
            VolumeLevelPreference = src.VolumeLevelPreference,
            IsDarkModePreference = src.IsDarkModePreference,
            IsFirstTimeUser = src.IsFirstTimeUser,
            PlaybackSpeed = src.PlaybackSpeed,
            MinimizeToTrayPreference = src.MinimizeToTrayPreference,
            IsStickToTop = src.IsStickToTop,
            EqualizerPreset = src.EqualizerPreset,
            LastKnownPosition = src.LastKnownPosition,
            LastKnownQuery = src.LastKnownQuery,
            LastKnownPlaybackQuery = src.LastKnownPlaybackQuery,
            LastKnownPlaybackQueueIndex = src.LastKnownPlaybackQueueIndex,
            LastKnownShuffleState = src.LastKnownShuffleState,
            CurrentRepeatMode = src.CurrentRepeatMode,
            IsMiniLyricsViewEnabled = src.IsMiniLyricsViewEnabled,
            PreferredMiniLyricsViewPosition = src.PreferredMiniLyricsViewPosition,
            PreferredLyricsSource = src.PreferredLyricsSource,
            AllowLyricsContribution = src.AllowLyricsContribution,
            AllowBackNavigationWithMouseFour = src.AllowBackNavigationWithMouseFour,
        };

        // Manual collection copy
        foreach (var f in src.UserMusicFoldersPreference) dest.UserMusicFoldersPreference.Add(f);
        foreach (var w in src.LastOpenedWindows) dest.LastOpenedWindows.Add(w);

        return dest;
    }

    // ==============================================================================
    // 📜 EVENTS & OTHERS
    // ==============================================================================

    public static DimmerPlayEventView? ToDimmerPlayEventView(this DimmerPlayEvent? src)
    {
        if (src is null) return null;
        var dest = new DimmerPlayEventView
        {
            Id = src.Id,
            SongId = src.SongId,
            SongName = src.SongName,
            PlayType = src.PlayType,
            PlayTypeStr = src.PlayTypeStr,
            DatePlayed = src.DatePlayed,
            DateFinished = src.DateFinished,
            WasPlayCompleted = src.WasPlayCompleted,
            PositionInSeconds = src.PositionInSeconds,
            EventDate = src.EventDate,
            DeviceName = src.DeviceName,
            DeviceFormFactor = src.DeviceFormFactor,
            DeviceModel = src.DeviceModel,
            DeviceManufacturer = src.DeviceManufacturer,
            DeviceVersion = src.DeviceVersion,
            // IsNewOrModified -> Ignored
        };

        // Custom AfterMap Logic from your config
        if (src.SongsLinkingToThisEvent != null && src.SongsLinkingToThisEvent.Any())
        {
            var concernedSong = src.SongsLinkingToThisEvent.FirstOrDefault(x => x.Id == dest.SongId);
            if (concernedSong != null)
            {
                dest.CoverImagePath = string.IsNullOrEmpty(concernedSong.CoverImagePath)
                    ? string.Empty
                    : concernedSong.CoverImagePath;
                dest.IsFav = concernedSong.IsFavorite;
                dest.ArtistName = concernedSong.ArtistName;
                dest.AlbumName = concernedSong.AlbumName;
            }
        }

        return dest;
    }

    public static DimmerPlayEvent? ToDimmerPlayEvent(this DimmerPlayEventView? src)
    {
        if (src is null) return null;
        return new DimmerPlayEvent
        {
            Id = src.Id,
            SongId = src.SongId,
            SongName = src.SongName,
            PlayType = src.PlayType,
            PlayTypeStr = src.PlayTypeStr,
            DatePlayed = src.DatePlayed,
            DateFinished = src.DateFinished,
            WasPlayCompleted = src.WasPlayCompleted,
            PositionInSeconds = src.PositionInSeconds,
            EventDate = src.EventDate ?? DateTimeOffset.MinValue,
            DeviceName = src.DeviceName,
            DeviceFormFactor = src.DeviceFormFactor,
            DeviceModel = src.DeviceModel,
            DeviceManufacturer = src.DeviceManufacturer,
            DeviceVersion = src.DeviceVersion,

            // Ignores relationships
        };
    }

    public static LyricPhraseModelView? ToLyricPhraseModelView(this SyncLyrics? src)
    {
        if (src is null) return null;
        return new LyricPhraseModelView
        {
            TimeStampMs = src.TimestampMs,
            Text = src.Text,
            // All other properties have defaults in the ViewModel
        };
    }

    public static SyncLyrics? ToSyncLyrics(this LyricPhraseModelView? src)
    {
        if (src is null) return null;
        return new SyncLyrics(src.TimeStampMs, src.Text ?? string.Empty);
    }

    public static UserNoteModelView? ToUserNoteModelView(this UserNoteModel? src)
    {
        if (src is null) return null;
        return new UserNoteModelView
        {
            Id = src.Id,
            UserMessageText = src.UserMessageText,
            UserMessageImagePath = src.UserMessageImagePath,
            UserMessageAudioPath = src.UserMessageAudioPath,
            IsPinned = src.IsPinned,
            UserRating = src.UserRating,
            MessageColor = src.MessageColor,
            CreatedAt = src.CreatedAt,
            ModifiedAt = src.ModifiedAt
        };
    }

    public static UserNoteModel? ToUserNoteModel(this UserNoteModelView? src)
    {
        if (src is null) return null;
        return new UserNoteModel
        {
            Id = src.Id ?? TaggingUtils.GenerateId("UNote"),
            UserMessageText = src.UserMessageText,
            UserMessageImagePath = src.UserMessageImagePath,
            UserMessageAudioPath = src.UserMessageAudioPath,
            IsPinned = src.IsPinned,
            UserRating = src.UserRating,
            MessageColor = src.MessageColor,
            CreatedAt = src.CreatedAt,
            ModifiedAt = src.ModifiedAt
        };
    }

    public static LastFMUserView? ToLastFMUserView(this LastFMUser? src)
    {
        if (src is null) return null;
        return new LastFMUserView
        {
            Name = src.Name,
            RealName = src.RealName,
            Url = src.Url,
            Country = src.Country,
            Age = src.Age,
            Gender = src.Gender,
            Playcount = src.Playcount,
            Playlists = src.Playlists,
            Registered = src.Registered,
            Type = src.Type,
            Image = src.Image?.ToLastImageView() ?? new LastImageView()
        };
    }

    public static LastFMUser? ToLastFMUser(this LastFMUserView? src)
    {
        if (src is null) return null;
        return new LastFMUser
        {
            Name = src.Name,
            RealName = src.RealName,
            Url = src.Url,
            Country = src.Country,
            Age = src.Age,
            Gender = src.Gender,
            Playcount = src.Playcount,
            Playlists = src.Playlists,
            Registered = src.Registered,
            Type = src.Type,
            Image = src.Image?.ToLastImage()
        };
    }
    public static UserModel? ParseUserToRealmUser(this UserModelOnline usr)
    {
        if (usr is null) return null;
        return new UserModel
        {
            UserName = usr.Username,
            UserEmail = usr.Email,
            IsPremium = usr.IsPremium,
            UserProfileImage = usr.ProfileImagePath
        };
    }
    //public static UserModelView? ParseUserToView(this UserModelOnline usr)
    //{
    //    if (usr is null) return null;
    //    return new UserModelView
    //    {
    //        Username = usr.Username,
    //        Email = usr.Email,
    //        IsPremium = usr.IsPremium,
    //        UserProfileImage = usr.ProfileImagePath
    //    };
    //}
    public static LastFMUser? ToLastFMUser(this Hqub.Lastfm.Entities.User? src)
    {
        if (src is null) return null;
        return new LastFMUser
        {
            Name = src.Name,
            RealName = src.RealName,
            Url = src.Url,
            Country = src.Country,
            Age = src.Age,
            Gender = src.Gender,
            Playcount = src.Playcount,
            Playlists = src.Playlists,
            Registered = src.Registered,
            Type = src.Type,
            Image = src.Images.LastOrDefault().ToLastImage()
        };
    }

    public static LastImageView? ToLastImageView(this LastImage? src)
    {
        if (src is null) return null;
        return new LastImageView
        {
            Size = src.Size,
            Url = src.Url
        };
    }

    public static LastImage? ToLastImage(this LastImageView? src)
    {
        if (src is null) return null;
        return new LastImage
        {
            Size = src.Size,
            Url = src.Url
        };
    }

    public static LastImage? ToLastImage(this Hqub.Lastfm.Entities.Image? src)
    {
        if (src is null) return null;
        return new LastImage
        {
            Size = src.Size,
            Url = src.Url
        };
    }

    // Helper for Collections
    private static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> source)
    {
        return new ObservableCollection<T>(source);
    }
}