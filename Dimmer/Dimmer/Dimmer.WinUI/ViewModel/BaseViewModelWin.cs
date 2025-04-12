using Dimmer.Data.ModelView;
using Dimmer.Orchestration;
using Dimmer.WinUI.Utils.StaticUtils;
using Dimmer.WinUI.Utils.StaticUtils.TaskBarSection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.WinUI.ViewModel;
public partial class BaseViewModelWin(IMapper mapper, SongsMgtFlow songsMgtFlow, IDimmerAudioService dimmerAudioService) : BaseViewModel(mapper, songsMgtFlow, dimmerAudioService)
{

    //partial void OnCurrentPositionPercentageChanging(double value)
    //{
    //    if (value > 0 && value < 100)
    //    {
    //        WindowsIntegration.SetTaskbarProgress(PlatUtils.GetWindowHandle(), completed: (uint)value, total: 100);
    //    }
    //}
    public static void SetTaskbarProgress(double position)
    {
        WindowsIntegration.SetTaskbarProgress(PlatUtils.GetWindowHandle(), completed: 50, total: 100);
    }
}
