using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SkiaSharp;

namespace Dimmer.ViewsAndPages.GraphSupport;


public class PhysicsEngine
{
    readonly Graph graph;
    const float springStiffness = 200f;
    const float springRest = 140f;
    const float repulsionStrength = 9000f;
    const float damping = 0.85f;
    const float maxMove = 100f;

    public PhysicsEngine(Graph graph)
    {
        this.graph = graph;
        if (!graph.Nodes.Contains(graph.Root)) graph.Nodes.Add(graph.Root);
    }

    public void Update(float dt)
    {
        foreach (var n in graph.Nodes) n.Force = new SKPoint(0, 0);

        // parent-child springs
        foreach (var n in graph.Nodes)
            foreach (var child in n.Children)
                if (graph.Nodes.Contains(child)) ApplySpring(n, child, springRest, springStiffness);

        // repulsion
        var nodes = graph.Nodes;
        for (int i = 0; i < nodes.Count; i++)
            for (int j = i + 1; j < nodes.Count; j++)
                ApplyRepulsion(nodes[i], nodes[j]);

        foreach (var n in graph.Nodes)
        {
            var ax = n.Force.X;
            var ay = n.Force.Y;
            n.Velocity = new SKPoint((n.Velocity.X + ax * dt) * damping, (n.Velocity.Y + ay * dt) * damping);
            var vx = System.Math.Max(-maxMove, System.Math.Min(maxMove, n.Velocity.X));
            var vy = System.Math.Max(-maxMove, System.Math.Min(maxMove, n.Velocity.Y));
            n.Velocity = new SKPoint(vx, vy);
            n.Position = new SKPoint(n.Position.X + n.Velocity.X * dt * 60f, n.Position.Y + n.Velocity.Y * dt * 60f);
        }
    }

    void ApplySpring(Node a, Node b, float rest, float k)
    {
        var dx = b.Position.X - a.Position.X;
        var dy = b.Position.Y - a.Position.Y;
        var dist = (float)System.Math.Sqrt(dx * dx + dy * dy);
        if (dist < 0.001f) dist = 0.001f;
        var diff = dist - rest;
        var fx = (k * diff) * (dx / dist);
        var fy = (k * diff) * (dy / dist);
        a.Force = new SKPoint(a.Force.X + fx, a.Force.Y + fy);
        b.Force = new SKPoint(b.Force.X - fx, b.Force.Y - fy);
    }

    void ApplyRepulsion(Node a, Node b)
    {
        var dx = b.Position.X - a.Position.X;
        var dy = b.Position.Y - a.Position.Y;
        var dist2 = dx * dx + dy * dy;
        if (dist2 < 0.0001f) { dx = 0.1f; dy = 0.1f; dist2 = dx * dx + dy * dy; }
        var invDist = 1.0f / (float)System.Math.Sqrt(dist2);
        var force = repulsionStrength / dist2;
        var fx = -force * dx * invDist;
        var fy = -force * dy * invDist;
        a.Force = new SKPoint(a.Force.X + fx, a.Force.Y + fy);
        b.Force = new SKPoint(b.Force.X - fx, b.Force.Y - fy);
    }
}

public class PanZoom
{
    public float OffsetX = 0;
    public float OffsetY = 0;
    public float Scale = 1f;
}

public class Renderer
{
    readonly Graph graph;
    SKPaint nodePaint = new SKPaint { IsAntialias = true };
    SKPaint textPaint = new SKPaint { IsAntialias = true, TextSize = 20, Color = SKColors.White, TextAlign = SKTextAlign.Center };
    SKPaint edgePaint = new SKPaint { IsAntialias = true, StrokeWidth = 4, Style = SKPaintStyle.Stroke, Color = SKColors.LightGray };
    SKPaint rootPaint = new SKPaint { IsAntialias = true, Color = SKColors.DarkOrange };
    SKPaint albumPaint = new SKPaint { IsAntialias = true, Color = SKColors.MediumPurple };
    SKPaint songPaint = new SKPaint { IsAntialias = true, Color = SKColors.SeaGreen };

    public Renderer(Graph g) { graph = g; }

    public void Draw(SKCanvas canvas, int width, int height, PanZoom panZoom)
    {
        canvas.Save();
        canvas.Translate(width / 2f + panZoom.OffsetX, height / 2f + panZoom.OffsetY);
        canvas.Scale(panZoom.Scale);

        foreach (var n in graph.Nodes)
            foreach (var c in n.Children)
                if (graph.Nodes.Contains(c))
                    canvas.DrawLine(n.Position.X, n.Position.Y, c.Position.X, c.Position.Y, edgePaint);

        foreach (var n in graph.Nodes.OrderBy(n => n.Radius)) DrawNode(canvas, n);
        canvas.Restore();
    }

    void DrawNode(SKCanvas canvas, Node n)
    {
        SKPaint p = songPaint;
        if (n == graph.Root) p = rootPaint;
        else if (n.Children.Count > 0) p = albumPaint;

        canvas.DrawCircle(n.Position.X, n.Position.Y, n.Radius, p);
        canvas.DrawText(n.Label, n.Position.X, n.Position.Y + n.Radius / 3f, textPaint);
    }

    public SKPoint ScreenToWorld(float sx, float sy, PanZoom panZoom)
    {
        var wx = (sx - panZoom.OffsetX) / panZoom.Scale;
        var wy = (sy - panZoom.OffsetY) / panZoom.Scale;
        return new SKPoint(wx, wy);
    }
}