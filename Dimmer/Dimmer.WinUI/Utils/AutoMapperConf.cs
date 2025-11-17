using Dimmer.WinUI.Utils.Models;

namespace Dimmer.WinUI.Utils;

public static class AutoMapperConfWinUI
{
    public static IMapper ConfigureAutoMapper()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<ArtistModelWin, ArtistModel>()
            .PreserveReferences().ReverseMap()
            .PreserveReferences();

        });

        return config.CreateMapper();
    }
}
