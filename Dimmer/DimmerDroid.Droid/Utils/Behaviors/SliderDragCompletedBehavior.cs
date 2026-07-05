using DevExpress.Maui.Editors;
using System.Windows.Input;

namespace Dimmer.Utils.Behaviors;

public class SliderDragCompletedBehavior : Behavior<DXSlider>
{
    private DXSlider? _slider;
    private bool _isUserDragging = false;
    private double _dragStartValue;

    public static readonly BindableProperty UserValueChangedCommandProperty =
        BindableProperty.Create(nameof(UserValueChangedCommand), typeof(ICommand), typeof(SliderDragCompletedBehavior));

    public static readonly BindableProperty DragPreviewCommandProperty =
        BindableProperty.Create(nameof(DragPreviewCommand), typeof(ICommand), typeof(SliderDragCompletedBehavior));

    public static readonly BindableProperty IsEnabledProperty =
        BindableProperty.Create(nameof(IsEnabled), typeof(bool), typeof(SliderDragCompletedBehavior), true);

    public ICommand UserValueChangedCommand
    {
        get => (ICommand)GetValue(UserValueChangedCommandProperty);
        set => SetValue(UserValueChangedCommandProperty, value);
    }

    public ICommand DragPreviewCommand
    {
        get => (ICommand)GetValue(DragPreviewCommandProperty);
        set => SetValue(DragPreviewCommandProperty, value);
    }

    public bool IsEnabled
    {
        get => (bool)GetValue(IsEnabledProperty);
        set => SetValue(IsEnabledProperty, value);
    }

    protected override void OnAttachedTo(DXSlider slider)
    {
        _slider = slider;

        var panGesture = new PanGestureRecognizer();
        panGesture.PanUpdated += OnPanUpdated;
        slider.GestureRecognizers.Add(panGesture);

        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += OnTap;
        slider.GestureRecognizers.Add(tapGesture);

        // Handle programmatic updates
        slider.PropertyChanged += OnSliderPropertyChanged;
    }

    private void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        if (!IsEnabled) return;
        if (_slider is null) return;

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _isUserDragging = true;
                _dragStartValue = _slider.Value;
                break;

            case GestureStatus.Running:
                if (_isUserDragging)
                {
                    DragPreviewCommand?.Execute(_slider.Value);
                }
                break;

            case GestureStatus.Completed:
                if (_isUserDragging && Math.Abs(_slider.Value - _dragStartValue) > 0.01)
                {
                    UserValueChangedCommand?.Execute(_slider.Value);
                }
                _isUserDragging = false;
                break;
        }
    }

    private void OnTap(object? sender, TappedEventArgs e)
    {
        if (!IsEnabled) return;
        if (_slider is null) return;

        var point = e.GetPosition(_slider);
        if (point.HasValue)
        {
            var position = point.Value.X / _slider.Width;
            var newValue = _slider.MinValue + (position * (_slider.MaxValue - _slider.MinValue));
            newValue = Math.Max(_slider.MinValue, Math.Min(_slider.MaxValue, newValue));
            UserValueChangedCommand?.Execute(newValue);
        }
    }

    private void OnSliderPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DXSlider.Value) && !_isUserDragging)
        {
            // Programmatic update - do nothing
            return;
        }
    }

    protected override void OnDetachingFrom(DXSlider slider)
    {
        slider.GestureRecognizers.Clear();
        slider.PropertyChanged -= OnSliderPropertyChanged;
        _slider = null;
    }
}