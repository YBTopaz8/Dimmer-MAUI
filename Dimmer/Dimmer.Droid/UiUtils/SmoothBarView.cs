using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Controls.PlatformConfiguration;

namespace Dimmer.UiUtils;

using Android.Animation;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Util;
using Android.Views;
using Android.Views.Animations;
using AndroidX.CoordinatorLayout.Widget;
using AndroidX.Core.Graphics.Drawable;
using AndroidX.Core.View;
using Google.Android.Material.Behavior;
using System;
using System.Collections.Generic;
using System.Linq;


    // --- 1. ENUMS & MODELS ---

    public enum BadgeType { Circle, Box }
    public enum TextOrientation { Side, Bottom }

    public class Badge
    {
        public float BadgeSize { get; set; } = 30f;
        public string BadgeText { get; set; } = "";
        public Color BadgeColor { get; set; } = Color.Red;
        public Color BadgeTextColor { get; set; } = Color.White;
        public float BadgeBoxCornerRadius { get; set; } = 8f;
        public BadgeType BadgeType { get; set; } = BadgeType.Circle;
    }

    public class BottomBarItem
    {
        public string Title { get; set; }
        public Drawable? Icon { get; set; }
        public Drawable? SelectedIcon { get; set; } // #105 Support diff icon on select
        public RectF Rect { get; set; } = new RectF();
        public int Alpha { get; set; } = 0;
        public Badge? Badge { get; set; }
        public bool IsVisible { get; set; } = true; // Dynamic removal support
        public bool IsEnabled { get; set; } = true; // #Disable specific item

        public BottomBarItem(string title, Drawable? icon, Drawable? selectedIcon = null)
        {
            Title = title;
            Icon = icon;
            SelectedIcon = selectedIcon ?? icon;
        }
    }

    // --- 2. THE MAIN VIEW ---

    // #86 Hide on Scroll Behavior Link
    public class SmoothBottomBar : View
    {
    private float _stretchFactor = 0f; // 0 = normal, 1 = max stretch
    private float _iconScale = 1.0f;
    private float _textScale = 0.0f;
    private float _iconRotation = 0.0f;

    private Typeface _customTypeface;

    public void SetTypeface(Typeface tf)
    {
        _customTypeface = tf;
        _paintText.SetTypeface(tf);
        _paintBadgeText.SetTypeface(tf);
        Invalidate();
    }
    // --- Constants ---
    private const int OPAQUE = 255;
        private const int TRANSPARENT = 0;
        private const long DEFAULT_ANIM_DURATION = 300;

        // --- Settings ---
        private int _barBackgroundColor = Color.ParseColor("#2D2D30");
        private int _indicatorColor = Color.ParseColor("#861B2D");
        private float _indicatorRadius;
        private float _sideMargins;
        private float _barCornerRadius = 0f;
        private float _itemPadding;
        private float _iconSize;
        private float _iconMargin;
        private float _textSize;
        private Color _textColor = Color.White;
        private Color _iconTint = Color.ParseColor("#C8FFFFFF");
        private Color _iconTintActive = Color.White;

        // New Features
        private TextOrientation _orientation = TextOrientation.Side; // #78 Text Position
        private bool _isBarEnabled = true; // Disable all
        private float _indicatorWidthFactor = 1.0f; // Resize indicator width

        // --- State ---
        private List<BottomBarItem> _items = new List<BottomBarItem>();
        private int _activeItemIndex = 0;
        private float _itemWidth;
        private float _indicatorLocation;
        private Color _currentIconTint;

        // --- Drawing ---
        private Paint _paintIndicator;
        private Paint _paintText;
        private Paint _paintBadge;
        private Paint _paintBadgeText;
        private Paint _paintBackground;
        private RectF _rect = new RectF();
        private Path _backgroundPath = new Path();

        // --- Events ---
        public event EventHandler<int> OnItemSelected;
        public event EventHandler<int> OnItemReselected;

        // --- Constructors ---
        public SmoothBottomBar(Context context) : base(context) { Init(); }
        public SmoothBottomBar(Context context, IAttributeSet attrs) : base(context, attrs) { Init(); }
        public SmoothBottomBar(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr) { Init(); }

        private void Init()
        {
            var metrics = Resources.DisplayMetrics;
            _indicatorRadius = TypedValue.ApplyDimension(ComplexUnitType.Dip, 20f, metrics);
            _sideMargins = TypedValue.ApplyDimension(ComplexUnitType.Dip, 10f, metrics);
            _itemPadding = TypedValue.ApplyDimension(ComplexUnitType.Dip, 10f, metrics);
            _iconSize = TypedValue.ApplyDimension(ComplexUnitType.Dip, 20f, metrics);
            _iconMargin = TypedValue.ApplyDimension(ComplexUnitType.Dip, 4f, metrics);
            _textSize = TypedValue.ApplyDimension(ComplexUnitType.Dip, 12f, metrics);

            _currentIconTint = _iconTintActive;

            _paintIndicator = new Paint(PaintFlags.AntiAlias) { Color = new Color(_indicatorColor)};
            _paintIndicator.SetStyle(Paint.Style.Fill );
            _paintText = new Paint(PaintFlags.AntiAlias) { Color = _textColor, TextSize = _textSize, TextAlign = Paint.Align.Center, FakeBoldText = true };
        _paintIndicator.SetStyle(Paint.Style.Fill);
        _paintBadge = new Paint(PaintFlags.AntiAlias) { StrokeWidth = 4f };
        _paintBadge.SetStyle(Paint.Style.Fill);
        _paintBadgeText = new Paint(PaintFlags.AntiAlias) { TextSize = _textSize, TextAlign = Paint.Align.Center, FakeBoldText = true };
            _paintBackground = new Paint(PaintFlags.AntiAlias) { Color = new Color(_barBackgroundColor)};
        _paintBackground.SetStyle(Paint.Style.Fill);
    }

        // --- PUBLIC API ---

        public void SetMenuItems(List<BottomBarItem> items)
        {
            _items = items;
            _activeItemIndex = 0;
            RequestLayout();
            Invalidate();
        }

        // #111 Get Item to modify dynamically
        public BottomBarItem GetItem(int index) => (index >= 0 && index < _items.Count) ? _items[index] : null;

        // Dynamic removal/update
        public void RemoveItem(int index)
        {
            if (index >= 0 && index < _items.Count)
            {
                _items.RemoveAt(index);
                if (_activeItemIndex >= _items.Count) _activeItemIndex = _items.Count - 1;
                RequestLayout();
                Invalidate();
            }
        }

        // Missing Setter for Corner Radius
        public void SetBarCornerRadius(float radiusDp)
        {
            _barCornerRadius = TypedValue.ApplyDimension(ComplexUnitType.Dip, radiusDp, Resources.DisplayMetrics);
            Invalidate();
        }

        // Feature: Custom Indicator Width
        public void SetIndicatorWidthFactor(float factor)
        {
            _indicatorWidthFactor = factor;
            Invalidate();
        }

        // Feature: Text Position (#78)
        public void SetItemOrientation(TextOrientation orientation)
        {
            _orientation = orientation;
            RequestLayout();
            Invalidate();
        }

        // Feature: Disable Bar
        public void SetBarEnabled(bool enabled)
        {
            _isBarEnabled = enabled;
            Alpha = enabled ? 1.0f : 0.5f;
        }

        // --- LAYOUT ---
        protected override void OnSizeChanged(int w, int h, int oldw, int oldh)
        {
            base.OnSizeChanged(w, h, oldw, oldh);
            RecalculateItems();
        }

        private void RecalculateItems()
        {
            if (_items.Count == 0) return;

            // Filter invisible items logic could go here, but for now we assume List reflects visible items
            int visibleCount = _items.Count(i => i.IsVisible);
            if (visibleCount == 0) return;

            float lastX = _sideMargins;
            _itemWidth = (Width - (_sideMargins * 2)) / visibleCount;

            foreach (var item in _items)
            {
                if (!item.IsVisible) continue;
                item.Rect = new RectF(lastX, 0f, _itemWidth + lastX, Height);
                lastX += _itemWidth;
            }

            // Reset Position
            if (_activeItemIndex < _items.Count)
            {
                _indicatorLocation = _items[_activeItemIndex].Rect.Left;
                // Init Alpha
                for (int i = 0; i < _items.Count; i++) _items[i].Alpha = (i == _activeItemIndex) ? OPAQUE : TRANSPARENT;
            }
        }

        // --- DRAWING ---
        protected override void OnDraw(Canvas canvas)
        {
            // 1. Background
            _backgroundPath.Reset();
            var cr = _barCornerRadius;
            _backgroundPath.AddRoundRect(new RectF(0, 0, Width, Height),
                new float[] { cr, cr, cr, cr, 0, 0, 0, 0 }, Path.Direction.Cw);
            canvas.DrawPath(_backgroundPath, _paintBackground);

            if (_items.Count == 0) return;

            // 2. Indicator
            var activeItem = _items[_activeItemIndex];
            _rect.Left = _indicatorLocation;
            _rect.Right = _indicatorLocation + _itemWidth;

            // Apply custom width factor to indicator
            float actualWidth = _rect.Width();
            float desiredWidth = actualWidth * _indicatorWidthFactor;
            float diff = (actualWidth - desiredWidth) / 2;
            _rect.Left += diff;
            _rect.Right -= diff;

            // Indicator Y position depends on Orientation
            if (_orientation == TextOrientation.Side)
            {
                _rect.Top = activeItem.Rect.CenterY() - (_iconSize / 2) - _itemPadding;
                _rect.Bottom = activeItem.Rect.CenterY() + (_iconSize / 2) + _itemPadding;
            }
            else // Bottom
            {
                // In Bottom mode, indicator usually looks better a bit taller or just centered
                _rect.Top = activeItem.Rect.CenterY() - (_iconSize / 2) - _itemPadding;
                _rect.Bottom = activeItem.Rect.CenterY() + (_iconSize / 2) + _itemPadding;
            }

            canvas.DrawRoundRect(_rect, _indicatorRadius, _indicatorRadius, _paintIndicator);

            // 3. Items
            float textHeight = (_paintText.Descent() + _paintText.Ascent()) / 2;

            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                if (!item.IsVisible) continue;

                // Select correct icon (Selected vs Normal)
                var iconToDraw = (i == _activeItemIndex) ? item.SelectedIcon : item.Icon;

                float cx = item.Rect.CenterX();
                float cy = Height / 2f;
                float alphaFraction = (float)item.Alpha / OPAQUE;

                int iconL, iconT, iconR, iconB;

                if (_orientation == TextOrientation.Side)
                {
                    // SIDE LOGIC: Icon moves left, text appears right
                    float textLength = _paintText.MeasureText(item.Title);
                    float offset = (textLength / 2) * alphaFraction;

                    iconL = (int)(cx - (_iconSize / 2) - offset);
                    iconR = (int)(cx + (_iconSize / 2) - offset);
                    iconT = (int)(cy - (_iconSize / 2));
                    iconB = (int)(cy + (_iconSize / 2));

                    // Text
                    _paintText.Alpha = item.Alpha;
                    if (item.Alpha > 0)
                    {
                        // Handle Long Text - "Marquee" replacement (Truncate)
                        string displayTitle = TruncateText(item.Title, (_itemWidth / 2));
                        canvas.DrawText(displayTitle, cx + (_iconSize / 2) + _iconMargin, cy - textHeight, _paintText);
                    }
                }
                else
                {
                    // BOTTOM LOGIC: Icon moves up, text appears down
                    float moveUpAmount = (_textSize + _iconMargin) * alphaFraction; // 0 to full height

                    float adjustedCy = cy - (moveUpAmount / 2); // Move icon up

                    iconL = (int)(cx - (_iconSize / 2));
                    iconR = (int)(cx + (_iconSize / 2));
                    iconT = (int)(adjustedCy - (_iconSize / 2));
                    iconB = (int)(adjustedCy + (_iconSize / 2));

                    // Text
                    _paintText.Alpha = item.Alpha;
                    if (item.Alpha > 0)
                    {
                        string displayTitle = TruncateText(item.Title, _itemWidth - 10);
                        // Draw text below icon
                        canvas.DrawText(displayTitle, cx, adjustedCy + (_iconSize / 2) + _textSize + _iconMargin, _paintText);
                    }
                }

                iconToDraw.Mutate();
                iconToDraw.SetBounds(iconL, iconT, iconR, iconB);

                // Tint
                var tintColor = (i == _activeItemIndex) ? _currentIconTint : _iconTint;
                DrawableCompat.SetTint(iconToDraw, tintColor.ToArgb());
                iconToDraw.Draw(canvas);

                // Badge
                if (item.Badge != null) DrawBadge(canvas, item, cx, cy);
            }
        }

        private string TruncateText(string text, float maxWidth)
        {
            if (_paintText.MeasureText(text) <= maxWidth) return text;

            // Binary search or iterative trim for ellipsis
            // Simple approach:
            string ellipsis = "...";
            float ellipsisWidth = _paintText.MeasureText(ellipsis);

            for (int i = text.Length - 1; i > 0; i--)
            {
                string sub = text.Substring(0, i);
                if (_paintText.MeasureText(sub) + ellipsisWidth <= maxWidth)
                {
                    return sub + ellipsis;
                }
            }
            return ""; // Too small
        }

        private void DrawBadge(Canvas canvas, BottomBarItem item, float cx, float cy)
        {
            // Simple Badge Draw (same as before, adjusted for cleaner code)
            _paintBadge.Color = item.Badge.BadgeColor;
            float badgeCy = cy - (_iconSize / 2) - 10;
            float badgeCx = cx + (_iconSize / 2) + 4;
            float size = item.Badge.BadgeSize;

            if (item.Badge.BadgeType == BadgeType.Circle)
                canvas.DrawCircle(badgeCx, badgeCy, size, _paintBadge);
            else
            {
                var box = new RectF(badgeCx - size, badgeCy - size, badgeCx + size, badgeCy + size);
                canvas.DrawRoundRect(box, item.Badge.BadgeBoxCornerRadius, item.Badge.BadgeBoxCornerRadius, _paintBadge);
            }

            _paintBadgeText.Color = item.Badge.BadgeTextColor;
            float th = (_paintBadgeText.Descent() + _paintBadgeText.Ascent()) / 2;
            canvas.DrawText(item.Badge.BadgeText, badgeCx, badgeCy - th, _paintBadgeText);
        }

        // --- CLICK HANDLING ---
        public override bool OnTouchEvent(MotionEvent? e)
        {
            if (!_isBarEnabled) return false; // Disable Interaction
            if (e is null) return false; // Disable Interaction

            if (e.Action == MotionEventActions.Up && Math.Abs(e.DownTime - e.EventTime) < 500)
            {
                float x = e.GetX();
                float y = e.GetY();

                for (int i = 0; i < _items.Count; i++)
                {
                    var item = _items[i];
                    if (!item.IsVisible || !item.IsEnabled) continue; // Skip disabled items

                    if (item.Rect.Contains(x, y))
                    {
                        if (i != _activeItemIndex)
                        {
                            SetActiveItem(i);
                            OnItemSelected?.Invoke(this, i);
                        }
                        else
                        {
                            OnItemReselected?.Invoke(this, i);
                        }
                        PerformClick();
                    if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
                        PerformHapticFeedback(FeedbackConstants.Confirm);
                    else
                        PerformHapticFeedback(FeedbackConstants.VirtualKey);
                    return true;
                    }
                }
            }
            return true;
        }

        // Renamed from SetActiveItem to match your request for SetSelectedItem compatibility
        public void SetSelectedItem(int pos) => SetActiveItem(pos);

        public void SetActiveItem(int pos)
        {
            _activeItemIndex = pos;
            for (int i = 0; i < _items.Count; i++)
            {
                if (i == pos) AnimateAlpha(_items[i], OPAQUE);
                else AnimateAlpha(_items[i], TRANSPARENT);
            }
            AnimateIndicator(pos);
            AnimateIconTint();
        }

        // --- ANIMATIONS (Same as before) ---
        private void AnimateAlpha(BottomBarItem item, int to)
        {
            var anim = ValueAnimator.OfInt(item.Alpha, to);
            anim.SetDuration(DEFAULT_ANIM_DURATION);
            anim.Update += (s, e) => { item.Alpha = (int)e.Animation.AnimatedValue; Invalidate(); };
            anim.Start();
        }

        private void AnimateIndicator(int pos)
        {
            var anim = ValueAnimator.OfFloat(_indicatorLocation, _items[pos].Rect.Left);
            anim.SetDuration(DEFAULT_ANIM_DURATION);
            //anim.SetInterpolator(new DecelerateInterpolator());
        anim.SetInterpolator(new OvershootInterpolator(1.0f));
        anim.Update += (s, e) => { _indicatorLocation = (float)e.Animation.AnimatedValue; Invalidate(); };
            anim.Start();
        }

        private void AnimateIconTint()
        {
            var anim = ValueAnimator.OfArgb(_currentIconTint.ToArgb(), _iconTintActive.ToArgb());
            anim.SetDuration(DEFAULT_ANIM_DURATION);
            anim.Update += (s, e) => { _currentIconTint = new Color((int)e.Animation.AnimatedValue); };
            anim.Start();
        }

        // --- CONFIGURATION METHODS ---
        public void SetBarBackgroundColor(Color color) { _barBackgroundColor = color; _paintBackground.Color = color; Invalidate(); }
        public void SetIndicatorColor(Color color) { _indicatorColor = color; _paintIndicator.Color = color; Invalidate(); }
        public void SetTextColor(Color color) { _textColor = color; _paintText.Color = color; Invalidate(); }
        public void SetIconTint(Color normal, Color active) { _iconTint = normal; _iconTintActive = active; _currentIconTint = active; Invalidate(); }
    }

    // --- 3. SCROLL BEHAVIOR (Hide on Scroll) ---
    // Reference: #86 "Hide BottomView on Scroll"
    public class HideBottomViewOnScrollBehavior<V> : CoordinatorLayout.Behavior where V : View
    {
        private int height = 0;
        private int currentState = 2; // 2 = SCROLLED_UP (Visible)

        public HideBottomViewOnScrollBehavior() { }
        public HideBottomViewOnScrollBehavior(Context context, IAttributeSet attrs) : base(context, attrs) { }

    public override bool OnLayoutChild(CoordinatorLayout? parent, Java.Lang.Object? child, int layoutDirection)
    {
        var view = child as V;
        if (view != null)
        {
            height = view.Height;
        }
            return base.OnLayoutChild(parent, child, layoutDirection);
        
    }

        public override bool OnStartNestedScroll(CoordinatorLayout? coordinatorLayout, Java.Lang.Object? child, View? directTargetChild, View? target, int axes, int type)
        {
            return axes == ViewCompat.ScrollAxisVertical;
        }

    public override void OnNestedScroll(CoordinatorLayout? coordinatorLayout, Java.Lang.Object? child, View? target, int dxConsumed, int dyConsumed, int dxUnconsumed, int dyUnconsumed, int type, int[]? consumed)
    {
        var view = child as V;
        if (view != null)
        {
            if (dyConsumed > 0 && currentState == 2) // Scrolling Down -> Hide
            {
                SlideDown(view);
            }
            else if (dyConsumed < 0 && currentState == 1) // Scrolling Up -> Show
            {
                SlideUp(view);
            }
        }
    }

        private void SlideUp(V child)
        {
            child.ClearAnimation();
            child.Animate()?.TranslationY(0).SetDuration(200).Start();
            currentState = 2;
        }

        private void SlideDown(V child)
        {
            child.ClearAnimation();
            child.Animate()?.TranslationY(height + 30).SetDuration(200).Start(); // +30 for margins
            currentState = 1; // SCROLLED_DOWN
        }
    }

