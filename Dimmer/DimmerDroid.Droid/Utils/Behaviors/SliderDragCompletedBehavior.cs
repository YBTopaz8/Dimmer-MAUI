using DevExpress.Maui.Editors;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace Dimmer.Utils.Behaviors;

public class SliderDragCompletedBehavior : Behavior<DXSlider>
{
    private DXSlider _slider;
    private bool _isUserDragging = false;
    private double _pendingValue;

    // Command that fires ONLY when user releases the thumb
    public static readonly BindableProperty UserValueChangedCommandProperty =
        BindableProperty.Create(nameof(UserValueChangedCommand), typeof(ICommand), typeof(SliderDragCompletedBehavior));

    public ICommand UserValueChangedCommand
    {
        get => (ICommand)GetValue(UserValueChangedCommandProperty);
        set => SetValue(UserValueChangedCommandProperty, value);
    }

    // Command that fires during drag (for UI preview)
    public static readonly BindableProperty DragPreviewCommandProperty =
        BindableProperty.Create(nameof(DragPreviewCommand), typeof(ICommand), typeof(SliderDragCompletedBehavior));

    public ICommand DragPreviewCommand
    {
        get => (ICommand)GetValue(DragPreviewCommandProperty);
        set => SetValue(DragPreviewCommandProperty, value);
    }

    protected override void OnAttachedTo(DXSlider slider)
    {
        _slider = slider;

        // Use native touch/pointer events
        var panGesture = new PanGestureRecognizer();
        panGesture.PanUpdated += OnPanUpdated;
        slider.GestureRecognizers.Add(panGesture);

        // Fallback for mouse
        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += OnTapped;
        slider.GestureRecognizers.Add(tapGesture);
    }

    private void OnPanUpdated(object sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _isUserDragging = true;
                break;

            case GestureStatus.Running:
                if (_isUserDragging && DragPreviewCommand?.CanExecute(null) == true)
                {
                    // Pass current slider value during drag
                    DragPreviewCommand?.Execute(_slider.Value);
                }
                break;

            case GestureStatus.Completed:
                if (_isUserDragging)
                {
                    _isUserDragging = false;
                    // Only fire when user releases
                    UserValueChangedCommand?.Execute(_slider.Value);
                }
                break;

            case GestureStatus.Canceled:
                _isUserDragging = false;
                break;
        }
    }

    private void OnTapped(object sender, TappedEventArgs e)
    {
        // Tapping also counts as user interaction
        UserValueChangedCommand?.Execute(_slider.Value);
    }

    protected override void OnDetachingFrom(DXSlider slider)
    {
        slider.GestureRecognizers.Clear();
        _slider = null;
    }
}