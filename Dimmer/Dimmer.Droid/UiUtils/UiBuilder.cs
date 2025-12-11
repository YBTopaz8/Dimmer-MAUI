namespace Dimmer.WinUI.UiUtils;

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

        var editText = new TextInputEditText(layout.Context);
        editText.Text = value;
        editText.LayoutParameters = paramsEdit;

        if (isMultiLine)
        {
            editText.InputType = Android.Text.InputTypes.TextFlagMultiLine;
            editText.SetMinLines(3);
            editText.Gravity = GravityFlags.Top;
        }

        layout.AddView(editText);
        return layout;
    }

    public static MaterialButton CreateButton(Context context, string text, EventHandler clickAction, bool isOutlined = false)
    {
        int style = isOutlined
            ? Resource.Style.Widget_Material3_Button_OutlinedButton
            : Resource.Style.Widget_Material3_Button;

        var btn = new MaterialButton(context, null, style) { Text = text };
        btn.Click += clickAction;
        return btn;
    }
}