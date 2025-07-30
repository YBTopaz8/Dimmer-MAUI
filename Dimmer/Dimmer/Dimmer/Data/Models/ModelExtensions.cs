namespace Dimmer.Data.Models;
public static class ModelExtensions
{
    public static SongModelView ToViewModel(this SongModel src)
    {
        if (src is null)
        {
            return new SongModelView();
        }

        var dest = new SongModelView
        {
            Id = src.Id,
            Title = src.Title,
            ArtistName = src.ArtistName,
            OtherArtistsName = src.OtherArtistsName,
            AlbumName = src.AlbumName,
            GenreName = src.Genre?.Name,
            FilePath = src.FilePath,
            DurationInSeconds = src.DurationInSeconds,
            ReleaseYear = src.ReleaseYear,
            TrackNumber = src.TrackNumber,
            FileFormat = src.FileFormat,
            FileSize = src.FileSize,
            BitRate = src.BitRate,
            Rating = src.Rating,
            HasLyrics = src.HasLyrics,
            HasSyncedLyrics = src.HasSyncedLyrics,
            SyncLyrics = src.SyncLyrics ?? string.Empty,
            CoverImagePath = src.CoverImagePath is null ? "musicnote.png" : src.CoverImagePath,
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
            Lyricist = src.Lyricist,
            BPM = src.BPM,
            Composer = src.Composer,
            Conductor = src.Conductor,
            Description = src.Description,
            Language = src.Language,
            DiscNumber = src.DiscNumber,
            DiscTotal = src.DiscTotal,
            UserIDOnline = src.UserIDOnline,
            IsNew = src.IsNew
            ,
            UserNotes = src.UserNotes.Select(x=> new UserNoteModelView() { UserMessageText=x.UserMessageText,
            CreatedAt=x.CreatedAt,ModifiedAt=x.ModifiedAt}).ToObservableCollection()
        };

        dest.PrecomputeSearchableText();

        return dest;
    }
}