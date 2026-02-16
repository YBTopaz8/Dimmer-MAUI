using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.ViewsAndPages.NativeViews.DimmerLive.LastFMViews;

using System.Reactive.Disposables;
using Dimmer.UiUtils;
using Dimmer.ViewModel;

using ProgressBar = Android.Widget.ProgressBar;

public class LastFMLoginFragment : Fragment
{
    private readonly BaseViewModelAnd _vm;
    public LastFMLoginFragment(BaseViewModelAnd vm) { _vm = vm; }

    public override View OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        var ctx = Context!;
        var root = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        root.SetGravity(GravityFlags.Center);
        root.SetPadding(80, 0, 80, 0);
        root.SetBackgroundColor(Color.ParseColor("#121212"));

        var logo = new ImageView(ctx);
        logo.SetImageResource(Resource.Drawable.lastfmsmoll); // Use big logo if available
        logo.SetColorFilter(Color.ParseColor("#D51007"));
        logo.LayoutParameters = new LinearLayout.LayoutParams(AppUtil.DpToPx(80), AppUtil.DpToPx(80)) { BottomMargin = 40 };

        var title = new TextView(ctx) { Text = "Last.fm Scrobbler", TextSize = 28, Typeface = Typeface.DefaultBold };
        title.SetTextColor(Color.White);

        var desc = new TextView(ctx) { Text = "Connect your account to track your music journey across the globe.", Gravity = GravityFlags.Center, Alpha = 0.6f };
        desc.SetTextColor(Color.White);
        desc.SetPadding(0, 20, 0, 60);

        var inputLayout = new TextInputLayout(ctx) { BoxBackgroundMode = TextInputLayout.BoxBackgroundOutline };
        inputLayout.SetBoxCornerRadii(30, 30, 30, 30);
        var input = new TextInputEditText(ctx) { Hint = "Last.fm Username" };
        input.SetTextColor(Color.White);
        inputLayout.AddView(input);

        var btn = new MaterialButton(ctx) { Text = "AUTHORIZE APP" };
        btn.SetBackgroundColor(Color.ParseColor("#D51007"));
        btn.CornerRadius=30;
        btn.LayoutParameters = new LinearLayout.LayoutParams(-1, AppUtil.DpToPx(55)) { TopMargin = 40 };

        btn.Click += async (s, e) => {
            BaseViewModel.LastFMName = input.Text!;
            await _vm.LoginToLastfm();
        };

        root.AddView(logo);
        root.AddView(title);
        root.AddView(desc);
        root.AddView(inputLayout);
        root.AddView(btn);
        return root;
    }
}