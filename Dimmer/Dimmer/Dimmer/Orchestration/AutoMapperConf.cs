// In Dimmer.Orchestration/AutoMapperConf.cs

// Assuming other necessary using statements for your ViewModels etc.

namespace Dimmer.Orchestration;

public static class AutoMapperConf
{
    public static IMapper ConfigureAutoMapper()
    {
        var config = new MapperConfiguration(cfg =>
        {
            // =====================================================================
            // SECTION 1: MAPPINGS BETWEEN VIEWMODELS/DTOS AND REALM MODELS
            // =====================================================================
            // These are for general application use (e.g., UI binding).

            cfg.CreateMap<SongModel, SongModelView>().ReverseMap().PreserveReferences();
            cfg.CreateMap<AlbumModel, AlbumModelView>().ReverseMap().PreserveReferences();
            cfg.CreateMap<ArtistModel, ArtistModelView>().ReverseMap().PreserveReferences();
            cfg.CreateMap<GenreModel, GenreModelView>().ReverseMap().PreserveReferences();
            cfg.CreateMap<PlaylistModel, PlaylistModelView>().ReverseMap().PreserveReferences();
            cfg.CreateMap<AppStateModel, AppStateModelView>().ReverseMap().PreserveReferences();
            // ... and your other ViewModel maps ...


            // =====================================================================
            // SECTION 2: MAPPINGS FOR REALM PERSISTENCE LOGIC
            // These are specifically to support your safe, optimal persistence code.
            // =====================================================================

            // --- PART A: Creating Unmanaged Deep Copies of Realm Objects ---
            // Goal: To create a full, unmanaged copy of an object from another Realm instance.
            // Used when you call `mapper.Map<T>(source)`.

            // For Album, Artist, and Genre, we just need to ignore the backlinks.
            cfg.CreateMap<AlbumModel, AlbumModel>()
                .ForMember(dest => dest.SongsInAlbum, opt => opt.Ignore()) // Backlink
                .ForMember(dest => dest.ArtistIds, opt => opt.Ignore()) // To-many relationship
                .ForMember(dest => dest.Tags, opt => opt.Ignore()) // To-many relationship
                .ForMember(dest => dest.UserNotes, opt => opt.Ignore()); // Embedded list

            cfg.CreateMap<ArtistModel, ArtistModel>()
               .ForMember(dest => dest.Songs, opt => opt.Ignore()) // Backlink
               .ForMember(dest => dest.Albums, opt => opt.Ignore()) // Backlink
               .ForMember(dest => dest.Tags, opt => opt.Ignore()) // To-many relationship
               .ForMember(dest => dest.UserNotes, opt => opt.Ignore()); // Embedded list

            cfg.CreateMap<GenreModel, GenreModel>()
               .ForMember(dest => dest.Songs, opt => opt.Ignore()); // Backlink

            // This is now a "shallow" copy map
            cfg.CreateMap<SongModel, SongModel>()
               .ForMember(dest => dest.Album, opt => opt.Ignore())
               .ForMember(dest => dest.Genre, opt => opt.Ignore())
               .ForMember(dest => dest.AlbumImageBytes, opt => opt.Ignore())
               .ForMember(dest => dest.CoverImageBytes, opt => opt.Ignore())
               .ForMember(dest => dest.ArtistImageBytes, opt => opt.Ignore())
               .ForMember(dest => dest.ArtistIds, opt => opt.Ignore())
               .ForMember(dest => dest.UserNotes, opt => opt.Ignore())
               .ForMember(dest => dest.PlayHistory, opt => opt.Ignore())
               .ForMember(dest => dest.Tags, opt => opt.Ignore())
               .ForMember(dest => dest.EmbeddedSync, opt => opt.Ignore())
               .ForMember(dest => dest.Playlists, opt => opt.Ignore());

            // Self-map for any other types you might need to copy.
            cfg.CreateMap<AppStateModel, AppStateModel>();
            cfg.CreateMap<DimmerPlayEventView, DimmerPlayEvent>()
            .ForMember(dest =>
                dest.SongsLinkingToThisEvent,
                opt => opt.Ignore())
            .ReverseMap();


            // --- PART B: Updating a Managed Realm Object from a ViewModel ---
            // Goal: To update an existing, managed object with new primitive data,
            // while leaving its relationships untouched so we can manage them manually.
            // Used when you call `mapper.Map(sourceViewModel, destinationModel)`.

            cfg.CreateMap<SongModelView, SongModel>()
             //.ForMember(dest => dest.Id, opt => opt.Ignore())
             .ForMember(dest => dest.Album, opt => opt.Ignore())
             .ForMember(dest => dest.Genre, opt => opt.Ignore())
             .ForMember(dest => dest.ArtistIds, opt => opt.Ignore())
             .ForMember(dest => dest.UserNotes, opt => opt.Ignore())
             .ForMember(dest => dest.PlayHistory, opt => opt.Ignore())
             .ForMember(dest => dest.CoverImageBytes, opt => opt.Ignore())
             .ForMember(dest => dest.AlbumImageBytes, opt => opt.Ignore())
             .ForMember(dest => dest.ArtistImageBytes, opt => opt.Ignore())
             .ForMember(dest => dest.Tags, opt => opt.Ignore())
             .ForMember(dest => dest.EmbeddedSync, opt => opt.Ignore())
             .ForMember(dest => dest.Playlists, opt => opt.Ignore());
        });

        // This is your best friend. It will throw a detailed exception at startup
        // if any of your mappings are invalid, saving hours of debugging.
        //config.AssertConfigurationIsValid();

        return config.CreateMapper();
    }
}



