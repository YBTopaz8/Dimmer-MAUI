using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Orchestration;

public partial class SongsMgtFlow : ObservableObject
{

    public SongsMgtFlow(BaseAppFlow baseFlow)
    {
        BaseFlow = baseFlow;
    }

    public BaseAppFlow BaseFlow { get; }
}
