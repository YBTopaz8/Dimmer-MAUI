using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Utils;

public class DimmerSliderListener : Java.Lang.Object, IBaseOnSliderTouchListener, IBaseOnChangeListener
{
    private readonly Action _onDragStart;
    private readonly Action<float> _onDragStop;
    private readonly Action<float, bool> _onValueChange;

    public DimmerSliderListener(Action onDragStart, Action<float> onDragStop, Action<float, bool> onValueChange = null)
    {
        _onDragStart = onDragStart;
        _onDragStop = onDragStop;
        _onValueChange = onValueChange;
    }

    // --- IBaseOnSliderTouchListener Implementation ---

    public void OnStartTrackingTouch(Java.Lang.Object slider)
    {
        _onDragStart?.Invoke();
    }

    public void OnStopTrackingTouch(Java.Lang.Object slider)
    {
        // We cast the generic Object back to a Slider to get the value
        if (slider is Slider materialSlider)
        {
            _onDragStop?.Invoke(materialSlider.Value);
        }
    }

    // --- IBaseOnChangeListener Implementation ---

    public void OnValueChange(Java.Lang.Object slider, float value, bool fromUser)
    {
        _onValueChange?.Invoke(value, fromUser);
    }
}