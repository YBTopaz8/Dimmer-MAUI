using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Dimmer.Data.Models;
using Dimmer.DimmerLive.Models;
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
