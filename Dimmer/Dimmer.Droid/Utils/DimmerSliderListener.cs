using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Utils;

public class DimmerSliderListener : Java.Lang.Object, IBaseOnSliderTouchListener, IBaseOnChangeListener, ILabelFormatter
{
    private readonly Action _onDragStart;
    private readonly Action<float> _onDragStop;
    private readonly Action<float, bool>? _onValueChange;
    public SliderDataType DataType { get; set; } = SliderDataType.Normal;
    public double TotalDurationInSeconds { get; set; } = 0;
    public DimmerSliderListener(Action onDragStart, Action<float> onDragStop, Action<float, bool>? onValueChange = null)
    {
        _onDragStart = onDragStart;
        _onDragStop = onDragStop;
        _onValueChange = onValueChange;
    }

    public string GetFormattedValue(float value)
    {
        switch (DataType)
        {
            case SliderDataType.Percentage:
                // Example: 50 -> "50%"
                return $"{Math.Round(value)}%";

            case SliderDataType.Time:
                // Assuming value is 0-100, we calculate the time based on TotalDuration
                if (TotalDurationInSeconds > 0)
                {
                    var currentSeconds = (value / 100f) * TotalDurationInSeconds;
                    var ts = TimeSpan.FromSeconds(currentSeconds);
                    return $"{ts.Minutes}:{ts.Seconds:D2}";
                }
                return "0:00";

            case SliderDataType.Normal:
            default:
                // Default numeric behavior
                return value.ToString("0.##");
        }
    }

    // --- IBaseOnSliderTouchListener Implementation ---

    public void OnStartTrackingTouch(Java.Lang.Object slider)
    {
        if (slider is Slider materialSlider)
        {
            // Set 'this' class as the formatter, logic is in GetFormattedValue above
            materialSlider.SetLabelFormatter(this);

            // Optional: Ensure the label behaves nicely
            materialSlider.LabelBehavior = LabelFormatter.LabelFloating;
            
        }

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
public enum SliderDataType
{
    Normal,     // Just numbers (e.g. 1, 2, 3)
    Percentage, // Adds a % sign (e.g. 50%)
    Time        // Formats as mm:ss (Needs total duration)
}