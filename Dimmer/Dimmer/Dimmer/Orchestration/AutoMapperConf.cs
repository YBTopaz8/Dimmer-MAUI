// In Dimmer.Orchestration/AutoMapperConf.cs

using AutoMapper;

using Dimmer.Data.Models;
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
            cfg.CreateMap<DimmerPlayEventView, DimmerPlayEvent>().ReverseMap();


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