using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.ViewsAndPages.NativeViews.Misc;

public partial class ArcFabAndroid : FrameLayout
{
    private bool _isOpen = false;
    private Button _mainFab;
    private View _dimmer;
    private List<Button> _subButtons = new List<Button>();

    // Configuration
    private double _baseAngle = 180; // Left
    private int _fabSizePx;
    private int _subFabSizePx;
    private int _marginPx;

    public event EventHandler<string> ItemClicked;

    public ArcFabAndroid(Context context, IAttributeSet attrs) : base(context, attrs)
    {
        Initialize(context);
    }

    public ArcFabAndroid(Context context) : base(context)
    {
        Initialize(context);
    }

    private void Initialize(Context context)
    {
        // Convert DP to Pixels for sizing
        var density = Resources?.DisplayMetrics?.Density;
        if (density != null)
        {

            _fabSizePx = (int)(60 * density);
            _subFabSizePx = (int)(50 * density);
            _marginPx = (int)(20 * density);
        }
        // 1. Create Dimmer (Overlay)
        _dimmer = new View(context);
        _dimmer.SetBackgroundColor(Color.Black);
        _dimmer.Alpha = 0;
        _dimmer.Visibility = ViewStates.Gone;
        _dimmer.Click += (s, e) => CloseMenu();

        // Layout Params: Match Parent
        var dimmerParams = new LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
        AddView(_dimmer, dimmerParams);

        // 2. Create Main FAB
        _mainFab = CreateRoundButton(context, _fabSizePx, Color.ParseColor("#0078D7"), "+");
        _mainFab.TextSize = 24;
        _mainFab.Click += (s, e) => ToggleMenu();

        // Layout Params: Bottom Right
        var mainParams = new LayoutParams(_fabSizePx, _fabSizePx)
        {
            Gravity = GravityFlags.Bottom | GravityFlags.Right
        };
        mainParams.SetMargins(0, 0, _marginPx, _marginPx);


        AddView(_mainFab, mainParams);
    }

    /// <summary>
    /// Call this to populate the menu
    /// </summary>
    public void SetMenuItems(List<string> items)
    {
        // Remove old sub-buttons if any
        foreach (var btn in _subButtons) RemoveView(btn);
        _subButtons.Clear();

        // Create new buttons
        for (int i = 0; i < items.Count; i++)
        {
            string text = items[i];
            var btn = CreateRoundButton(Context!, _subFabSizePx, Color.Gray, text);
            btn.Tag = text; // Store string in Tag
            btn.Click += SubBtn_Click;

            // Initial State: Hidden and at (0,0) relative to the container
            // We actually want them centered behind the Main FAB initially
            btn.Alpha = 0;
            btn.Visibility = ViewStates.Invisible;

            // Layout Params: Same as Main FAB (Stacked behind it)
            var paramsBtn = new LayoutParams(_subFabSizePx, _subFabSizePx)
            {
                Gravity = GravityFlags.Bottom | GravityFlags.Right
            };
            // Offset margins slightly to center it behind the larger Main FAB
            int offset = (_fabSizePx - _subFabSizePx) / 2;
            paramsBtn.SetMargins(0, 0, _marginPx + offset, _marginPx + offset);

            // Insert at index 1 (Above Dimmer, Below Main FAB)
            AddView(btn, 1, paramsBtn);
            _subButtons.Add(btn);
        }
    }

    private void SubBtn_Click(object? sender, EventArgs e)
    {
        if (sender is View v && v.Tag != null)
        {
            ItemClicked?.Invoke(this, v.Tag.ToString());
        }
        CloseMenu();
    }

    private Button CreateRoundButton(Context ctx, int size, Color color, string text)
    {
       
            var btn = new Button(ctx);
            btn.Text = text;
            btn.SetTextColor(Color.White);

            // Create Circle Drawable programmatically
            var shape = new GradientDrawable();
            shape.SetShape(ShapeType.Oval);
            shape.SetColor(color);

            btn.Background = shape;
            btn.Elevation = 10; // Shadow
            btn.StateListAnimator = null; // Remove default press sink if desired
            return btn;
        
    }
    private void ToggleMenu() { if (_isOpen) CloseMenu(); else OpenMenu(); }

    private void OpenMenu()
    {
        _isOpen = true;
        _dimmer.Visibility = ViewStates.Visible;
        _dimmer.Animate()?.Alpha(0.6f).SetDuration(200).Start();
        _mainFab.Animate()?.Rotation(45f).SetDuration(200).Start();

        double radius = 300; // px
        double spacing = 25; // degrees

        for (int i = 0; i < _subButtons.Count; i++)
        {
            var btn = _subButtons[i];
            btn.Visibility = ViewStates.Visible; // Make visible for animation

            // Math
            double offsetMultiplier = (i == 0) ? 0 : (i % 2 == 1) ? (i + 1) / 2.0 : -(i / 2.0);
            double angleRad = (_baseAngle + (offsetMultiplier * spacing)) * (Math.PI / 180);

            // Important: In Android View Coords, Negative Y is UP.
            // But TranslationY is relative. 
            // Cos(180) = -1 (Left). Sin(180) = 0.
            // We need to calculate offsets relative to the bottom-right corner.
            float finalX = (float)(radius * Math.Cos(angleRad));
            float finalY = (float)(radius * Math.Sin(angleRad));

            btn.Animate()?
               .TranslationX(finalX)
               .TranslationY(finalY)
               .Alpha(1.0f)
               .SetDuration(350)
               .SetStartDelay(i * 30)
               .SetInterpolator(new OvershootInterpolator(1.2f))
               .Start();
        }
    }

    private void CloseMenu()
    {
        _isOpen = false;
        _dimmer.Animate()?.Alpha(0f).SetDuration(200)
               .WithEndAction(new Java.Lang.Runnable(() => _dimmer.Visibility = ViewStates.Gone))
               .Start();
        _mainFab.Animate()?.Rotation(0f).SetDuration(200).Start();

        foreach (var btn in _subButtons)
        {
            btn.Animate()?
               .TranslationX(0)
               .TranslationY(0)
               .Alpha(0f)
               .SetDuration(250)
               .SetInterpolator(new AccelerateInterpolator())
               .Start();
        }
    }
}
