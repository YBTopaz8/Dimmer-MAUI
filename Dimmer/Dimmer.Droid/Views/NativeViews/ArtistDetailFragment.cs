using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Android.Graphics;
using Android.OS;

using AndroidX.DynamicAnimation;
using AndroidX.Fragment.App;

using Java.Util.Streams;

using ImageButton = Android.Widget.ImageButton;
using Orientation = Android.Widget.Orientation;
using View = Android.Views.View;

namespace Dimmer.Views.NativeViews;

public class ArtistDetailFragment : Fragment
{
    private readonly int _index;

    BaseViewModelAnd MyViewModel;

    enum SongTransitionAnimation
    {
        Spring,
        Fade,
        Scale,
        Slide
    }

    public ArtistDetailFragment(int index)
    {
        _index = index;
        MyViewModel = IPlatformApplication.Current!.Services.GetService<BaseViewModelAnd>()!;
    }
    public override View? OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        var ctx = Context!;
        var root  = new LinearLayout(ctx)
        {
            Orientation = Orientation.Vertical
        };
        root .SetPadding(40, 80, 40, 80);
        root .SetBackgroundColor(Android.Graphics.Color.White);

        // ─── Header image ───────────────────────────────
        var headerImg = new ImageView(ctx)
        {
            LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 500),
            TransitionName = $"artistImage_{_index}"
        };

        //image.SetImageResource(Resource.Drawable.dimmicoo); image source is likely a BYTE[]
        // OR a string PATH on DISK
        headerImg.TransitionName = $"artistImage_{_index}";
        headerImg.LayoutParameters = new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent, 500);
        headerImg.SetScaleType(ImageView.ScaleType.CenterCrop);

        headerImg.ViewTreeObserver!.AddOnGlobalLayoutListener(new GlobalLayoutListener(headerImg, ApplyEntranceEffect));


        root .AddView(headerImg);

        // ─── Artist name ────────────────────────────────
        var artistName = new TextView(ctx)
        {
            Text = MyViewModel.SelectedSong!.ArtistName,
            TextSize = 26,
        };
        artistName.SetTextColor(Android.Graphics.Color.Black);
        artistName.SetTypeface(null, Android.Graphics.TypefaceStyle.Bold);
        artistName.SetPadding(0, 40, 0, 20);
        root .AddView(artistName);

        // ─── Chips row ──────────────────────────────────
        var chipRow = new LinearLayout(ctx)
        {
            Orientation = Orientation.Horizontal
        };
        chipRow.SetPadding(0, 0, 0, 40);
        chipRow.SetGravity(GravityFlags.CenterHorizontal);
        chipRow.SetClipToPadding(false);
        var realm = MyViewModel.RealmFactory.GetRealmInstance();
        var artist = realm.Find<ArtistModel>(MyViewModel.SelectedSong.Id);
        var songsCount = artist.Songs.Count();
        var songs = MyViewModel._mapper.Map<ObservableCollection<SongModelView>>(artist.Songs);




        string[] chipTexts = { $"{songsCount} songs total", $"{artist.Albums.Count()} albums", $"X mins total" };
        foreach (var chipText in chipTexts)
        {
            var chip = new TextView(ctx)
            {
                Text = chipText,
                TextSize = 14,
            };
            chip.SetPadding(30, 12, 30, 12);
            chip.SetBackgroundResource(Resource.Drawable.heart); // optional drawable
            chip.SetTextColor(Android.Graphics.Color.White);
            chip.SetBackgroundColor(Android.Graphics.Color.Transparent);
            var lp = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.WrapContent,
                ViewGroup.LayoutParams.WrapContent
            )
            { RightMargin = 20 };
            chipRow.AddView(chip, lp);
        }
        root.AddView(chipRow);
        
        var recycler = new AndroidX.RecyclerView.Widget.RecyclerView(ctx);
        recycler.SetLayoutManager(new AndroidX.RecyclerView.Widget.LinearLayoutManager(ctx));
        recycler.SetAdapter(new SongAdapter(ctx, MyViewModel, songs.ToList()));

        root.AddView(recycler, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent));

        return root ;
    }
    class GlobalLayoutListener : Java.Lang.Object, ViewTreeObserver.IOnGlobalLayoutListener
    {
        private readonly View _view;
        private readonly Action<View, SongTransitionAnimation> _action;
        public GlobalLayoutListener(View v, Action<View, SongTransitionAnimation> action)
        {
            _view = v;
            _action = action;
        }

        public void OnGlobalLayout()
        {
            _view.ViewTreeObserver!.RemoveOnGlobalLayoutListener(this);
            _action(_view, SongTransitionAnimation.Spring);
        }
    }

    private void ApplyEntranceEffect(View target, SongTransitionAnimation anim)
    {
        switch (anim)
        {
            case SongTransitionAnimation.Fade:
                target.Alpha = 0f;
                target.Animate()!
                    .Alpha(1f)
                    .SetDuration(350)
                    .SetInterpolator(new Android.Views.Animations.DecelerateInterpolator())
                    .Start();
                break;

            case SongTransitionAnimation.Scale:
                target.ScaleX = 0.8f;
                target.ScaleY = 0.8f;
                target.PivotX = target.Width / 2f;
                target.PivotY = target.Height / 2f;
                target.Animate()!
                    .ScaleX(1f)
                    .ScaleY(1f)
                    .SetDuration(350)
                    .SetInterpolator(new Android.Views.Animations.OvershootInterpolator())
                    .Start();
                break;

            case SongTransitionAnimation.Slide:
                target.TranslationY = 80f;
                target.Animate()!
                    .TranslationY(0)
                    .SetDuration(350)
                    .SetInterpolator(new Android.Views.Animations.DecelerateInterpolator())
                    .Start();
                break;

            case SongTransitionAnimation.Spring:
            default:
                // Physics-based bounce like your WinUI3 spring
                using (var springAnim = new SpringAnimation(target,
                    DynamicAnimation.TranslationY, 0)
                { })
                {
                    var springForce = new SpringForce(0)
                    .SetDampingRatio(SpringForce.DampingRatioMediumBouncy)!
                    .SetStiffness(SpringForce.StiffnessLow);
                    springAnim.SetSpring(springForce);
                    target.TranslationY = 80f;
                    springAnim.Start();

                }
                ;
                    break;
        }
    }

}