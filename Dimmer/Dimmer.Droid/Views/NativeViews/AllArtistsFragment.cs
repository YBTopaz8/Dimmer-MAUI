using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Color = Android.Graphics.Color;
using Android.OS;

using AndroidX.Core.View;
using AndroidX.Fragment.App;
using AndroidX.Lifecycle;
using AndroidX.RecyclerView.Widget;
using AndroidX.Transitions;

using Google.Android.Material.FloatingActionButton;
using Google.Android.Material.Transition;

using static Android.Provider.DocumentsContract;
using static Android.Provider.Telephony.Mms;
using static Microsoft.Maui.LifecycleEvents.AndroidLifecycle;

using Button = Android.Widget.Button;
using Orientation = Android.Widget.Orientation;
using View = Android.Views.View;
using AndroidX.Activity;

namespace Dimmer.Views.NativeViews;

public class AllArtistsFragment : Fragment
{
    private readonly BaseViewModelAnd MyViewModel;

    private FloatingActionButton? startFab;
    private LinearLayout? expandedCard;
    private LinearLayout? contactCard;
    private View? startCard;
    private View? endView;
    private FrameLayout? root;
    public AllArtistsFragment()
    {
        MyViewModel = IPlatformApplication.Current!.Services!.GetService<BaseViewModelAnd>()!;
    }
    public override View? OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        var ctx = Context!;
        root = new FrameLayout(ctx)
        {
            Id = View.GenerateViewId()
        };

        // ---------------- Recycler ----------------
        var recycler = new RecyclerView(ctx);
        recycler.SetLayoutManager(new LinearLayoutManager(ctx));
        recycler.SetAdapter(new SongAdapter(ctx, MyViewModel, MyViewModel.SearchResults));
        recycler.SetPadding(0, 0, 0, 260); // space for FAB
        root.AddView(recycler, new FrameLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.MatchParent));

        // ---------------- FAB ----------------
        startFab = new FloatingActionButton(ctx);
        startFab.SetImageResource(Android.Resource.Drawable.IcInputAdd);
        ViewCompat.SetTransitionName(startFab, "fab");
        var fabParams = new FrameLayout.LayoutParams(180, 180)
        {
            Gravity = GravityFlags.Bottom | GravityFlags.End,
            BottomMargin = 80,
            RightMargin = 80
        };
        root.AddView(startFab, fabParams);

        // ---------------- Expanded Card ----------------
        expandedCard = CreateCard(ctx, "All Artists", Color.Rgb(63, 81, 181));
        ViewCompat.SetTransitionName(expandedCard, "expandedCard");
        expandedCard.Visibility = ViewStates.Gone;
        root.AddView(expandedCard);

        // ---------------- Contact Card ----------------
        contactCard = CreateCard(ctx, "Contact", Color.Rgb(0, 150, 136));
        ViewCompat.SetTransitionName(contactCard, "contactCard");
        contactCard.Visibility = ViewStates.Gone;
        root.AddView(contactCard);

        InitializeAsync();
        return root;
    }
    public override void OnViewCreated(View view, Bundle? savedInstanceState)
    {
        AddTransitionableTarget(view, startFab!);
        AddTransitionableTarget(view, expandedCard!);
        AddTransitionableTarget(view, contactCard!);
    }
    private void AddTransitionableTarget(View view, View target)
    {
        target.Click += (s, e) =>
        {
            if (target == startFab)
                ShowEndView(startFab!, contactCard!);
            else if (target == expandedCard || target == contactCard)
                ShowStartView(target);
            else
                ShowEndView(target, expandedCard!);
        };
    }
    private LinearLayout CreateCard(Context ctx, string title, Color color)
    {
        var card = new LinearLayout(ctx)
        {
            Orientation = Orientation.Vertical,
            Background = new Android.Graphics.Drawables.ColorDrawable(color)
        };
        card.SetGravity(GravityFlags.Center);
        var text = new TextView(ctx)
        {
            Text = title,
            TextSize = 22,
            Gravity = GravityFlags.Center,
            
        };
        text.SetTextColor(Color.White);
        card.AddView(text, new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent));

        var closeBtn = new Button(ctx)
        {
            Text = "Close"
        };
        closeBtn.Click += (s, e) => ShowStartView(card);
        card.AddView(closeBtn);

        return card;
    }

    private void ShowEndView(View startView, View end)
    {
        endView = end;
        var transition = BuildContainerTransform(true);
        transition.StartView = startView;
        transition.EndView = end;
        transition.AddTarget(end);

        TransitionManager.BeginDelayedTransition(root!, transition);
        startView.Visibility = ViewStates.Invisible;
        end.Visibility = ViewStates.Visible;

        var finalEndView = end;
        var callback = new MyBackPressedCallback(true, () =>
        {
            ShowStartView(finalEndView);
        });
        RequireActivity().OnBackPressedDispatcher.AddCallback(this, callback);
    }
    private sealed class MyBackPressedCallback : AndroidX.Activity.OnBackPressedCallback
    {
        private readonly Action _onBack;

        public MyBackPressedCallback(bool enabled, Action onBack) : base(enabled)
        {
            _onBack = onBack;
        }

        public override void HandleOnBackPressed()
        {
            _onBack?.Invoke();
            Remove(); // unregister itself
        }
    }
    private void ShowStartView(View end)
    {
        View startView = end == contactCard ? startFab! : startCard ?? startFab!;
        var transition = BuildContainerTransform(false);
        transition.StartView = end;
        transition.EndView = startView;
        transition.AddTarget(startView);

        TransitionManager.BeginDelayedTransition(root!, transition);
        startView.Visibility = ViewStates.Visible;
        end.Visibility = ViewStates.Invisible;
    }
    private MaterialContainerTransform BuildContainerTransform(bool entering)
    {
        var transform = new MaterialContainerTransform(Context!, entering)
        {
            ScrimColor =Color.Transparent,
            DrawingViewId = root!.Id,
            FadeMode = MaterialContainerTransform.FadeModeIn
        };
        transform.SetDuration(450);
        transform.SetInterpolator(new Android.Views.Animations.AccelerateDecelerateInterpolator());
        return transform;
    }
    bool _initialized;
    private void InitializeAsync()
    {
        if (!_initialized)
        {
            _initialized = true;
            MyViewModel.InitializeAllVMCoreComponentsAsync();

        }
    }
    private LinearLayout CreateArtistCard(string name, int index)
    {
        var card = new LinearLayout(Context)
        {
            Orientation = Orientation.Horizontal
        };

        var image = new ImageView(Context);
        image.SetImageResource(Resource.Drawable.dimmicoo);
        image.TransitionName = $"artistImage_{index}";
        image.SetPadding(10, 10, 10, 10);
        image.Click += (s, e) => OpenArtistDetail(index, image);

        var title = new TextView(Context);
        title.Text = name;
        title.TextSize = 18;
        title.SetPadding(20, 0, 20, 0);

        var button = new Button(Context)
        {
            Text = "Info"
        };
        button.Click += (s, e) => ShowQuickInfo(button);

        card.AddView(image, new LinearLayout.LayoutParams(200, 200));
        card.AddView(title);
        card.AddView(button);

        return card;
    }
    private void OpenArtistDetail(int index, View sharedView)
    {
        var detail = new ArtistDetailFragment(index);

        // ----- ensure both fragments share the same container -----
        var containerId = TransitionActivity.MyStaticID;
        if (containerId == 0)
            containerId = (sharedView.RootView as ViewGroup)?.Id ?? View.GenerateViewId();

        // ----- set up transform -----
        var enter = new MaterialContainerTransform()
        {
            DrawingViewId = containerId,
            ScrimColor = Android.Graphics.Color.Transparent,
            FadeMode = MaterialContainerTransform.FadeModeIn,
            
        };

        enter.SetDuration(450);
        enter.SetInterpolator(new Android.Views.Animations.AccelerateDecelerateInterpolator());

        var returnTrans = new MaterialContainerTransform()
        {
            DrawingViewId = containerId,
            ScrimColor = Android.Graphics.Color.Transparent,
            FadeMode = MaterialContainerTransform.FadeModeOut,
          
        };
        enter.SetDuration(350);
        returnTrans.SetInterpolator(new Android.Views.Animations.AccelerateDecelerateInterpolator());

        // ----- assign to fragments -----
        detail.SharedElementEnterTransition = enter;
        detail.SharedElementReturnTransition = returnTrans;
        detail.EnterTransition = new AndroidX.Transitions.Fade(AndroidX.Transitions.Fade.ModeIn);
        detail.ReturnTransition = new AndroidX.Transitions.Fade(AndroidX.Transitions.Fade.ModeOut);

        SharedElementEnterTransition = enter;
        SharedElementReturnTransition = returnTrans;

        // small delay lets layout settle before animation starts
        sharedView.Post(() =>
        {
            ParentFragmentManager.BeginTransaction()
                .SetReorderingAllowed(true)
                .AddSharedElement(sharedView, $"artistImage_{index}")
                .Replace(containerId, detail)
                .AddToBackStack(null)
                .Commit();
        });
    }

 
