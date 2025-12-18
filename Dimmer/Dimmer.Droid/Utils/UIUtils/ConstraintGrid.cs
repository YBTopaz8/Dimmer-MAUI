using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Android.Content;

using AndroidX.ConstraintLayout.Widget;
using AndroidX.Media3.Common;

using Google.Android.Material.Color.Utilities;

using static Android.Print.PrintAttributes;
using static Dimmer.Utils.UIUtils.ConstraintGrid;

using Grid = Dimmer.Utils.UIUtils.ConstraintGrid.Grid;
using GridLength = Dimmer.Utils.UIUtils.ConstraintGrid.GridLength;

namespace Dimmer.Utils.UIUtils;

public enum GridUnitType
{
    Auto,
    Star,
    Fixed
}
public class ConstraintGrid
{
   

    public record GridLength(GridUnitType Type, float Value = 1);

    public static class Grid
    {
        public static GridLength Auto => new(GridUnitType.Auto);
        public static GridLength Star(float v = 1) => new(GridUnitType.Star, v);
        public static GridLength Px(int px) => new(GridUnitType.Fixed, px);
    }

    public class GridCell
    {
        public View? Content;
        public int Row;
        public int Column;
        public int RowSpan = 1;
        public int ColumnSpan = 1;
        public GravityFlags Gravity = GravityFlags.Center;
    }



    private readonly ConstraintLayout _layout;
    private readonly Context _context;
    private readonly List<Guideline> _rowGuides = new();
    private readonly List<Guideline> _colGuides = new();
    public ConstraintGrid(Context context)
    {
        _context = context;
        _layout = new ConstraintLayout(context)
        {
            LayoutParameters = new ViewGroup.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent)
        };
    }

    public ConstraintLayout Layout => _layout;

    public void Setup(GridLength[] rows, GridLength[] cols, int spacing = 0)
    {
        _layout.RemoveAllViews();
        _rowGuides.Clear();
        _colGuides.Clear();

        // Create horizontal guidelines for rows
        float cumulativePercent = 0f;
        for (int r = 0; r < rows.Length; r++)
        {
            var guide = new Guideline(_context) { Id = View.GenerateViewId() };
            _layout.AddView(guide);

            float percent = rows[r].Type switch
            {
                GridUnitType.Auto => -1f, // will skip percent, use wrap_content
                GridUnitType.Star => rows[r].Value / TotalStar(rows),
                GridUnitType.Fixed => -1f,
                _ => -1f
            };

            var lp = new ConstraintLayout.LayoutParams(0, 0)
            {
                Orientation = 0
            };

            if (percent > 0)
            {
                cumulativePercent += percent;
                lp.GuidePercent = cumulativePercent;
            }

            guide.LayoutParameters = lp;
            _rowGuides.Add(guide);
        }

        // Create vertical guidelines for columns
        cumulativePercent = 0f;
        for (int c = 0; c < cols.Length; c++)
        {
            var guide = new Guideline(_context) { Id = View.GenerateViewId() };
            _layout.AddView(guide);

            float percent = cols[c].Type switch
            {
                GridUnitType.Auto => -1f,
                GridUnitType.Star => cols[c].Value / TotalStar(cols),
                GridUnitType.Fixed => -1f,
                _ => -1f
            };

            var lp = new ConstraintLayout.LayoutParams(0, 0)
            {
                Orientation = 1
            };

            if (percent > 0)
            {
                cumulativePercent += percent;
                lp.GuidePercent = cumulativePercent;
            }

            guide.LayoutParameters = lp;
            _colGuides.Add(guide);
        }
    }

    private static float TotalStar(GridLength[] defs)
    {
        float total = 0;
        foreach (var g in defs)
            if (g.Type == GridUnitType.Star) total += g.Value;
        return total;
    }

    public void AddCell(GridCell cell)
    {
        if (cell == null) return;
        if (cell.Content == null) return;

        if (cell.Content.Parent != null)
            ((ViewGroup)cell.Content.Parent).RemoveView(cell.Content);

        _layout.AddView(cell.Content);

        var set = new ConstraintSet();
        set.Clone(_layout);

        // Horizontal constraints
        int colStart = cell.Column == 0 ? ConstraintSet.ParentId : _colGuides[cell.Column - 1].Id;
        int colEnd = cell.Column + cell.ColumnSpan - 1 >= _colGuides.Count ? ConstraintSet.ParentId : _colGuides[cell.Column + cell.ColumnSpan - 1].Id;

        set.Connect(cell.Content.Id, ConstraintSet.Left, colStart, ConstraintSet.Left);
        set.Connect(cell.Content.Id, ConstraintSet.Right, colEnd, ConstraintSet.Left);

        // Vertical constraints
        int rowStart = cell.Row == 0 ? ConstraintSet.ParentId : _rowGuides[cell.Row - 1].Id;
        int rowEnd = cell.Row + cell.RowSpan - 1 >= _rowGuides.Count ? ConstraintSet.ParentId : _rowGuides[cell.Row + cell.RowSpan - 1].Id;

        set.Connect(cell.Content.Id, ConstraintSet.Top, rowStart, ConstraintSet.Top);
        set.Connect(cell.Content.Id, ConstraintSet.Bottom, rowEnd, ConstraintSet.Top);

        // Optional: Gravity
        set.SetVerticalBias(cell.Content.Id, GetVerticalBias(cell.Gravity));
        set.SetHorizontalBias(cell.Content.Id, GetHorizontalBias(cell.Gravity));

        set.ApplyTo(_layout);
    }

    private static float GetHorizontalBias(GravityFlags g)
    {
        return g.HasFlag(GravityFlags.Left) ? 0f :
               g.HasFlag(GravityFlags.Right) ? 1f : 0.5f;
    }

    private static float GetVerticalBias(GravityFlags g)
    {
        return g.HasFlag(GravityFlags.Top) ? 0f :
               g.HasFlag(GravityFlags.Bottom) ? 1f : 0.5f;
    }
}


