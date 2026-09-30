using SkiaSharp;

using System;
using System.Collections.Generic;
using System.Text;

namespace Dimmer.DimmerAudio.Views;

//public class SpectrumAnalyzerView :
//{
//    #region Bindable Properties

//    public static readonly BindableProperty SpectrumDataProperty =
//        BindableProperty.Create(nameof(SpectrumData), typeof(float[]), typeof(SpectrumAnalyzerView), null,
//            propertyChanged: (b, o, n) => ((SpectrumAnalyzerView)b).InvalidateSurface());

//    public static readonly BindableProperty StartColorProperty =
//        BindableProperty.Create(nameof(StartColor), typeof(Color), typeof(SpectrumAnalyzerView), Colors.MediumSpringGreen,
//            propertyChanged: (b, o, n) => ((SpectrumAnalyzerView)b).InvalidateSurface());

//    public static readonly BindableProperty EndColorProperty =
//        BindableProperty.Create(nameof(EndColor), typeof(Color), typeof(SpectrumAnalyzerView), Colors.DarkSlateBlue,
//            propertyChanged: (b, o, n) => ((SpectrumAnalyzerView)b).InvalidateSurface());

//    public static readonly BindableProperty BarCountProperty =
//        BindableProperty.Create(nameof(BarCount), typeof(int), typeof(SpectrumAnalyzerView), 64,
//            propertyChanged: (b, o, n) => ((SpectrumAnalyzerView)b).InvalidateSurface());

//    public static readonly BindableProperty SpacingProperty =
//        BindableProperty.Create(nameof(Spacing), typeof(float), typeof(SpectrumAnalyzerView), 4f,
//            propertyChanged: (b, o, n) => ((SpectrumAnalyzerView)b).InvalidateSurface());

//    public static readonly BindableProperty CornerRadiusProperty =
//        BindableProperty.Create(nameof(CornerRadius), typeof(float), typeof(SpectrumAnalyzerView), 8f,
//            propertyChanged: (b, o, n) => ((SpectrumAnalyzerView)b).InvalidateSurface());

//    public static readonly BindableProperty MinDbProperty =
//        BindableProperty.Create(nameof(MinDb), typeof(float), typeof(SpectrumAnalyzerView), -80f,
//            propertyChanged: (b, o, n) => ((SpectrumAnalyzerView)b).InvalidateSurface());

//    public static readonly BindableProperty MaxDbProperty =
//        BindableProperty.Create(nameof(MaxDb), typeof(float), typeof(SpectrumAnalyzerView), 0f,
//            propertyChanged: (b, o, n) => ((SpectrumAnalyzerView)b).InvalidateSurface());

//    public static readonly BindableProperty CurveFactorProperty =
//        BindableProperty.Create(nameof(CurveFactor), typeof(float), typeof(SpectrumAnalyzerView), 1.5f,
//            propertyChanged: (b, o, n) => ((SpectrumAnalyzerView)b).InvalidateSurface());

//    #endregion

//    #region CLR Properties

//    /// <summary>
//    /// The raw FFT magnitude data in decibels.
//    /// </summary>
//    public float[]? SpectrumData
//    {
//        get => (float[]?)GetValue(SpectrumDataProperty);
//        set => SetValue(SpectrumDataProperty, value);
//    }

//    /// <summary>
//    /// The bottom color of the gradient.
//    /// </summary>
//    public Color StartColor
//    {
//        get => (Color)GetValue(StartColorProperty);
//        set => SetValue(StartColorProperty, value);
//    }

//    /// <summary>
//    /// The top color of the gradient.
//    /// </summary>
//    public Color EndColor
//    {
//        get => (Color)GetValue(EndColorProperty);
//        set => SetValue(EndColorProperty, value);
//    }

//    public int BarCount
//    {
//        get => (int)GetValue(BarCountProperty);
//        set => SetValue(BarCountProperty, value);
//    }

//    public float Spacing
//    {
//        get => (float)GetValue(SpacingProperty);
//        set => SetValue(SpacingProperty, value);
//    }

//    public float CornerRadius
//    {
//        get => (float)GetValue(CornerRadiusProperty);
//        set => SetValue(CornerRadiusProperty, value);
//    }

//    public float MinDb
//    {
//        get => (float)GetValue(MinDbProperty);
//        set => SetValue(MinDbProperty, value);
//    }

//    public float MaxDb
//    {
//        get => (float)GetValue(MaxDbProperty);
//        set => SetValue(MaxDbProperty, value);
//    }

//    /// <summary>
//    /// Exaggerates the height of louder sounds and suppresses quieter ones. Default 1.5.
//    /// </summary>
//    public float CurveFactor
//    {
//        get => (float)GetValue(CurveFactorProperty);
//        set => SetValue(CurveFactorProperty, value);
//    }

//    #endregion

//    // We cache the SKPaint to save Memory/CPU allocations during 60FPS drawing
//    private readonly SKPaint _barPaint = new()
//    {
//        Style = SKPaintStyle.Fill,
//        IsAntialias = true
//    };

//    public SpectrumAnalyzerView()
//    {
//        IgnorePixelScaling = true; // Ensures sharp rendering on high-DPI Android/Windows screens
//    }

//    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
//    {
//        base.OnPaintSurface(e);

//        var info = e.Info;
//        var canvas = e.Surface.Canvas;

//        // 1. Clear the canvas (transparent background)
//        canvas.Clear(SKColors.Transparent);

//        var data = SpectrumData;
//        if (data == null || data.Length == 0)
//            return;

//        // 2. Setup Gradient Shader (Dynamic based on control height)
//        _barPaint.Shader = SKShader.CreateLinearGradient(
//            new SKPoint(0, info.Height), // Bottom
//            new SKPoint(0, 0),           // Top
//            new[] { StartColor.ToSKColor(), EndColor.ToSKColor() },
//            null,
//            SKShaderTileMode.Clamp);

//        // 3. Calculate Layout
//        int barsToDraw = Math.Min(data.Length, BarCount);
//        if (barsToDraw <= 0) return;

//        float barWidth = (float)info.Width / barsToDraw;
//        float actualSpacing = Spacing * (info.Width / (float)Width); // Adjust spacing for pixel scaling

//        // Ensure spacing doesn't consume the whole bar
//        if (actualSpacing >= barWidth)
//            actualSpacing = barWidth * 0.2f;

//        float usableBarWidth = barWidth - actualSpacing;

//        // 4. Draw the Bars
//        for (int i = 0; i < barsToDraw; i++)
//        {
//            float db = data[i];

//            // Clamp to allowed ranges
//            db = Math.Clamp(db, MinDb, MaxDb);

//            // Normalize between 0.0 and 1.0
//            float normalized = (db - MinDb) / (MaxDb - MinDb);

//            // Apply exponential curve (makes the visualizer look punchier)
//            if (CurveFactor != 1.0f && normalized > 0)
//            {
//                normalized = (float)Math.Pow(normalized, CurveFactor);
//            }

//            // Calculate height (minimum 4 pixels so silent frequencies show a tiny dot)
//            float barHeight = Math.Max(4f, info.Height * normalized);

//            // Calculate positions
//            float x = i * barWidth;
//            float y = info.Height - barHeight;

//            var rect = new SKRect(x, y, x + usableBarWidth, info.Height);

//            // Draw rounded rectangle
//            canvas.DrawRoundRect(rect, CornerRadius, CornerRadius, _barPaint);
//        }
//    }
//}