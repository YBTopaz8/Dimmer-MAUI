using AutoMapper;
using Dimmer.Interfaces;
using Dimmer.Orchestration;
using Dimmer.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.ViewModel;
public partial class BaseAlbumViewModel : ObservableObject
{
    private readonly IMapper _mapper;
    private readonly IPlayerStateService _stateService;
    private readonly ISettingsService _settingsService;
    private readonly SubscriptionManager _subs;
    [ObservableProperty]
    public partial List<AlbumModelView>? SelectedAlbumsCol { get; internal set; }
    [ObservableProperty]
    public partial AlbumModelView? SelectedAlbum { get; internal set; }

    public AlbumsMgtFlow AlbumsMgtFlow { get; }
    public PlayListMgtFlow PlaylistsMgtFlow { get; }
    public SongsMgtFlow SongsMgtFlow { get; }
    public BaseAlbumViewModel(
            IMapper mapper,
            AlbumsMgtFlow albumsMgtFlow,
            PlayListMgtFlow playlistsMgtFlow,
            SongsMgtFlow songsMgtFlow,
            IPlayerStateService stateService,
            ISettingsService settingsService,
            SubscriptionManager subs)
    {
        
            _mapper = mapper;
            AlbumsMgtFlow = albumsMgtFlow;
            PlaylistsMgtFlow = playlistsMgtFlow;
            SongsMgtFlow = songsMgtFlow;
            _stateService = stateService;
            _settingsService = settingsService;
            _subs = subs;
        SubscribeToAlbumListChanges();
    }

    private void SubscribeToAlbumListChanges()
    {
        AlbumsMgtFlow.SpecificAlbums.Subscribe(albums =>
        {
            if (albums != null && albums.Count > 0)
            {
                SelectedAlbumsCol = _mapper.Map<List<AlbumModelView>>(albums);
            }
        });
    }

    public void GetAlbumForSpecificSong(SongModelView song)
    {
        //albumsMgtFlow.GetAlbumsBySongModel(song);
    }
}
