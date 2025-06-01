//using Dimmer.DimmerLive.Models;


namespace Dimmer.Orchestration;
public static class AutoMapperConf
{
    public static IMapper ConfigureAutoMapper()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<DimmerPlayEvent, PlayDataLink>()
                   // Id is already ObjectId → ObjectId
                   .ForMember(dest => dest.Id,
                              opt => opt.MapFrom(src => src.Id))
                   // SongId on DimmerPlayEvent is ObjectId?; on ViewModel it's a string.
                   .ForMember(dest => dest.SongId,
                              opt => opt.MapFrom(src => src.SongId.HasValue
                                                          ? src.SongId.ToString()
                                                          : null))
                   // PlayType is int → int (auto)
                   .ForMember(dest => dest.PlayType,
                              opt => opt.MapFrom(src => src.PlayType))
                   // DateStarted (DateTime) comes from DatePlayed (DateTimeOffset)
                   .ForMember(dest => dest.DateStarted,
                              opt => opt.MapFrom(src => src.DatePlayed.UtcDateTime))
                   // DateFinished (DateTime) comes from DateFinished (DateTimeOffset)
                   .ForMember(dest => dest.DateFinished,
                              opt => opt.MapFrom(src => src.DateFinished.UtcDateTime))
                   // WasPlayCompleted maps bool → bool
                   .ForMember(dest => dest.WasPlayCompleted,
                              opt => opt.MapFrom(src => src.WasPlayCompleted))
                   // PositionInSeconds maps double → double
                   .ForMember(dest => dest.PositionInSeconds,
                              opt => opt.MapFrom(src => src.PositionInSeconds))
                   // EventDate (DateTimeOffset?) → DateTime; if null, take UtcNow
                   .ForMember(dest => dest.EventDate,
                              opt => opt.MapFrom(src =>
                                  (src.EventDate ?? DateTimeOffset.UtcNow).UtcDateTime));
            // (If you also want reverse mapping, you can add .ReverseMap() here,
            //  but with two different date types it may need adjustments.)

            cfg.CreateMap<SongModel, SongModelView>()
            .PreserveReferences().ReverseMap()
            .PreserveReferences();

            cfg.CreateMap<DimmerPlayEventView, DimmerPlayEvent>().ForMember(dest => dest.SongsLinkingToThisEvent, opt => opt.Ignore())
            .PreserveReferences().ReverseMap()
            .PreserveReferences();

            cfg.CreateMap<DimmerPlayEvent, DimmerPlayEventView>()
            .PreserveReferences().ReverseMap()
            .PreserveReferences();

            cfg.CreateMap<AlbumModel, AlbumModelView>()
                .PreserveReferences(); // Add this
            cfg.CreateMap<AlbumModelView, AlbumModel>()
                .ForMember(dest => dest.Name, opt => opt.Ignore())
                .PreserveReferences();

            cfg.CreateMap<ArtistModel, ArtistModelView>().PreserveReferences().ReverseMap().PreserveReferences();


            cfg.CreateMap<UserNoteModel, UserNoteModelView>().ReverseMap();
            cfg.CreateMap<UserNoteModel, UserModelOnline>().ReverseMap();
            cfg.CreateMap<UserModelOnline, UserModelView>().ReverseMap();
            cfg.CreateMap<UserModel, UserModelView>().ReverseMap();
            cfg.CreateMap<GenreModel, GenreModelView>().ReverseMap();
            cfg.CreateMap<PlaylistModel, PlaylistModelView>().ReverseMap();
            cfg.CreateMap<AppStateModel, AppStateModelView>().ReverseMap();
            cfg.CreateMap<AppStateModel, AppStateModel>()
            .ConstructUsing(src => new AppStateModel())
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            ;
            cfg.CreateMap<LyricPhraseModel, LyricPhraseModelView>().ReverseMap();
        });
        //config.AssertConfigurationIsValid();

        return config.CreateMapper();
    }
}
