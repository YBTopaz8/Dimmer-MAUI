namespace Dimmer.Orchestration;
public static class AutoMapperConf
{
    public static IMapper ConfigureAutoMapper()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<SongModel, SongModelView>().ReverseMap();            
            cfg.CreateMap<AlbumModel, AlbumModelView>().ReverseMap()
            .ForMember(dest => dest.Name, opt => opt.Ignore())
            .ReverseMap();
            cfg.CreateMap<ArtistModel, ArtistModelView>().ReverseMap();            
            cfg.CreateMap<GenreModel, GenreModelView>().ReverseMap();
            cfg.CreateMap<PlaylistModel, PlaylistModelView>().ReverseMap();            
        });

        return config.CreateMapper();
    }
}
