using Google.Android.Material.Snackbar;

using static Dimmer.Utils.AppUtil;

using TextAlignment = Android.Views.TextAlignment;

namespace Dimmer.UiUtils;

public static class UiBuilder
{
    public static LinearLayout.LayoutParams Params(int w, int h, int margin = 0)
    {
        var p = new LinearLayout.LayoutParams(w, h);
        p.SetMargins(margin, margin, margin, margin);
        return p;
    }

    public static MaterialCardView CreateSectionCard(Context context, string title, View contentView)
    {
        var card = new MaterialCardView(context) { Radius = 24, Elevation = 4 };
        card.LayoutParameters = Params(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent, 16);
        card.SetContentPadding(32, 32, 32, 32);

        var container = new LinearLayout(context) { Orientation = Orientation.Vertical };

        var header = new MaterialTextView(context) { Text = title, TextSize = 20 };
        header.SetTypeface(null, Android.Graphics.TypefaceStyle.Bold);
        header.SetTextColor(Android.Graphics.Color.DarkGray);
        header.LayoutParameters = Params(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent, 0);
        ((LinearLayout.LayoutParams)header.LayoutParameters).BottomMargin = 24;

        container.AddView(header);
        container.AddView(contentView);
        card.AddView(container);
        return card;
    }

    public static TextInputLayout CreateInput(Context context, string hint, string value, bool isMultiLine = false)
    {
        var layout = new TextInputLayout(context, null, Resource.Style.Widget_Material3_TextInputLayout_OutlinedBox);
        layout.Hint = hint;
        layout.LayoutParameters = Params(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent, 8);

        var paramsEdit = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);

        var editText = new TextInputEditText(layout.Context!);
        editText.Text = value;
        editText.LayoutParameters = paramsEdit;

        if (isMultiLine)
        {
            editText.InputType = Android.Text.InputTypes.TextFlagMultiLine;
            editText.SetMinLines(2);
            editText.Gravity = GravityFlags.Top;
        }

