﻿using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.ViewModel;
public partial class BaseAlbumViewModel : BaseViewModel
{
    private readonly IMapper mapper;
    [ObservableProperty]
    public partial ObservableCollection<AlbumModelView>? SelectedAlbumsCol { get; internal set; }
    [ObservableProperty]
    public partial AlbumModelView? SelectedAlbum { get; internal set; }

    public BaseAlbumViewModel(IMapper mapper, AlbumsMgtFlow albumsMgtFlow, SongsMgtFlow songsMgtFlow, IDimmerAudioService dimmerAudioService)
        : base(mapper, albumsMgtFlow, songsMgtFlow, dimmerAudioService)
    {
        this.mapper=mapper;
        SubscribeToAlbumListChanges();
    }

    private void SubscribeToAlbumListChanges()
    {
        AlbumsMgtFlow.SpecificAlbums.Subscribe(albums =>
        {
            if (albums != null && albums.Count > 0)
            {
                SelectedAlbumsCol = mapper.Map<List<AlbumModelView>>(albums).ToObservableCollection();
                SelectedAlbum = SelectedAlbumsCol[0];
            }
        }); 
    }

    public void GetAlbumsForSpecificSong(SongModelView song)
    {
        albumsMgtFlow.GetAlbumsBySongModel(song);
        
    }
}
