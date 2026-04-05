using System;
using System.Collections.Generic;
using System.Text;
using StateTriggerBase = Microsoft.UI.Xaml.StateTriggerBase;

namespace Dimmer.WinUI.Utils.StateTriggers;

public class BooleanStateTrigger : StateTriggerBase
{
    private bool _isActive;

    public static readonly DependencyProperty BindingProperty =
        DependencyProperty.Register(nameof(Binding), typeof(bool), typeof(BooleanStateTrigger),
            new PropertyMetadata(false, OnBindingChanged));

    public bool Binding
    {
        get => (bool)GetValue(BindingProperty);
        set => SetValue(BindingProperty, value);
    }

    private static void OnBindingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var trigger = (BooleanStateTrigger)d;
        trigger.SetActive((bool)e.NewValue);
    }

    public new void SetActive(bool isActive)
    {
        _isActive = isActive;
        SetActive(_isActive);
    }
}