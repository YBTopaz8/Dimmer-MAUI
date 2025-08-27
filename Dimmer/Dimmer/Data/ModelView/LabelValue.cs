﻿namespace Dimmer.Data.ModelView;
public class LabelValue
{
    public string Label { get; set; }
    public double Value { get; set; }
    public LabelValue(string label, double value) { Label = label; Value = value; }
}