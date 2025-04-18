using Dimmer.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.WinUI.ViewModel;
public class BaseAlbumViewModelWin : BaseAlbumViewModel, IDisposable
{
    public BaseAlbumViewModelWin(IMapper mapper, AlbumsMgtFlow albumsMgtFlow,
        BaseViewModel baseViewModel,
        PlayListMgtFlow playlistsMgtFlow, SongsMgtFlow songsMgtFlow, IPlayerStateService stateService, ISettingsService settingsService, SubscriptionManager subs) : base(mapper, baseViewModel, albumsMgtFlow, playlistsMgtFlow, songsMgtFlow, stateService, settingsService, subs)
    {
    }

    public void Dispose()
    {
        
    }
}
