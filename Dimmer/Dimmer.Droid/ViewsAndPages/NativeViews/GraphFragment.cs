using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Android.OS;

using AndroidX.AutoFill.Inline;
using AndroidX.Fragment.App;

using Dimmer.ViewsAndPages.GraphSupport;

using SkiaSharp.Views.Android;

using static AndroidX.ConstraintLayout.Core.State.State;

using Renderer = Dimmer.ViewsAndPages.GraphSupport.Renderer;
using View = Android.Views.View;

namespace Dimmer.ViewsAndPages.NativeViews;


public class GraphFragment : Fragment
{
    private SKCanvasView? _canvasView;
    private Graph? graph;
    private PhysicsEngine? physics;
    private Renderer? renderer;
    private PanZoom? panZoom;

    public override View OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        var ctx = Context!;
        var root = new FrameLayout(ctx)
        {
            LayoutParameters = new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)
        };
        root.SetBackgroundColor(Android.Graphics.Color.Black);

        // SKCanvasView
        _canvasView = new SKCanvasView(ctx)
        {
            LayoutParameters = new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent),
            TransitionName = "artistImage" // same shared element
        };
        root.AddView(_canvasView);

        graph = Graph.SampleGraph();
        physics = new PhysicsEngine(graph);
        renderer = new Renderer(graph);
        panZoom = new PanZoom();

        _canvasView.PaintSurface += (s, e) => renderer.Draw(e.Surface.Canvas, e.Info.Width, e.Info.Height, panZoom);
        _canvasView.Clickable = true;
        _canvasView.SetFocusable(ViewFocusability.Focusable);
        _canvasView.FocusableInTouchMode = true;
        //_canvasView.SetFocusableInTouchMode(true);
        _canvasView.Touch += Canvas_Touch;

        // Start simple physics loop
        var handler = new Handler(Looper.MainLooper!);
        void Tick()
        {
            physics.Update(0.016f);
            _canvasView.Invalidate();
            handler.PostDelayed(Tick, 16);
        }
        handler.Post(Tick);

        return root;
    }

    private void Canvas_Touch(object? sender, View.TouchEventArgs e)
    {
        var me = e.Event;
        if (me != null && renderer != null && graph != null && panZoom != null)
        {
            if (me.ActionMasked == MotionEventActions.Up)
            {
                var pt = renderer.ScreenToWorld(me.GetX(), me.GetY(), panZoom);
                var node = graph.PickNode(pt, 28);
                if (node != null)
                {
                    if (!node.IsExpanded)
                        graph.ExpandLazy(node);
                    else
                        graph.Collapse(node);
                }
            }
        }
    }
}