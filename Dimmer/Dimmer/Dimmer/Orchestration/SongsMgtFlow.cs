using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Orchestration;

public partial class SongsMgtFlow : ObservableObject
{

    public SongsMgtFlow(IDimmerAudioService dimmerAudioService)
    {
        DimmerAudioService=dimmerAudioService;
    }
    public IDimmerAudioService DimmerAudioService { get; }
}