//// In Dimmer.Orchestration/AutoMapperConf.cs
//using AutoMapper;
//using Dimmer.Data.Models;
//using Dimmer.Data.ModelView; // Your ViewModel namespace
//using System;
//using System.Linq;
//using Realms;
//// Add this for the detailed error message builder
//using System.Text;

//namespace Dimmer.Orchestration;

//public static class AutoMapperConf
//{
//    public static IMapper ConfigureAutoMapper()
//    {

//        var config = new MapperConfiguration(cfg =>
//        {
//            // =====================================================================
//            // SECTION 1: MAPPINGS FROM REALM MODELS TO VIEWMODELS
//            // =====================================================================
//            // These are for displaying data in the UI. We must account for every
//            // property in the destination ViewModel.

//            // --- Primary Data Mappings ---
//            // In SECTION 2 of AutoMapperConf.cs

//            cfg.CreateMap<SongModelView, SongModel>()
//                // Ignore all relationships and collections that are managed by Realm
//                .ForMember(dest => dest.Album, opt => opt.Ignore())
//                .ForMember(dest => dest.Artist, opt => opt.Ignore())
//                .ForMember(dest => dest.Genre, opt => opt.Ignore())
//                .ForMember(dest => dest.ArtistIds, opt => opt.Ignore())
//                .ForMember(dest => dest.Tags, opt => opt.Ignore())
//                .ForMember(dest => dest.PlayHistory, opt => opt.Ignore())
//                .ForMember(dest => dest.Playlists, opt => opt.Ignore())
//                .ForMember(dest => dest.UserNotes, opt => opt.Ignore())

//                // Ignore embedded objects that are complex to map back
//                .ForMember(dest => dest.EmbeddedSync, opt => opt.Ignore())

//                // Ignore properties that are not part of the ViewModel's responsibility
//                .ForMember(dest => dest.AlbumImageBytes, opt => opt.Ignore())
//                .ForMember(dest => dest.ArtistImageBytes, opt => opt.Ignore())

//                // Ignore Realm-specific properties
//                .ForMember(dest => dest.Id, opt => opt.Ignore()) // Usually you don't map the ID back
//                .ForMember(dest => dest.IsNew, opt => opt.Ignore())
//                .ForMember(dest => dest.ObjectSchema, opt => opt.Ignore())
//                .ForMember(dest => dest.EmbeddedSync, opt => opt.MapFrom(src => src.EmbeddedSync))
//                .PreserveReferences();

//            cfg.CreateMap<SongModel, SongModelView>()
//                .ForMember(dest => dest.ArtistIds, opt => opt.MapFrom(src => src.ArtistIds))
//                //.ForMember(dest => dest.Album, opt => opt.MapFrom(src => src.Album))
//                //.ForMember(dest => dest.Genre, opt => opt.MapFrom(src => src.Genre))
//                //.ForMember(dest => dest.GenreName, opt => opt.MapFrom(src => src.Genre != null ? src.Genre.Name : "Unknown"))
//                .ForMember(dest => dest.PlayEvents, opt => opt.MapFrom(src => src.PlayHistory))
//                .ForMember(dest => dest.UserNote, opt => opt.MapFrom(src => src.UserNotes))
//                .ForMember(dest => dest.EmbeddedSync, opt => opt.MapFrom(src => src.EmbeddedSync))
//                .ForMember(dest => dest.IsPlaying, opt => opt.Ignore())
//                .ForMember(dest => dest.IsCurrentPlayingHighlight, opt => opt.Ignore())
//                .ForMember(dest => dest.AllArtists, opt => opt.Ignore())
//                .ForMember(dest => dest.AllAlbums, opt => opt.Ignore())
//                .PreserveReferences();