private FrameLayout? _infoCard;

    private void ShowQuickInfo(View anchor)
    {
        var parent = (ViewGroup)anchor.Parent!;

        // If card already visible → close it instead
        if (_infoCard != null && parent.IndexOfChild(_infoCard) != -1)
        {
            var close = new MaterialContainerTransform()
            {
                StartView = _infoCard,
                EndView = anchor,
                ScrimColor = Android.Graphics.Color.Transparent
            };
            close.SetDuration(550);
            close.SetInterpolator(new Android.Views.Animations.AccelerateDecelerateInterpolator());

            TransitionManager.BeginDelayedTransition(parent, close);
            parent.RemoveView(_infoCard);
            _infoCard = null;
            return;
        }

        // Otherwise, create and open new card
        _infoCard = new FrameLayout(Context);
        _infoCard.SetBackgroundColor(Android.Graphics.Color.DarkSlateBlue);
        _infoCard.Alpha = 0;
        
        _infoCard.SetPadding(30, 30, 30, 30);

        var text = new TextView(Context);
        text.Text = "Quick Info Here!";
        text.TextSize = 16;
        _infoCard.AddView(text);

        var open = new MaterialContainerTransform()
        {
            StartView = anchor,
            EndView = _infoCard,
            ScrimColor = Android.Graphics.Color.Transparent
        };
        open.SetDuration(500);
        open.SetInterpolator(new Android.Views.Animations.AccelerateDecelerateInterpolator());

        TransitionManager.BeginDelayedTransition(parent!, open);
        parent!.AddView(_infoCard, new FrameLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent, 300));

        _infoCard.Animate()!.Alpha(1f).SetDuration(300).Start();
    }
}