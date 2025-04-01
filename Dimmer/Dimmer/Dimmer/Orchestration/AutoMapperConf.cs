using AutoMapper;
using Dimmer.Data.Models;
using Dimmer.Data.ModelView;
using Dimmer.Database.ModelView;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Orchestration;
public static class AutoMapperConf
{
    public static IMapper ConfigureAutoMapper()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<SongModel, SongModelView>();
            cfg.CreateMap<SongModelView, SongModel>();
            cfg.CreateMap<AlbumModel, AlbumModelView>();
            cfg.CreateMap<AlbumModelView, AlbumModel>();
            cfg.CreateMap<ArtistModel, ArtistModelView>();
            cfg.CreateMap<ArtistModelView, ArtistModel>();
            cfg.CreateMap<GenreModel, GenreModelView>();
            cfg.CreateMap<GenreModelView, GenreModel>();
        });

        return config.CreateMapper();
    }
}
