﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Utilities.Extensions;

public static class ModelToViewExtensions
{
    public static SongModel? ToModel(this SongModelView? view, IMapper mapper)
    {
        if (view == null || mapper == null)
            return null;
        return mapper.Map<SongModel>(view); // Assumes AutoMapper or similar IMapper setup
    }

    public static SongModelView? ToModelView(this SongModel? model, IMapper mapper)
    {
        if (model == null || mapper == null)
            return null;
        return mapper.Map<SongModelView>(model);
    }

    public static ArtistModelView? ToModelView(this ArtistModel? model, IMapper mapper)
    {
        if (model == null || mapper == null)
            return null;
        return mapper.Map<ArtistModelView>(model);
    }

    public static ArtistModel? ToModel(this ArtistModelView? model, IMapper mapper)
    {
        if (model == null || mapper == null)
            return null;
        return mapper.Map<ArtistModel>(model);
    }

    public static AlbumModelView? ToModelView(this AlbumModel? model, IMapper mapper)
    {
        if (model == null || mapper == null)
            return null;
        return mapper.Map<AlbumModelView>(model);
    }

    public static AlbumModel? ToModel(this AlbumModelView? model, IMapper mapper)
    {
        if (model == null || mapper == null)
            return null;
        return mapper.Map<AlbumModel>(model);
    }

    public static UserModelView? ToModelView(this UserModel? model, IMapper mapper)
    {
        if (model == null || mapper == null)
            return null;
        return mapper.Map<UserModelView>(model);
    }

    public static UserModel? ToModel(this UserModelView? model, IMapper mapper)
    {
        if (model == null || mapper == null)
            return null;
        return mapper.Map<UserModel>(model);
    }
}