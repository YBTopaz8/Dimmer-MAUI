using Dimmer.DimmerLive.Models;

namespace Dimmer.Orchestration;
public static class AutoMapperConf
{
    public static IMapper ConfigureAutoMapper()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<SongModel, SongModelView>().ReverseMap();            
            cfg.CreateMap<SongModelView, SongModelView>();            
            cfg.CreateMap<AlbumModel, AlbumModelView>().ReverseMap()
            .ForMember(dest => dest.Name, opt => opt.Ignore())
            .ReverseMap();
            cfg.CreateMap<ArtistModel, ArtistModelView>().ReverseMap();            
            cfg.CreateMap<UserNoteModel, UserNoteModelView>().ReverseMap();            
            cfg.CreateMap<UserNoteModel, UserModelOnline>().ReverseMap();            
            cfg.CreateMap<UserModelOnline, UserModelView>().ReverseMap();            
            cfg.CreateMap<UserModel, UserModelView>().ReverseMap();            
            cfg.CreateMap<GenreModel, GenreModelView>().ReverseMap();
            cfg.CreateMap<PlaylistModel, PlaylistModelView>().ReverseMap();            
            cfg.CreateMap<DimmerSharedSong, SongModelView>().ReverseMap();                    
            cfg.CreateMap<LyricPhraseModel, LyricPhraseModelView>().ReverseMap();            
        });

        return config.CreateMapper();
    }
}