        layout.AddView(editText);
        return layout;
    }
    public static bool IsDark(Configuration? CallerFragConfig)
    {
        if(CallerFragConfig == null) { return false; }
        return (CallerFragConfig.UiMode & Android.Content.Res.UiMode.NightMask) == Android.Content.Res.UiMode.NightYes;
    }

    public static MaterialButton CreateMaterialButton(Context ctx, Android.Content.Res.Configuration? callerConfig, EventHandler? clickAction=null, bool isPrimary = false, int sizeDp = 50, int? iconRes=null)
    {
        
        var btn = new MaterialButton(ctx);
        if (iconRes is not null)
        {

            btn.Icon = AndroidX.Core.Content.ContextCompat.GetDrawable(ctx, (int)iconRes);
            btn.IconGravity = MaterialButton.IconGravityTextStart;
        }
        btn.IconPadding = 0;
        btn.InsetTop = 0;
        btn.InsetBottom = 0;
        
        var sizePx = AppUtil.DpToPx(sizeDp);
        btn.LayoutParameters = new LinearLayout.LayoutParams(sizePx, sizePx) { LeftMargin = 20, RightMargin = 20 };
        btn.CornerRadius = sizePx / 2;

        if (isPrimary)
        {
            btn.SetBackgroundColor(Android.Graphics.Color.DarkSlateBlue);
            btn.IconTint = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.White);
        }
        else
        {
            btn.SetBackgroundColor(Android.Graphics.Color.Transparent);
            btn.IconTint = Android.Content.Res.ColorStateList.ValueOf(IsDark(callerConfig) ? Android.Graphics.Color.White : Android.Graphics.Color.Black);
            btn.StrokeWidth = AppUtil.DpToPx(1);
            btn.StrokeColor = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Gray);
        }
        if(clickAction is not null)
            btn.Click += clickAction;
        return btn;
    }


    public static void ShowSnackBar(
            View anchorView,
            string message,
            Color? bgColor = null,
            Color? textColor = null,
            float textSizeSp = 14f,
            Typeface? typeface = null,
            int duration = Snackbar.LengthShort,
            int? iconResId = null,
            string? actionText = null,
            Action<View>? actionCallback = null
        )
    {
        var context = anchorView.Context;
        var snackbar = Snackbar.Make(anchorView, message, duration);

        // Snackbar.View is actually Snackbar.SnackbarLayout
        if (snackbar.View is ViewGroup snackbarLayout)
        {
            // Background color
            snackbarLayout.SetBackgroundColor(bgColor ?? Color.ParseColor("#323232"));

            // Find the default TextView
            var textView = snackbarLayout.GetChildAt(0) as TextView;
            if (textView != null)
            {
                textView.SetTextColor(textColor ?? Color.White);
                textView.TextSize = textSizeSp;
                if (typeface != null)
                    textView.Typeface = typeface;

                // Optional icon
                if (iconResId.HasValue)
                {
                    var drawable = context.GetDrawable(iconResId.Value);
                    textView.SetCompoundDrawablesWithIntrinsicBounds(drawable, null, null, null);
                    textView.CompoundDrawablePadding = (int)(8 * context.Resources.DisplayMetrics.Density);
                }
            }
        }

        // Optional action button
        if (!string.IsNullOrEmpty(actionText) && actionCallback != null)
        {
            snackbar.SetAction(actionText, v => actionCallback(v));
        }

        snackbar.Show();
    }

    public static MaterialTextView CreateHeader(Context ctx, string text)
    {
        var tv = new MaterialTextView(ctx) { Text = text, TextSize = 28, Typeface = Typeface.DefaultBold };
        tv.SetTextColor(IsDark(ctx) ? Color.White : Color.Black);
        return tv;
    }

    public static MaterialTextView CreateSectionTitle(Context ctx, string text)
    {
        var tv = new MaterialTextView(ctx) { Text = text, TextSize = 14, Typeface = Typeface.DefaultBold };
        tv.SetTextColor(IsDark(ctx) ? Color.LightGray : Color.DarkGray);
        tv.SetPadding(10, 40, 0, 10);
        return tv;
    }

    public static MaterialCardView CreateCard(Context ctx)
    {
        var card = new MaterialCardView(ctx)
        {
            Radius = DpToPx(16),
            CardElevation = DpToPx(2),
            UseCompatPadding = true
            
        };
        card.SetBackgroundColor(IsDark(ctx) ? Color.ParseColor("#1E1E1E") : Color.White);
        return card;
    }

    public static TextView CreateStatItem(Context ctx, string label, string value)
    {
        // Simple Vertical Stack for stats
        // Implementation detail...
        return new TextView(ctx) { Text = value, TextSize = 18, Typeface = Typeface.DefaultBold };
    }

    // Standard List Item ViewHolder for Devices/Friends
    public static RecyclerView.ViewHolder CreateListItemVH(Context ctx)
    {
        var root = new LinearLayout(ctx) { Orientation = Orientation.Horizontal, WeightSum = 4 };
        root.SetPadding(20, 20, 20, 20);

        var textStack = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        textStack.LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 3);

        var title = new TextView(ctx) { TextSize = 16, Typeface = Typeface.DefaultBold };
        var sub = new TextView(ctx) { TextSize = 12 };
        textStack.AddView(title);
        textStack.AddView(sub);

        var btn = new MaterialButton(ctx);
        btn.LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1);

        root.AddView(textStack);
        root.AddView(btn);

        return new SimpleVH(root, title, sub, btn);
    }

    public static bool IsDark(Context ctx) => (ctx.Resources.Configuration.UiMode & Android.Content.Res.UiMode.NightMask) == Android.Content.Res.UiMode.NightYes;

    public static Color ThemedBGColor( Context ctx)
    {
        return UiBuilder.IsDark(ctx) ? Color.ParseColor("#0D0E20") : Color.ParseColor("#E7EEF3");
        
    }
}





