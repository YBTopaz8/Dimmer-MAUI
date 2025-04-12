


using Dimmer.Orchestration;
using Dimmer.UIUtils;
using Dimmer.Utilities.Enums;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Dimmer.WinUI.Views;
public partial class SingleSongPageViewModel : BaseViewModel
{
    #region private fields   
    #endregion
    public SingleSongPageViewModel(SongsMgtFlow songsMgt, IMapper mapper, IDimmerAudioService dimmerAudioService) : base(mapper, songsMgt, dimmerAudioService)
    {

    }
}
