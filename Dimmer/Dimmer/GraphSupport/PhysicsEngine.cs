using SkiaSharp;

namespace Dimmer.GraphSupport;


public class PhysicsEngine
{
    readonly Graph graph;
    const float springStiffness = 150f;
    const float springRest = 140f;
    const float repulsionStrength = 9000f;
    const float DAMPING = 0.50f;
    const float MAX_SPEED = 50f; 
    const float CENTER_GRAVITY = 0.05f;

    const float SPRING_LEN = 150f;      // Ideal length of edges
    const float SPRING_K = 0.05f;       // Strength of edges (Lower = looser)
    public PhysicsEngine(Graph graph)
    {
        this.graph = graph;
        if (!graph.Nodes.Contains(graph.Root)) graph.Nodes.Add(graph.Root);
    }

    public void Update(float dt)
    {
        foreach (var n in graph.Nodes)
        {
            n.Force = SKPoint.Empty;

            // Pull stragglers back to center so graph doesn't drift
            if (!n.IsFixed)
            {
                n.Force.X -= n.Position.X * CENTER_GRAVITY;
                n.Force.Y -= n.Position.Y * CENTER_GRAVITY;
            }
        }
        var nodes = graph.Nodes.ToArray();
        for (int i = 0; i < nodes.Length; i++)
        {
            var a = nodes[i];
            for (int j = i + 1; j < nodes.Length; j++)
            {
                var b = nodes[j];
                ApplyRepulsion(a, b);
            }
        }

        // 3. Springs (Parents pull Children)
        foreach (var n in nodes)
        {
            foreach (var child in n.Children)
            {
                if (graph.Nodes.Contains(child))
                {
                    ApplySpring(n, child);
                }
            }
        }

        // 4. Apply Physics to Position
        foreach (var n in nodes)
        {
            // If Fixed (Root), don't move
            if (n.IsFixed)
            {
                n.Velocity = SKPoint.Empty;
                continue;
            }

            // F = ma (assuming mass = 1) -> v += F * dt
            n.Velocity.X += n.Force.X * dt;
            n.Velocity.Y += n.Force.Y * dt;

            // Apply Damping (Friction)
            n.Velocity.X *= DAMPING;
            n.Velocity.Y *= DAMPING;

            // Clamp Velocity (Prevent explosions)
            n.Velocity.X = Math.Clamp(n.Velocity.X, -MAX_SPEED, MAX_SPEED);
            n.Velocity.Y = Math.Clamp(n.Velocity.Y, -MAX_SPEED, MAX_SPEED);

            // Apply to Position
            n.Position.X += n.Velocity.X;
            n.Position.Y += n.Velocity.Y;
        }
    }


    static void ApplySpring(Node a, Node b)
    {
        var dx = b.Position.X - a.Position.X;
        var dy = b.Position.Y - a.Position.Y;
        var dist = (float)Math.Sqrt(dx * dx + dy * dy);

        if (dist < 0.1f) dist = 0.1f; // Prevent div by zero

        // Hooke's Law: F = -k * (x - L)
        var displacement = dist - SPRING_LEN;
        var force = SPRING_K * displacement;

        var fx = (dx / dist) * force;
        var fy = (dy / dist) * force;

        a.Force.X += fx;
        a.Force.Y += fy;
        b.Force.X -= fx;
        b.Force.Y -= fy;
    }

    static void ApplyRepulsion(Node a, Node b)
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