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

    public BaseAlbumViewModelWin() : this(
            IPlatformApplication.Current.Services.GetRequiredService<IMapper>(),
            IPlatformApplication.Current.Services.GetRequiredService<AlbumsMgtFlow>(),
            IPlatformApplication.Current.Services.GetRequiredService<BaseViewModel>(),
            IPlatformApplication.Current.Services.GetRequiredService<PlayListMgtFlow>(),
            IPlatformApplication.Current.Services.GetRequiredService<SongsMgtFlow>(),
            IPlatformApplication.Current.Services.GetRequiredService<IPlayerStateService>(),
            IPlatformApplication.Current.Services.GetRequiredService<ISettingsService>(),
            IPlatformApplication.Current.Services.GetRequiredService<SubscriptionManager>())
    { }
    public void Dispose()
    {
        
    }
}
