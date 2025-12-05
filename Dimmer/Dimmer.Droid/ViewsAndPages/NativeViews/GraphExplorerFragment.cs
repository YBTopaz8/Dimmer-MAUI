

using System.Diagnostics;

namespace Dimmer.ViewsAndPages.NativeViews;



public class GraphExplorerFragment : Fragment
{
    
    private SKCanvasView _canvasView;
    private Slider _zoomSlider;
    private FrameLayout _rootLayout;

    // Graph Systems
    private Graph _graph;
    private PhysicsEngine _physics;
    private Renderer _renderer;
    private PanZoom _panZoom = new PanZoom();

    // Loop & Interaction
    private bool _isAnimating = true;
    private Stopwatch _stopwatch = new Stopwatch();
    private BaseViewModelAnd _viewModel;

    // Touch State
    private float _lastX, _lastY;
    private bool _isDragging = false;
    private const float CLICK_THRESHOLD = 10f; // Pixels
    private float _downX, _downY;

    public GraphExplorerFragment(BaseViewModelAnd viewModel)
    {
        _viewModel = viewModel;
    }

    public GraphExplorerFragment() { } // Required empty constructor

    public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
    {
        var ctx = Context;

        // 1. Setup Root Layout (FrameLayout to stack Canvas + UI Overlays)
        _rootLayout = new FrameLayout(ctx)
        {
            LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)
        };
        _rootLayout.SetBackgroundColor(Color.ParseColor("#1E1E1E")); // Dark BG

