using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.ViewsAndPages.ViewUtils;


public class MorphMenu
{
    private readonly Context _ctx;
    private readonly ViewGroup _parentRoot; // The container (CoordinatorLayout/FrameLayout)
    private readonly View _anchorFab;       // The FAB that morphs

    private MaterialCardView? _menuCard;
    private FrameLayout? _overlay;
    private bool _isOpen = false;

    // The list of items to render
    private readonly List<MenuItem> _menuItems = new();

    // Simple data holder
    private record MenuItem(string Text, int IconRes, Action OnClick);

    public MorphMenu(Context ctx, ViewGroup parentRoot, View anchorFab)
    {
        _ctx = ctx;
        _parentRoot = parentRoot;
        _anchorFab = anchorFab;
    }

    // --- Configuration API ---

    public MorphMenu AddItem(string text, int iconRes, Action onClick)
    {
        _menuItems.Add(new MenuItem(text, iconRes, onClick));
        return this; // Allows chaining
    }

    public void Show()
    {
        if (_isOpen) return;

        // Lazy creation: We only build the UI the first time we show it
        if (_menuCard == null) BuildMenuUi();

        // 1. Setup the Morph Transition (FAB -> CARD)
        var transform = new MaterialContainerTransform
        {
            StartView = _anchorFab,
            EndView = _menuCard,
            ScrimColor = Android.Graphics.Color.Transparent, // We use our own overlay
            ContainerColor = Android.Graphics.Color.ParseColor("#1E1E1E"), // Menu Background Color
            FadeMode = MaterialContainerTransform.FadeModeThrough,
            PathMotion = new MaterialArcMotion() // The arc is crucial for the "Morph" feel
        };
        transform.SetDuration(350);
        // 2. Begin Transition
        TransitionManager.BeginDelayedTransition(_parentRoot, transform);

        // 3. Swap Visibility
        _anchorFab.Visibility = ViewStates.Invisible; // Hide FAB
        _menuCard!.Visibility = ViewStates.Visible;   // Show Menu

        if (_overlay != null)
        {
            _overlay.Visibility = ViewStates.Visible;

            // We animate the background color alpha manually to separate it from the Morph
            var color = Android.Graphics.Color.ParseColor("#AA000000");
            _overlay.SetBackgroundColor(Android.Graphics.Color.Transparent); // Reset

            // Simple value animator for background dim (Optional polish)
            _overlay.Background?.Alpha = 0;
            _overlay.SetBackgroundColor(color);
        }

        _isOpen = true;
    }

    public void Hide()
    {
        if (!_isOpen || _menuCard == null) return;

        // 1. Setup Reverse Morph (CARD -> FAB)
        var transform = new MaterialContainerTransform
        {
            StartView = _menuCard,
            EndView = _anchorFab,
            ScrimColor = Android.Graphics.Color.Transparent,
            ContainerColor = Android.Graphics.Color.ParseColor("#1E1E1E"),
            FadeMode = MaterialContainerTransform.FadeModeThrough,
           
            PathMotion = new MaterialArcMotion()
        };
        transform.SetDuration(300);

        // 2. Begin Transition
        TransitionManager.BeginDelayedTransition(_parentRoot, transform);

        // 3. Swap Visibility
        _menuCard.Visibility = ViewStates.Invisible;
        _anchorFab.Visibility = ViewStates.Visible;

        if (_overlay != null)
        {
            _overlay.Animate()?.Alpha(0f)?.SetDuration(200)?.WithEndAction(new Java.Lang.Runnable(() =>
            {
                _overlay.Visibility = ViewStates.Gone;
                _overlay.Alpha = 1f;
            }))?.Start();
        }

        _isOpen = false;
    }

    // --- UI Construction ---

    private void BuildMenuUi()
    {
        // 1. The Overlay (Dim Background)
        _overlay = new FrameLayout(_ctx)
        {
            Clickable = true, // Catch clicks
            Focusable = true,
            LayoutParameters = new ViewGroup.LayoutParams(-1, -1)
        };
        _overlay.SetBackgroundColor(Android.Graphics.Color.ParseColor("#AA000000")); // 66% Black
        _overlay.Visibility = ViewStates.Gone;
        _overlay.Click += (s, e) => Hide(); // Click outside to close
        _parentRoot.AddView(_overlay);

        // 2. The Menu Card
        _menuCard = new MaterialCardView(_ctx)
        {
            Radius = AppUtil.DpToPx(16), // Rounded corners
            CardElevation = AppUtil.DpToPx(8),
            Visibility = ViewStates.Invisible // Start hidden
        };

        // Positioning: Anchor to Bottom-Right (or wherever your FAB is)
        // You might need to adjust margins based on your specific layout
        var lp = new FrameLayout.LayoutParams(AppUtil.DpToPx(250), ViewGroup.LayoutParams.WrapContent);
        lp.Gravity = GravityFlags.Bottom | GravityFlags.Right;
        lp.SetMargins(0, 0, AppUtil.DpToPx(16), AppUtil.DpToPx(16)); 
        
        int margin = AppUtil.DpToPx(16);
        lp.SetMargins(0, 0, margin, margin);
        
        _menuCard.LayoutParameters = lp;

        // 3. The Content (Linear Layout)
        var listLayout = new LinearLayout(_ctx) { Orientation = Orientation.Vertical };
        listLayout.SetPadding(0, AppUtil.DpToPx(8), 0, AppUtil.DpToPx(8));

        // 4. Generate Buttons from the List
        foreach (var item in _menuItems)
        {
            var btn = CreateMenuButton(item);
            listLayout.AddView(btn);
        }

        _menuCard.AddView(listLayout);
        _overlay.AddView(_menuCard);
    }

    private View CreateMenuButton(MenuItem item)
    {
        // Material Button optimized for Menu use
        var btn = new MaterialButton(_ctx, null, Resource.Attribute.materialButtonStyle);

        btn.Text = item.Text;
        btn.SetIconResource(item.IconRes);

        // Styling
        btn.IconGravity = MaterialButton.IconGravityTextStart;
        btn.IconPadding = AppUtil.DpToPx(12);
        btn.Gravity = GravityFlags.Start | GravityFlags.CenterVertical;
        btn.SetAllCaps(false); // MD3 uses Sentence case
        btn.TextSize = 16;
        btn.LetterSpacing = 0;

        // Colors
        btn.SetTextColor(Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.White));
        btn.IconTint = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.LightGray);

        // Layout
        btn.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, AppUtil.DpToPx(56));

        // Logic
        btn.Click += (s, e) =>
        {
            // Optional: Wait for ripple to finish before hiding/acting
            item.OnClick?.Invoke();
            Hide();
        };

        return btn;
    }
}