//            cfg.CreateMap<AlbumModel, AlbumModelView>()
//                .ForMember(dest => dest.Songs, opt => opt.Ignore())
//                .ForMember(dest => dest.ImageBytes, opt => opt.Ignore())
//                .ForMember(dest => dest.IsCurrentlySelected, opt => opt.Ignore())
//                .PreserveReferences();

//            cfg.CreateMap<ArtistModel, ArtistModelView>()
//                .ForMember(dest => dest.ImageBytes, opt => opt.Ignore())
//                .ForMember(dest => dest.IsCurrentlySelected, opt => opt.Ignore())
//                .ForMember(dest => dest.IsVisible, opt => opt.Ignore())
//                .PreserveReferences();

//            cfg.CreateMap<GenreModel, GenreModelView>()
//                .ForMember(dest => dest.IsCurrentlySelected, opt => opt.Ignore())
//                .PreserveReferences();

//            cfg.CreateMap<PlaylistModel, PlaylistModelView>()
//                .ForMember(dest => dest.DateCreated, opt => opt.MapFrom(src => src.DateCreated.ToString("o")))
//                .ForMember(dest => dest.SongInPlaylist, opt => opt.MapFrom(src => src.SongsInPlaylist))
//                .ForMember(dest => dest.CurrentSong, opt => opt.Ignore())
//                .PreserveReferences();

//            cfg.CreateMap<UserModel, UserModelView>()
//                .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.UserName))
//                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.UserEmail))
//                .ForMember(dest => dest.Password, opt => opt.MapFrom(src => src.UserPassword))
//                .ForMember(dest => dest.UserHasAccount, opt => opt.Ignore())
//                .PreserveReferences();

//            // --- App State & Events Mappings ---

//            cfg.CreateMap<AppStateModel, AppStateModelView>()
//                .ForMember(dest => dest.IsShowCloseConfirmation, opt => opt.Ignore())
//                .PreserveReferences();

//            cfg.CreateMap<DimmerPlayEvent, DimmerPlayEventView>()
//                .ForMember(dest => dest.IsNewOrModified, opt => opt.Ignore())
//                .PreserveReferences();

//            // --- Embedded Object & Sub-Type Mappings ---

//            cfg.CreateMap<SyncLyrics, SyncLyricsView>();
//            cfg.CreateMap<UserNoteModel, UserNoteModelView>();
//            cfg.CreateMap<PlaylistEvent, PlaylistEventView>()
//                .ForMember(dest => dest.EventSong, opt => opt.Ignore());

//            // =====================================================================
//            // SECTION 2: MAPPINGS FROM VIEWMODELS BACK TO REALM MODELS
//            // =====================================================================
//            // For updating data. We only map simple properties and ignore ALL relationships.

//            // cfg.CreateMap<SongModelView, SongModel>() ...

//            cfg.CreateMap<AlbumModelView, AlbumModel>()
//                .ForMember(dest => dest.Id, opt => opt.Ignore())
//                .ForMember(dest => dest.ArtistIds, opt => opt.Ignore())
//                .ForMember(dest => dest.SongsInAlbum, opt => opt.Ignore())
//                .ForMember(dest => dest.Tags, opt => opt.Ignore())
//                .ForMember(dest => dest.UserNotes, opt => opt.Ignore())
//                // FIX: Ignore Realm and other unmapped properties
//                .ForMember(dest => dest.IsNew, opt => opt.Ignore())
//                .ForMember(dest => dest.TrackTotal, opt => opt.Ignore())
//                .ForMember(dest => dest.DiscTotal, opt => opt.Ignore())
//                .ForMember(dest => dest.DiscNumber, opt => opt.Ignore())
//                .ForMember(dest => dest.ObjectSchema, opt => opt.Ignore());

