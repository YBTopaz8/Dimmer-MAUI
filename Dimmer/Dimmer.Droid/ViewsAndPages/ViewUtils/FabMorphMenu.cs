using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AndroidX.Lifecycle;

namespace Dimmer.ViewsAndPages.ViewUtils;


public class FabMorphMenu
{
    private readonly Context _ctx;
    private readonly ViewGroup _parentRoot; // The container (usually CoordinatorLayout or FrameLayout)
    private readonly View _anchorFab;
    private MaterialCardView? _menuCard; 
    private LinearLayout? _menuContent;
    private bool _isOpen = false;
    private FrameLayout? _overlay; // Invisible view to catch clicks outside

    private readonly List<MenuItem> _menuItems = new();
    private record MenuItem(string Text, int IconRes, Action OnClick);
    //public FabMorphMenu(Context ctx, ViewGroup parentRoot, View fab, FragmentManager _parentFragmentManager
    //    ,BaseViewModelAnd vm)
    //{
   public FabMorphMenu(Context ctx, ViewGroup parentRoot, View anchorFab)
    {
        _ctx = ctx;
        _parentRoot = parentRoot;
        //parentFragmentManager= _parentFragmentManager;
        //this.MyViewModel = vm;
        _anchorFab = anchorFab;
    }
    public FabMorphMenu AddItem(string text, int iconRes, Action onClick)
    {
        _menuItems.Add(new MenuItem(text, iconRes, onClick));
        return this;
    }
    public void Show()
    {
        if (_isOpen) return;

        // 1. Create the Menu View (if not created yet)
        if (_menuCard == null)
            BuildMenuUi();

        PositionCardOverFab();
        if (_menuContent != null)
        {
            _menuContent.Alpha = 0f;
            // Fade content IN after the bubble has started growing (Delay 50ms)
            _menuContent.Animate()?.Alpha(1f)?.SetDuration(200)?.SetStartDelay(50)?.Start();
        }

        // 2. Setup the Morph Transition
        var transform = new MaterialContainerTransform
        {
            StartView = _anchorFab,
            EndView = _menuCard,
            ScrimColor = Android.Graphics.Color.Transparent, // We handle scrim manually if needed
            ContainerColor = Android.Graphics.Color.ParseColor("#1E1E1E"), // Dark Grey/Black MD3
            FadeMode = MaterialContainerTransform.FadeModeCross,
            
            PathMotion = new MaterialArcMotion() // The arc makes it feel organic
        };

        // 4. Run Transition
        TransitionManager.BeginDelayedTransition(_parentRoot, transform);

        // 5. Swap Visibility
        _anchorFab.Visibility = ViewStates.Invisible;
        _menuCard!.Visibility = ViewStates.Visible;

        if (_overlay != null)
        {
            _overlay.Visibility = ViewStates.Visible;
            _overlay.Alpha = 0;
            _overlay.Animate()?.Alpha(1f)?.SetDuration(200)?.Start();
        }

        _isOpen = true;
    }


    public void Hide()
    {
        if (!_isOpen || _menuCard == null) return;
        if (_menuContent != null)
        {
            _menuContent.Animate()?.Alpha(0f)?.SetDuration(100)?.Start();
        }

        // 1. Setup Reverse Morph
        var transform = new MaterialContainerTransform
        {
            StartView = _menuCard,
            EndView = _anchorFab,
            ScrimColor = Android.Graphics.Color.Transparent,
            ContainerColor = Android.Graphics.Color.ParseColor("#1E1E1E"),
            FadeMode = MaterialContainerTransform.FadeModeCross,
            
            PathMotion = new MaterialArcMotion()
        };
        transform.SetDuration(300);

        // 2. Run Transition
        TransitionManager.BeginDelayedTransition(_parentRoot, transform);

        // 3. Swap Visibility
        _menuCard.Visibility = ViewStates.Invisible;
        _anchorFab.Visibility = ViewStates.Visible;
        if (_overlay != null) _overlay.Visibility = ViewStates.Gone;

        _isOpen = false;
    }

    private void BuildMenuUi()
    {
        // Overlay Container
        _overlay = new FrameLayout(_ctx)
        {
            LayoutParameters = new ViewGroup.LayoutParams(-1, -1),
            Clickable = true,
            Focusable = true
        };
        _overlay.SetBackgroundColor(Android.Graphics.Color.ParseColor("#AA000000"));
        _overlay.Visibility = ViewStates.Gone;
        _overlay.Click += (s, e) => Hide();
        _parentRoot.AddView(_overlay);

        // Menu Card
        _menuCard = new MaterialCardView(_ctx)
        {
            Radius = AppUtil.DpToPx(16),
            CardElevation = AppUtil.DpToPx(8),
            Visibility = ViewStates.Invisible,
            Clickable = true
        };

        _menuCard.LayoutParameters = new FrameLayout.LayoutParams(AppUtil.DpToPx(240), ViewGroup.LayoutParams.WrapContent);

        // Content Layout (Saved to _menuContent for animation)
        _menuContent = new LinearLayout(_ctx) { Orientation = Orientation.Vertical };
        _menuContent.SetPadding(0, AppUtil.DpToPx(8), 0, AppUtil.DpToPx(8));

        foreach (var item in _menuItems)
        {
            _menuContent.AddView(CreateMenuButton(item));
        }

        _menuCard.AddView(_menuContent);
        _overlay.AddView(_menuCard);
    }

    private void PositionCardOverFab()
    {
        if (_menuCard == null || _overlay == null) return;

        // 1. Get FAB Screen Coordinates (Absolute pixels)
        int[] fabLoc = new int[2];
        _anchorFab.GetLocationInWindow(fabLoc);
        int fabX = fabLoc[0];
        int fabY = fabLoc[1];

        // 2. Calculate Margins
        // We want the card's Bottom-Right to match the FAB's Bottom-Right
        int parentWidth = _parentRoot.Width;
        int parentHeight = _parentRoot.Height;

        int marginRight = parentWidth - (fabX + _anchorFab.Width);
        int marginBottom = parentHeight - (fabY + _anchorFab.Height);

        // 3. Apply Margins to the Card
        var lp = (FrameLayout.LayoutParams)_menuCard.LayoutParameters!;
        lp.Gravity = GravityFlags.Bottom | GravityFlags.Right;
        lp.RightMargin = marginRight;
        lp.BottomMargin = marginBottom;
        _menuCard.LayoutParameters = lp;
    }
    // Helper to make those "Bubble" rows
    private View CreateMenuButton(MenuItem item)
    {
        var btn = new MaterialButton(_ctx, null, Resource.Attribute.materialButtonStyle);
        btn.Text = item.Text;
        btn.SetIconResource(item.IconRes);
        btn.IconGravity = MaterialButton.IconGravityTextStart;
        btn.IconPadding = AppUtil.DpToPx(12);
        btn.Gravity = GravityFlags.Start | GravityFlags.CenterVertical;
        btn.SetAllCaps(false);
        btn.TextSize = 16;
        btn.SetTextColor(Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.White));
        btn.IconTint = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.LightGray);
        btn.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, AppUtil.DpToPx(56));

        btn.Click += (s, e) =>
        {
            btn.PostDelayed(() =>
            {
                item.OnClick?.Invoke();
                Hide();
            }, 100);
        };
        return btn;
    }
}