        // 2. Setup Skia Canvas
        _canvasView = new SKCanvasView(ctx)
        {
            LayoutParameters = new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)
        };
        _canvasView.PaintSurface += OnPaintSurface;
        _canvasView.Touch += OnTouch;
        _rootLayout.AddView(_canvasView);

        // 3. Setup UI Overlay (Zoom Slider)
        var uiStack = new LinearLayout(ctx)
        {
            Orientation = Orientation.Vertical,
            LayoutParameters = new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        };

        var zoomLabel = new TextView(ctx) { Text = "Zoom Level"};
        zoomLabel.SetTextColor(Color.White);
        zoomLabel.Gravity = GravityFlags.Center;

        _zoomSlider = new Slider(ctx);
        _zoomSlider.ValueFrom = 0.1f;
        _zoomSlider.ValueTo = 3.0f;
        _zoomSlider.Value = 1.0f;
        //_zoomSlider.AddOnChangeListener(new ZoomListener(_panZoom, _canvasView));

        uiStack.AddView(zoomLabel);
        uiStack.AddView(_zoomSlider);

        // Position UI at bottom
        var uiParams = (FrameLayout.LayoutParams)uiStack.LayoutParameters;
        uiParams.Gravity = GravityFlags.Bottom;
        uiParams.SetMargins(50, 0, 50, 50);

        _rootLayout.AddView(uiStack);

        // 4. Initialize Graph Data
        InitializeGraphData();

        return _rootLayout;
    }

    private void InitializeGraphData()
    {
        // --- REAL DATA INTEGRATION HERE ---
        // Instead of SampleGraph, we build from ViewModel

        _graph = new Graph();

        // Example: Create a root node for "Library" or the currently playing Artist
        var rootNode = new Node("root", "My Library")
        {
            Position = new SKPoint(0, 0),
            Radius = 80,
            IsFixed = true,
            Velocity = new SKPoint(0, 0)
        };
        _graph.Root = rootNode;
        _graph.Nodes.Add(rootNode);

        // Populate initial level (e.g., Top 5 Artists from ViewModel)
        if (_viewModel.TopArtists != null)
        {
            int i = 0;
            // Assuming TopArtists is a list of your ArtistModel
            foreach (var artist in _viewModel.TopArtists.Take(5))
            {
                var artistNode = new Node($"artist_{artist.Name}", artist.Name)
                {
                    Radius = 50,
                    Color = SKColors.MediumPurple
                };

                // IMPORTANT: Spawn them slightly offset so they don't explode
                float angle = i * 1.0f; // Just a dummy angle
                artistNode.Position = new SKPoint(
                    (float)Math.Cos(angle) * 10,
                    (float)Math.Sin(angle) * 10
                );

                artistNode.Children.Add(new Node("dummy", "loading..."));
                rootNode.Children.Add(artistNode);
                i++;
            }
        }
        else
        {
            // Fallback if VM is empty
            _graph = Graph.SampleGraph();
        }

        // Expand the root immediately
        _graph.ExpandLazy(_graph.Root);

        _physics = new PhysicsEngine(_graph);
        _renderer = new Renderer(_graph);
        
        _stopwatch.Start();
    }

    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        var width = e.Info.Width;
        var height = e.Info.Height;

        // 1. Clear Screen
        canvas.Clear(SKColors.DarkGray); // Deep dark blue/gray

        // 2. Calculate Delta Time (dt)
        float dt = 0.016f; // Fixed step is safer for physics stability than variable dt
        // Or: (float)_stopwatch.Elapsed.TotalSeconds; _stopwatch.Restart();

        // 3. Update Physics
        if (_isAnimating)
        {
            _physics.Update(dt);
        }

        // 4. Draw Graph
        _renderer.Draw(canvas, width, height, _panZoom);

        // 5. Trigger next frame (Game Loop)
        // We invalidate to request the next render pass immediately
        _canvasView.Invalidate();
    }

    private void OnTouch(object? sender, View.TouchEventArgs e)
    {
        switch (e.Event.Action)
        {
            case MotionEventActions.Down:
                _lastX = e.Event.GetX();
                _lastY = e.Event.GetY();
                _downX = _lastX;
                _downY = _lastY;
                _isDragging = false;
                break;

            case MotionEventActions.Move:
                float x = e.Event.GetX();
                float y = e.Event.GetY();
                float dx = x - _lastX;
                float dy = y - _lastY;

                // Check drag threshold
                if (Math.Abs(x - _downX) > CLICK_THRESHOLD || Math.Abs(y - _downY) > CLICK_THRESHOLD)
                {
                    _isDragging = true;
                }

                // Pan the camera
                _panZoom.OffsetX += dx;
                _panZoom.OffsetY += dy;

                _lastX = x;
                _lastY = y;
                break;

            case MotionEventActions.Up:
                // Handle Click
                if (!_isDragging)
                {
                    HandleTap(e.Event.GetX(), e.Event.GetY());
                }
                break;
        }
        e.Handled = true;
    }

    private void HandleTap(float screenX, float screenY)
    {
        // 1. Convert Screen Coordinates to World Coordinates
        var worldPoint = _renderer.ScreenToWorld(screenX, screenY, _panZoom);

        // 2. Ask Graph if we hit anything
        // Hit radius needs to be slightly generous for fingers
        var clickedNode = _graph.PickNode(worldPoint, 40f);

        if (clickedNode != null)
        {
            OnNodeClicked(clickedNode);
        }
    }

    private async void OnNodeClicked(Node node)
    {
        // Haptic Feedback
        _canvasView.PerformHapticFeedback(FeedbackConstants.ClockTick);

        // LOGIC: What happens when you tap?

        if (node.Children.Count > 0)
        {
            // It's a Parent Node (Artist/Album) -> Toggle Expand
            if (node.IsExpanded)
            {
                _graph.Collapse(node);
            }
            else
            {
                // -- DATA LOADING SIMULATION --
                // If this is an Artist node and children are dummies, load real albums now
                if (node.Id.StartsWith("artist_") && node.Children.Any(c => c.Id == "dummy"))
                {
                    await LoadRealDataForArtist(node);
                }

                _graph.ExpandLazy(node);
            }
        }
        else
        {
            // It's a Leaf Node (Song) -> Play it!
            Toast.MakeText(Context, $"Playing: {node.Label}", ToastLength.Short).Show();

            // Invoke ViewModel Command
            // var song = _viewModel.AllSongs.FirstOrDefault(s => s.Id == node.Id);
            // if(song != null) _viewModel.PlaySongCommand.Execute(song);
        }
    }

    private async Task LoadRealDataForArtist(Node artistNode)
    {
        // 1. Clear dummy
        artistNode.Children.Clear();

        // 2. Parse ID
        var artistId = artistNode.Id.Replace("artist_", "");

        // 3. Find in ViewModel (Simulated)
        // var artist = _viewModel.AllArtists.FirstOrDefault(a => a.Id == artistId);
        // var albums = _viewModel.GetAlbumsForArtist(artistId);

        // 4. Create Nodes
        for (int i = 0; i < 5; i++)
        {
            var albNode = new Node($"alb_{artistId}_{i}", $"Album {i}");
            albNode.Radius = 40;

            // Add songs to album
            for (int j = 0; j < 8; j++)
            {
                albNode.Children.Add(new Node($"song_{i}_{j}", $"Track {j}") { Radius = 25 });
            }

            artistNode.Children.Add(albNode);
        }

        // Physics 'kick' to wake up simulation if it settled
        foreach (var n in _graph.Nodes) n.Velocity = new SKPoint(1, 1);
    }

    // Helper class for Slider
    class ZoomListener : Java.Lang.Object, IBaseOnChangeListener
    {
        private readonly PanZoom _pz;
        private readonly View _view;
        public ZoomListener(PanZoom pz, View view) { _pz = pz; _view = view; }
        public void OnValueChange(Java.Lang.Object slider, float value, bool fromUser)
        {
            _pz.Scale = value;
            _view.Invalidate();
        }
    }
}
