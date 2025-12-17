using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.ViewModel;

public partial class LastFMViewModel : ObservableObject
{
    public BaseViewModel _baseViewModel;

    public LastFMViewModel(BaseViewModel baseViewModel)
    {
        _baseViewModel = baseViewModel;
    }
}
