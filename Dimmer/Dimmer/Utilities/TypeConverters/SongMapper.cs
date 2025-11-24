using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Utilities.TypeConverters;

public static class SongMapper
{
    public static SongModelView ToSongView(this SongModel src)
    {
        if (src == null) return null;

        var dest = new SongModelView
        {
            Id = src.Id,
            Title = src.Title,
            DurationInSeconds = src.DurationInSeconds,
            FilePath = src.FilePath,
            CoverImagePath = src.CoverImagePath,
            ArtistName = src.OtherArtistsName,
            AlbumName = src.AlbumName,
            GenreName = src.GenreName,

            // Basic Stats
            PlayCount = src.PlayCount,
            PlayCompletedCount = src.PlayCompletedCount,
            SkipCount = src.SkipCount,
            IsFavorite = src.IsFavorite,
            Rating = src.Rating,
            DateCreated = src.DateCreated,
            FileFormat = src.FileFormat,
            FileSize = src.FileSize,
            BitRate = src.BitRate,
            SampleRate = src.SampleRate,
            HasLyrics = src.HasLyrics,
            HasSyncedLyrics = src.HasSyncedLyrics,
            IsInstrumental = (bool)(src.IsInstrumental ?? false),

            // Manual Collections (Avoid RealmLists in UI)
            // We map to empty collections first to avoid heavy recursion
            PlayEvents = new ObservableCollection<DimmerPlayEventView>(),

            // Copy specific properties needed for UI
            TitleDurationKey = src.TitleDurationKey
        };

        // Custom Logic from your old AutoMapper config
        dest.RefreshDenormalizedProperties();

        return dest;
    }

    // Helper for Lists
    public static List<SongModelView> ToSongViewList(this IEnumerable<SongModel> sources)
    {
        if (sources == null) return new List<SongModelView>();
        // .ToList() forces execution before mapping
        return sources.Select(s => s.ToSongView()).ToList();
    }
}