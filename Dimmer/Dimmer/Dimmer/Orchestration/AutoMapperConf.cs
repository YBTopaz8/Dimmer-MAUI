//using Dimmer.DimmerLive.Models;


namespace Dimmer.Orchestration;
public static class AutoMapperConf
{
    public static IMapper ConfigureAutoMapper()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<SongModel, SongModelView>()
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
            
            .ConstructUsing(src=>new AppStateModel())
            .ForMember(dest =>dest.Id, opt => opt.MapFrom(src => src.Id))
            ;
            cfg.CreateMap<LyricPhraseModel, LyricPhraseModelView>().ReverseMap();            
        });
        //config.AssertConfigurationIsValid();

        return config.CreateMapper();
    }
}
