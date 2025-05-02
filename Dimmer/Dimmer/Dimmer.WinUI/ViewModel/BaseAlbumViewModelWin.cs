using Dimmer.Services;

namespace Dimmer.WinUI.ViewModel;
public class BaseAlbumViewModelWin : BaseAlbumViewModel, IDisposable
{
    public BaseAlbumViewModelWin(IMapper mapper, BaseViewModel baseViewModel, AlbumsMgtFlow albumsMgtFlow, PlayListMgtFlow playlistsMgtFlow, SongsMgtFlow songsMgtFlow, IPlayerStateService stateService, ISettingsService settingsService, SubscriptionManager subs) : base(mapper, baseViewModel, albumsMgtFlow, playlistsMgtFlow, songsMgtFlow, stateService, settingsService, subs)
    {
    }

    public void Dispose()
    {
        
    }
}
