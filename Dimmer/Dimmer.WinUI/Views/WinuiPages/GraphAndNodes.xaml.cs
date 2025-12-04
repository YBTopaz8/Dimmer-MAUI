using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

using Dimmer.GraphSupport;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using SkiaSharp;
using SkiaSharp.Views.Windows;

using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views.WinuiPages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class GraphAndNodes : Page
{
    public GraphAndNodes()
    {

        this.InitializeComponent();
        this.Loaded += GraphPage_Loaded;
        this.Unloaded += GraphPage_Unloaded;
    }

    private void GraphPage_Unloaded(object sender, RoutedEventArgs e)
    {
        // Stop the loop when leaving the page to prevent memory leaks
        CompositionTarget.Rendering -= OnRendering;
        _stopwatch.Stop();
    }
    private void GraphPage_Loaded(object sender, RoutedEventArgs e)
    {
        InitializeGraph();

        _stopwatch.Start();

        // SUBSCRIBE TO THE RENDER LOOP (The Fix)
        // This runs once per frame (approx 60 times a second), keeping the UI responsive.
        CompositionTarget.Rendering += OnRendering;
    }


    private Graph _graph;
    private PhysicsEngine _physics;
    private Renderer _renderer;
    private PanZoom _panZoom = new PanZoom();

    // Loop
    private bool _isAnimating = true;
    private Stopwatch _stopwatch = new Stopwatch();

    // Interaction
    private bool _isDragging = false;
    private float _lastX, _lastY;
    private float _downX, _downY;

    // ViewModel Mock/Reference (Replace with your actual VM injection)
    private dynamic _viewModel;

   
    private void InitializeGraph()
    {
        _graph = new Graph();

        // 1. Root Node (Fixed Anchor)
        var root = new Node("root", "Library")
        {
            Position = new SKPoint(0, 0),
            IsFixed = true,
            Color = SKColors.OrangeRed,
            Radius = 60
        };
        _graph.Root = root;
        _graph.Nodes.Add(root);

        // 2. Add Dummy Data (Simulating Artists)
        for (int i = 0; i < 6; i++)
        {
            float angle = i * 1.0f;
            var artistNode = new Node($"artist_{i}", $"Artist {i}")
            {
                Radius = 45,
                Color = SKColors.MediumPurple,
                // Initial offset to prevent stacking
                Position = new SKPoint((float)Math.Cos(angle) * 10, (float)Math.Sin(angle) * 10)
            };

            // Add dummy child so it looks expandable
            artistNode.Children.Add(new Node("dummy", "loading..."));

            root.Children.Add(artistNode);
        }

        _graph.ExpandLazy(_graph.Root);

        _physics = new PhysicsEngine(_graph);
        _renderer = new Renderer(_graph);
    }


    private void OnRendering(object? sender, object e)
    {
        if (_physics is null) return;
        if (_renderer is null) return;
        // 1. Update Physics
        // Using a fixed step (0.016 = 60fps) is usually smoother for UI physics than variable dt
        _physics.Update(0.016f);

        // 2. Request a Redraw
        // This triggers OnPaintSurface, but allows the UI thread to handle clicks first
        GraphCanvas.Invalidate();
    }

    private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
    {
        if (_physics is null) return;
        if (_renderer is null) return;
        var canvas = e.Surface.Canvas;
        int width = e.Info.Width;
        int height = e.Info.Height;

        // 1. Clear
        canvas.Clear(SKColors.DarkSlateGray);

        // 2. Physics Step
        // Fixed time step is better, but simple dt works for UI
        float dt = 0.016f;
        if (_isAnimating)
        {
            _physics.Update(dt);
        }

        // 3. Draw
        _renderer.Draw(canvas, width, height, _panZoom);

        // 4. Request Next Frame
        if (_isAnimating)
        {
            GraphCanvas.Invalidate();
        }
    }

    // --- Input Handling ---

    private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(GraphCanvas).Position;
        _lastX = (float)point.X;
        _lastY = (float)point.Y;
        _downX = _lastX;
        _downY = _lastY;
        _isDragging = false;

        // Capture pointer to track movement outside bounds
        (sender as UIElement).CapturePointer(e.Pointer);
    }

    private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!e.Pointer.IsInContact) return;

        var point = e.GetCurrentPoint(GraphCanvas).Position;
        float x = (float)point.X;
        float y = (float)point.Y;

        float dx = x - _lastX;
        float dy = y - _lastY;

        // Threshold check
        if (Math.Abs(x - _downX) > 5 || Math.Abs(y - _downY) > 5)
        {
            _isDragging = true;
        }

        // Pan
        _panZoom.OffsetX += dx;
        _panZoom.OffsetY += dy;

        _lastX = x;
        _lastY = y;
    }

    private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        (sender as UIElement).ReleasePointerCapture(e.Pointer);

        if (!_isDragging)
        {
            var point = e.GetCurrentPoint(GraphCanvas).Position;
            HandleTap((float)point.X, (float)point.Y);
        }
    }

    private void OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        var delta = e.GetCurrentPoint(GraphCanvas).Properties.MouseWheelDelta;

        // Simple zoom logic
        if (delta > 0)
            _panZoom.Scale *= 1.1f;
        else
            _panZoom.Scale *= 0.9f;

        // Clamp
        _panZoom.Scale = Math.Clamp(_panZoom.Scale, 0.1f, 5.0f);

        // Sync Slider
        ZoomSlider.Value = _panZoom.Scale;
    }

    private void HandleTap(float screenX, float screenY)
    {
        // Get World Coordinates
        // Note: We need Canvas ActualSize, but e.Info isn't available here. 
        // Use ActualWidth/Height from XAML element
        var worldPoint = _renderer.ScreenToWorld(
            screenX,
            screenY,
            _panZoom);

        var hitNode = _graph.PickNode(worldPoint, 40f);

        if (hitNode != null)
        {
            OnNodeClicked(hitNode);
        }
    }

    private async void OnNodeClicked(Node node)
    {
        if (node.Children.Count > 0)
        {
            if (node.IsExpanded)
            {
                _graph.Collapse(node);
            }
            else
            {
                // Simulate Data Load
                if (node.Id.StartsWith("artist_") && node.Children.Any(c => c.Id == "dummy"))
                {
                    await LoadRealData(node);
                }
                _graph.ExpandLazy(node);
            }
        }
        else
        {
            // Leaf Node (Song) logic
            Debug.WriteLine($"Playing {node.Label}");
        }

        // Wake up physics
        foreach (var n in _graph.Nodes) n.Velocity = new SKPoint(1, 1);
    }

    private async Task LoadRealData(Node parent)
    {
        parent.Children.Clear();
        // Fake delay
        await Task.Delay(50);

        for (int i = 0; i < 5; i++)
        {
            var album = new Node($"alb_{parent.Id}_{i}", $"Album {i}")
            {
                Radius = 35,
                Color = SKColors.SteelBlue
            };

            // Songs
            for (int j = 0; j < 6; j++)
            {
                album.Children.Add(new Node($"s_{i}_{j}", $"Track {j}") { Radius = 20, Color = SKColors.LightSeaGreen });
            }
            parent.Children.Add(album);
        }
    }

    private void OnZoomValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        _panZoom.Scale = (float)e.NewValue;
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        // Navigation Logic
        if (Frame.CanGoBack) Frame.GoBack();
    }
}