public static class ConstraintGridHelper
{
    /// <summary>
    /// Build a ConstraintGrid from WinUI-like row/column definitions and cells.
    /// </summary>
    /// <param name="context">Android context</param>
    /// <param name="rowDefs">Row definitions, e.g. "Auto", "*", "2*"</param>
    /// <param name="colDefs">Column definitions, e.g. "Auto", "*", "3*"</param>
    /// <param name="cells">Cells with content, row/col, span, gravity</param>
    /// <param name="spacing">Optional spacing/padding between cells</param>
    /// <returns>Fully built ConstraintGrid</returns>
    public static ConstraintGrid Build(Context context, string[] rowDefs, string[] colDefs, IEnumerable<GridCell> cells, int spacing = 0)
    {
        var rows = ParseDefinitions(rowDefs);
        var cols = ParseDefinitions(colDefs);

        var grid = new ConstraintGrid(context);
        grid.Setup(rows, cols, spacing);

        foreach (var cell in cells)
        {
            grid.AddCell(cell);
        }

        return grid;
    }

    /// <summary>
    /// Parse WinUI-like string definition to GridLength
    /// </summary>
    private static GridLength[] ParseDefinitions(string[] defs)
    {
        var result = new GridLength[defs.Length];

        for (int i = 0; i < defs.Length; i++)
        {
            string def = defs[i].Trim();

            if (string.Equals(def, "Auto", StringComparison.OrdinalIgnoreCase))
            {
                result[i] = Grid.Auto;
            }
            else if (def.EndsWith("*"))
            {
                // e.g. "*" = 1*, "2*" = 2 star
                string starPart = def.Substring(0, def.Length - 1);
                float value = 1;
                if (!string.IsNullOrEmpty(starPart))
                    float.TryParse(starPart, out value);
                result[i] = Grid.Star(value);
            }
            else if (int.TryParse(def, out int px))
            {
                result[i] = Grid.Px(px);
            }
            else
            {
                // fallback
                result[i] = Grid.Star(1);
            }
        }

        return result;
    }
}