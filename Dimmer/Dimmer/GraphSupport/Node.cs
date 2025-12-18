using SkiaSharp;

namespace Dimmer.GraphSupport;

public class Node
{
    public string Id;
    public string Label;
    public SKPoint Position;
    public SKPoint Velocity;
    public SKPoint Force;
    public SKColor Color;
    public float Radius = 40;
    public List<Node> Children = new List<Node>();
    public Node? Parent;
    public bool IsExpanded = false;
    public bool IsFixed = false;
    public Node(string id, string label)
    {
        Id = id;
        Label = label;
        Position = new SKPoint(0.1f, 0.1f);
        Velocity = new SKPoint(0, 0);
        Force = new SKPoint(0, 0);
    }
}

public class Graph
{
    public List<Node> Nodes = new List<Node>();
    public Node Root;

    public static Graph SampleGraph()
    {
        var g = new Graph();
        var artist = new Node("artist_1", "Artist A") { Position = new SKPoint(0, 0), Radius = 56 };
        g.Root = artist;
        g.Nodes.Add(artist);

        // lazy albums - not yet added to graph.Nodes until expanded
        artist.Children = Enumerable.Range(1, 6)
            .Select(i => new Node($"alb_{i}", $"Album {i}") { Radius = 42 }).ToList();

        // prepare songs for each album
        int sId = 1;
        foreach (var alb in artist.Children)
        {
            alb.Children = Enumerable.Range(1, 8)
                .Select(j => new Node($"song_{sId++}", $"Song {j}") { Radius = 28 }).ToList();
            foreach (var song in alb.Children) song.Children = new List<Node>(); // leaf
        }

        return g;
    }

    public void ExpandLazy(Node center)
    {
        if (center.IsExpanded) return;
        foreach (var c in center.Children)
        {
            if (!Nodes.Contains(c))
            {
                Nodes.Add(c);
                c.Parent = center;
                var angle = (float)(Nodes.Count * 0.618f % 1.0 * System.Math.PI * 2);
                c.Position = center.Position + new SKPoint((float)(120 * System.Math.Cos(angle)), (float)(120 * System.Math.Sin(angle)));
            }
        }
        center.IsExpanded = true;
    }

    public void Collapse(Node center)
    {
        if (!center.IsExpanded) return;
        var toRemove = new List<Node>();
        foreach (var c in center.Children)
        {
            CollectDescendants(c, toRemove);
            toRemove.Add(c);
        }
        foreach (var r in toRemove)
        {
            Nodes.Remove(r);
            r.Parent = null;
        }
        center.IsExpanded = false;
    }

    void CollectDescendants(Node n, List<Node> acc)
    {
        foreach (var c in n.Children)
        {
            CollectDescendants(c, acc);
            acc.Add(c);
        }
    }

    public Node PickNode(SKPoint worldPoint, float hitRadius)
    {
        Node best = null;
        var bestD2 = float.MaxValue;
        foreach (var n in Nodes)
        {
            var dx = n.Position.X - worldPoint.X;
            var dy = n.Position.Y - worldPoint.Y;
            var d2 = dx * dx + dy * dy;
            var r = (n.Radius + hitRadius);
            if (d2 <= r * r && d2 < bestD2)
            {
                best = n; bestD2 = d2;
            }
        }
        return best;
    }
}