//            cfg.CreateMap<ArtistModelView, ArtistModel>()
//                .ForMember(dest => dest.Id, opt => opt.Ignore())
//                .ForMember(dest => dest.Songs, opt => opt.Ignore())
//                .ForMember(dest => dest.Albums, opt => opt.Ignore())
//                .ForMember(dest => dest.Tags, opt => opt.Ignore())
//                .ForMember(dest => dest.UserNotes, opt => opt.Ignore())
//                // FIX: Ignore Realm and other unmapped properties
//                .ForMember(dest => dest.IsNew, opt => opt.Ignore())
//                .ForMember(dest => dest.ImagePath, opt => opt.Ignore())
//                .ForMember(dest => dest.ObjectSchema, opt => opt.Ignore());

//            cfg.CreateMap<DimmerPlayEventView, DimmerPlayEvent>()
//                .ForMember(dest => dest.Id, opt => opt.Ignore())
//                .ForMember(dest => dest.SongsLinkingToThisEvent, opt => opt.Ignore())
//                // FIX: Ignore Realm-specific properties
//                .ForMember(dest => dest.IsNew, opt => opt.Ignore())
//                .ForMember(dest => dest.ObjectSchema, opt => opt.Ignore());

//            cfg.CreateMap<AppStateModelView, AppStateModel>()
//                .ForMember(dest => dest.IsNew, opt => opt.Ignore())
//                // FIX: Ignore the Realm-specific property
//                .ForMember(dest => dest.ObjectSchema, opt => opt.Ignore());

//            // =====================================================================
//            // SECTION 3: MAPPINGS FOR UNMANAGED REALM OBJECT CLONING
//            // =====================================================================
//            // For creating thread-safe copies. Ignore all relationships and backlinks.

//            cfg.CreateMap<AlbumModel, AlbumModel>()
//                .ForMember(dest => dest.ArtistIds, opt => opt.Ignore())
//                .ForMember(dest => dest.SongsInAlbum, opt => opt.Ignore())
//                .ForMember(dest => dest.Tags, opt => opt.Ignore())
//                .ForMember(dest => dest.UserNotes, opt => opt.Ignore());

//            cfg.CreateMap<ArtistModel, ArtistModel>()
//               .ForMember(dest => dest.Songs, opt => opt.Ignore())
//               .ForMember(dest => dest.Albums, opt => opt.Ignore())
//               .ForMember(dest => dest.Tags, opt => opt.Ignore())
//               .ForMember(dest => dest.UserNotes, opt => opt.Ignore());

//            cfg.CreateMap<GenreModel, GenreModel>()
//               .ForMember(dest => dest.Songs, opt => opt.Ignore())
//               .ForMember(dest => dest.UserNotes, opt => opt.Ignore());

//            cfg.CreateMap<SongModel, SongModel>()
//               .ForMember(dest => dest.Album, opt => opt.Ignore())
//               .ForMember(dest => dest.Artist, opt => opt.Ignore())
//               .ForMember(dest => dest.Genre, opt => opt.Ignore())
//               .ForMember(dest => dest.ArtistIds, opt => opt.Ignore())
//               .ForMember(dest => dest.Tags, opt => opt.Ignore())
//               .ForMember(dest => dest.EmbeddedSync, opt => opt.Ignore())
//               .ForMember(dest => dest.PlayHistory, opt => opt.Ignore())
//               .ForMember(dest => dest.Playlists, opt => opt.Ignore())
//               .ForMember(dest => dest.UserNotes, opt => opt.Ignore());

//            cfg.CreateMap<AppStateModel, AppStateModel>();
//        });

//        try
//        {
//            config.AssertConfigurationIsValid();
//        }
//        catch (AutoMapperConfigurationException ex)
//        {
//            var builder = new StringBuilder();
//            builder.AppendLine("========== AutoMapper Configuration Error ==========");
//            builder.AppendLine(ex.Message);

//            foreach (var error in ex.Errors)
//            {
//                builder.AppendLine($"\n----- Invalid Map: {error.TypeMap.SourceType.Name} -> {error.TypeMap.DestinationType.Name} -----");

//                if (error.UnmappedPropertyNames?.Any() == true)
//                {
//                    builder.AppendLine("Unmapped properties:");
//                    foreach (var propName in error.UnmappedPropertyNames)
//                    {
//                        builder.AppendLine($"  - {propName}");
//                    }
//                }
//            }
//            builder.AppendLine("======================================================");

//            Console.WriteLine(builder.ToString());

//            throw;
//        }

//        return config.CreateMapper();
//    }
//}