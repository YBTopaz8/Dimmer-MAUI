// Assuming your Realm model classes (AlbumModel, ArtistModel, etc.) are in a namespace
// that's either directly accessible or via a 'using Dimmer.DimmerLive.Models;' statement.
// Also assuming your ViewModel classes (AlbumModelView, etc.) are similarly accessible.
// You'll also need: using AutoMapper;

namespace Dimmer.Orchestration;

public static class AutoMapperConf
{
    public static IMapper ConfigureAutoMapper()
    {
        var config = new MapperConfiguration(cfg =>
        {
            // --- Mappings for DTOs/ViewModels and other transformations ---

            cfg.CreateMap<DimmerPlayEvent, PlayDataLink>()
               .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
               .ForMember(dest => dest.SongId, opt => opt.MapFrom(src => src.SongId.HasValue ? src.SongId.ToString() : null))
               .ForMember(dest => dest.PlayType, opt => opt.MapFrom(src => src.PlayType))
               .ForMember(dest => dest.DateStarted, opt => opt.MapFrom(src => src.DatePlayed.UtcDateTime))
               .ForMember(dest => dest.DateFinished, opt => opt.MapFrom(src => src.DateFinished.UtcDateTime))
               .ForMember(dest => dest.WasPlayCompleted, opt => opt.MapFrom(src => src.WasPlayCompleted))
               .ForMember(dest => dest.PositionInSeconds, opt => opt.MapFrom(src => src.PositionInSeconds))
               .ForMember(dest => dest.EventDate, opt => opt.MapFrom(src => (src.EventDate ?? DateTimeOffset.UtcNow).UtcDateTime));

            cfg.CreateMap<SongModel, SongModelView>()
               .PreserveReferences().ReverseMap().PreserveReferences();

            cfg.CreateMap<DimmerPlayEventView, DimmerPlayEvent>()
               .ForMember(dest => dest.SongsLinkingToThisEvent, opt => opt.Ignore())
               .PreserveReferences().ReverseMap().PreserveReferences();

            cfg.CreateMap<DimmerPlayEvent, DimmerPlayEventView>()
               .PreserveReferences().ReverseMap().PreserveReferences();

            cfg.CreateMap<AlbumModel, AlbumModelView>()
               .PreserveReferences(); // One direction

            cfg.CreateMap<AlbumModelView, AlbumModel>() // Other direction
               .ForMember(dest => dest.Name, opt => opt.Ignore()) // Assuming Name is PK or not to be set from View
               .PreserveReferences();

            cfg.CreateMap<ArtistModel, ArtistModelView>()
               .PreserveReferences().ReverseMap().PreserveReferences();

            cfg.CreateMap<UserNoteModel, UserNoteModelView>().ReverseMap();
            cfg.CreateMap<UserNoteModel, UserModelOnline>().ReverseMap();
            cfg.CreateMap<UserModelOnline, UserModelView>().ReverseMap();
            cfg.CreateMap<UserModel, UserModelView>().ReverseMap();
            cfg.CreateMap<GenreModel, GenreModelView>().ReverseMap();
            cfg.CreateMap<PlaylistModel, PlaylistModelView>().ReverseMap();
            cfg.CreateMap<AppStateModel, AppStateModelView>().ReverseMap();
            cfg.CreateMap<LyricPhraseModel, LyricPhraseModelView>().ReverseMap();

            // --- Self-mappings for creating unmanaged copies of Realm objects ---
            // These are crucial for the "managed by another live Realm" scenario.

            cfg.CreateMap<AlbumModel, AlbumModel>()
       .ConstructUsing(src => new AlbumModel())
       // Ignore the backlink IQueryable<SongModel>
       .ForMember(dest => dest.SongsInAlbum, opt => opt.Ignore())
       // If you have any other navigation lists on AlbumModel, ignore them too:
       .PreserveReferences();

            cfg.CreateMap<ArtistModel, ArtistModel>()
               .ConstructUsing(src => new ArtistModel())
       .ForMember(dest => dest.Songs, opt => opt.Ignore())
       .ForMember(dest => dest.Albums, opt => opt.Ignore())

               .PreserveReferences();

            cfg.CreateMap<GenreModel, GenreModel>()
               .ConstructUsing(src => new GenreModel())
                      .ForMember(dest => dest.Songs, opt => opt.Ignore())

               .PreserveReferences();

            cfg.CreateMap<SongModel, SongModel>()
               .ConstructUsing(src => new SongModel())
               .PreserveReferences();

            cfg.CreateMap<AppStateModel, AppStateModel>()
               .ConstructUsing(src => new AppStateModel())
               .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id)) // Explicit PK mapping if needed, good practice
               .PreserveReferences(); // If AppStateModel has complex children or references

            // Add self-mappings for any other RealmObject types you might pass through
            // the ProcessTopLevelEntity helper or need to create unmanaged copies of.
            // Example:
            // cfg.CreateMap<YourOtherRealmObjectModel, YourOtherRealmObjectModel>()
            //    .ConstructUsing(src => new YourOtherRealmObjectModel())
            //    .PreserveReferences();
        });

        // It's highly recommended to uncomment this during development and for unit tests.
        // This will throw an exception at startup if any configurations are invalid.
        //config.AssertConfigurationIsValid();

        return config.CreateMapper();
    }
}