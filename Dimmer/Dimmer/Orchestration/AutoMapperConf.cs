// In Dimmer.Orchestration/AutoMapperConf.cs

// Assuming other necessary using statements for your ViewModels etc.

using static Dimmer.Data.Models.LastFMUser;
using static Dimmer.Data.ModelView.LastFMUserView;

namespace Dimmer.Orchestration;

public static class AutoMapperConf
{
    public static IMapper ConfigureAutoMapper()
    {
        try
        {

            var config = new MapperConfiguration(cfg =>
            {
            // =====================================================================
            // SECTION 1: MAPPINGS FROM REALM MODELS TO VIEWMODELS
            // =====================================================================
            // These are explicit to prevent threading issues and silent failures.

            cfg.CreateMap<SongModel, SongModelView>()
                .ForMember(dest => dest.ArtistName, opt => opt.MapFrom(src => src.OtherArtistsName))
                .ForMember(dest => dest.AlbumName, opt => opt.MapFrom(src => src.AlbumName))
                .ForMember(dest => dest.GenreName, opt => opt.MapFrom(src => src.Genre.Name))
                .ForMember(dest => dest.Album, opt => opt.Ignore()) // Must ignore RealmObject properties
                .ForMember(dest => dest.Genre, opt => opt.Ignore())
                .ForMember(dest => dest.PlayEvents, opt => opt.MapFrom(src => src.PlayHistory))
                .ForMember(dest => dest.UserNotes, opt => opt.Ignore())
                .ForMember(dest => dest.EmbeddedSync, opt => opt.MapFrom(src => src.EmbeddedSync))
                //.ForMember(dest => dest.IsPlaying, opt => opt.Ignore())
                .ForMember(dest => dest.IsCurrentPlayingHighlight, opt => opt.Ignore())
                .ForMember(dest => dest.SearchableText, opt => opt.Ignore()) // This is computed in AfterMap
                .ForMember(dest => dest.CurrentPlaySongDominantColor, opt => opt.Ignore()) // This is computed in AfterMap
                .ForMember(dest => dest.ArtistToSong, opt => opt.MapFrom(src => src.ArtistToSong))
                .AfterMap((src, dest) =>
                {
                    dest.PrecomputeSearchableText();

                    const int maxTitleLength = 200; // A safe length to avoid path issues. Adjust as needed.

                    // Check if the Title is not null and exceeds the max length
                    if (!string.IsNullOrEmpty(dest.Title) && dest.Title.Length > maxTitleLength)
                    {
                        // Truncate the string and add an ellipsis
                        dest.Title = string.Concat(dest.Title.AsSpan(0, maxTitleLength), "...");
                    }
                    });

            cfg.CreateMap<AlbumModel, AlbumModelView>()
              .ForMember(dest => dest.ImageBytes, opt => opt.Ignore()) // Handle this manually
                    .ForMember(dest => dest.Songs, opt => opt.Ignore()) // Ignore backlink
              .ForMember(dest => dest.IsCurrentlySelected, opt => opt.Ignore());

            cfg.CreateMap<ArtistModel, ArtistModelView>()
                .ForMember(dest => dest.ImageBytes, opt => opt.Ignore()) // Handle this manually
                .ForMember(dest => dest.IsCurrentlySelected, opt => opt.Ignore())
                .ForMember(dest => dest.IsVisible, opt => opt.Ignore());

            cfg.CreateMap<GenreModel, GenreModelView>()
                .ForMember(dest => dest.IsCurrentlySelected, opt => opt.Ignore());

            cfg.CreateMap<PlaylistModel, PlaylistModelView>()
                .ForMember(dest => dest.SongInPlaylist, opt => opt.Ignore()) // Cannot map RealmObject list to another
                .ForMember(dest => dest.SongsIdsInPlaylist, opt => opt.MapFrom(src => src.ManualSongIds))
                .ForMember(dest => dest.CurrentSong, opt => opt.Ignore())
                .ForMember(dest => dest.Color, opt => opt.Ignore()) // Assuming this is UI state
                .ForMember(dest => dest.PlaylistType, opt => opt.Ignore())
                .ForMember(dest => dest.PlaylistEvents, opt => opt.Ignore())
                .ForMember(dest => dest.DeviceName, opt => opt.Ignore());

            cfg.CreateMap<AppStateModel, AppStateModelView>()
                .ForMember(dest => dest.IsShowCloseConfirmation, opt => opt.Ignore());

            cfg.CreateMap<UserModel, UserModelView>()
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.UserEmail))
                .ForMember(dest => dest.UserHasAccount, opt => opt.Ignore())
                .ForMember(dest => dest.ProfileImageFile, opt => opt.Ignore())
                .ForMember(dest => dest.Password, opt => opt.MapFrom(src => src.UserPassword));

            cfg.CreateMap<DimmerPlayEvent, DimmerPlayEventView>()
.ForMember(dest => dest.IsNewOrModified, opt => opt.Ignore());

            cfg.CreateMap<SyncLyrics, SyncLyricsView>()
                .ForMember(dest => dest.TimestampEnd, opt => opt.Ignore())
                .ForMember(dest => dest.Beats, opt => opt.Ignore())
                .ForMember(dest => dest.IsLyricSynced, opt => opt.Ignore());

            cfg.CreateMap<PlaylistEvent, PlaylistEventView>()
                .ForMember(dest => dest.EventSong, opt => opt.Ignore());

            cfg.CreateMap<UserNoteModel, UserNoteModelView>();
            cfg.CreateMap<LastFMUser, LastFMUserView>();
            cfg.CreateMap<LastImage, LastImageView>();

            // =====================================================================
            // SECTION 2: MAPPINGS FROM VIEWMODELS BACK TO REALM MODELS
            // =====================================================================
            // For saving data. Ignore all relationships and Realm-specific properties.

            cfg.CreateMap<SongModelView, SongModel>()
                .ForMember(dest => dest.Album, opt => opt.Ignore())
                .ForMember(dest => dest.Artist, opt => opt.Ignore())
                .ForMember(dest => dest.Genre, opt => opt.Ignore())
                .ForMember(dest => dest.ArtistToSong, opt => opt.Ignore())
                .ForMember(dest => dest.Tags, opt => opt.Ignore())
                .ForMember(dest => dest.PlayHistory, opt => opt.Ignore())
                //.ForMember(dest => dest.UserNotes, opt => opt.Ignore())
                                .ForMember(dest => dest.UserNotes, opt => opt.Ignore())
                .ForMember(dest => dest.EmbeddedSync, opt => opt.Ignore())
                .ForMember(dest => dest.CoverImageBytes, opt => opt.Ignore())
                .ForMember(dest => dest.ArtistImageBytes, opt => opt.Ignore())
                .ForMember(dest => dest.AlbumImageBytes, opt => opt.Ignore())


 .ForMember(dest => dest.ObjectSchema, opt => opt.Ignore())
                .ForMember(dest => dest.LyricsText, opt => opt.Ignore())
                .AfterMap((src, dest) => dest.SetTitleAndDuration(src.Title, src.DurationInSeconds));

            cfg.CreateMap<DimmerPlayEventView, DimmerPlayEvent>()
                .ForMember(dest => dest.IsNew, opt => opt.Ignore())
                .ForMember(dest => dest.SongsLinkingToThisEvent, opt => opt.Ignore())

.ForMember(dest => dest.ObjectSchema, opt => opt.Ignore());

            // Self-maps for creating unmanaged copies
            cfg.CreateMap<AlbumModel, AlbumModel>()
            .ForMember(dest => dest.SongsInAlbum, opt => opt.Ignore())
              .ForMember(dest => dest.ArtistIds, opt => opt.Ignore()) // Handle this manually
              .ForMember(dest => dest.UserNotes, opt => opt.Ignore());



            cfg.CreateMap<ArtistModel, ArtistModel>()
            .ForMember(dest => dest.Songs, opt => opt.Ignore())
            .ForMember(dest => dest.Albums, opt => opt.Ignore());


            cfg.CreateMap<GenreModel, GenreModel>().ForMember(dest => dest.Songs, opt => opt.Ignore());
            cfg.CreateMap<SongModel, SongModel>(); // Add others as needed
            cfg.CreateMap<AppStateModel, AppStateModel>();



            cfg.CreateMap<AlbumModelView, AlbumModel>()
                .ForMember(dest => dest.SongsInAlbum, opt => opt.Ignore()) // Must ignore RealmList/backlinks
                .ForMember(dest => dest.UserNotes, opt => opt.Ignore())    // Must ignore Realm relationships
                .ForMember(dest => dest.ArtistIds, opt => opt.Ignore())   // This is likely managed separately
                .ForMember(dest => dest.Tags, opt => opt.Ignore())   // This is likely managed separately
                .ForMember(dest => dest.Artist, opt => opt.Ignore())   // This is likely managed separately
                .ForMember(dest => dest.ObjectSchema, opt => opt.Ignore()); // Ignore Realm-specific property

            cfg.CreateMap<ArtistModelView, ArtistModel>()
                .ForMember(dest => dest.Songs, opt => opt.Ignore())       // Must ignore RealmList
                .ForMember(dest => dest.Albums, opt => opt.Ignore())      // Must ignore RealmList
                .ForMember(dest => dest.Tags, opt => opt.Ignore())      // Must ignore RealmList
                .ForMember(dest => dest.UserNotes, opt => opt.Ignore())      // Must ignore RealmList
                .ForMember(dest => dest.ObjectSchema, opt => opt.Ignore()); // Ignore Realm-specific property

            cfg.CreateMap<GenreModelView, GenreModel>()
                .ForMember(dest => dest.Songs, opt => opt.Ignore())       // Must ignore backlinks
                .ForMember(dest => dest.UserNotes, opt => opt.Ignore())       // Must ignore backlinks
                    .ForMember(dest => dest.ObjectSchema, opt => opt.Ignore()); // Ignore Realm-specific property


                cfg.CreateMap<UserNoteModelView, UserNoteModel>()
                .ForMember(dest => dest.IsManaged, opt => opt.Ignore()) // Ignore relationships
                .ForMember(dest => dest.ObjectSchema, opt => opt.Ignore()) // Ignore relationships
            ;
            });


            // This should now pass without errors.
            config.AssertConfigurationIsValid();

            return config.CreateMapper();
        
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);

            throw new InvalidOperationException("AutoMapper configuration failed.", ex);
        }
    }
}


