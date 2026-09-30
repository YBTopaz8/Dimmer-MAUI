using System;
using System.Collections.Generic;
using System.Text;

namespace Dimmer.ViewModel;

public partial class EqBandViewModel : ObservableObject
{
    public int Index { get; }
    public string FrequencyLabel { get; }
    public bool IsBasic { get; } // True if this is one of the standard 10 bands
    private Action<int, double> _onChangedAction;

    [ObservableProperty]
    public partial double Gain { get; set; }

    partial void OnGainChanged(double value) => _onChangedAction(Index, value);

    public EqBandViewModel(int index, string freq, bool isBasic, Action<int, double> onChanged)
    {
        Index = index;
        FrequencyLabel = freq;
        IsBasic = isBasic;
        _onChangedAction = onChanged;
    }
}