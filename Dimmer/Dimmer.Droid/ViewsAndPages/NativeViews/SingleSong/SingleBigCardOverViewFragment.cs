

using AndroidX.CoordinatorLayout.Widget;
using AndroidX.Lifecycle;
using Dimmer.UiUtils;
using Dimmer.Utilities;
using static Android.Provider.DocumentsContract;
using static Android.Provider.MediaStore.Audio;

namespace Dimmer.ViewsAndPages.NativeViews.SingleSong;

public partial class SingleBigCardOverViewFragment : Fragment
{
    private ImageView _backgroundImageView;

    public SingleBigCardOverViewFragment(BaseViewModelAnd vm)
    {

        MyViewModel = vm;
    }

    public BaseViewModelAnd MyViewModel { get; }
    public CoordinatorLayout root { get; private set; }

    public override View? OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        var ctx = Context;

        // 1. Root: CoordinatorLayout (Crucial for FABs)
        root = new CoordinatorLayout(ctx)
        {
            LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)
        };

       
        _backgroundImageView = new ImageView(ctx)
        {
            LayoutParameters = new FrameLayout.LayoutParams(-1, -1),

        };

        _backgroundImageView.SetScaleType(ImageView.ScaleType.CenterCrop);

        root.AddView(_backgroundImageView);
        // 2. Main Content Container (Linear Layout inside Coordinator)
        var contentLinear = new LinearLayout(ctx)
        {
            Orientation = Orientation.Vertical,
            LayoutParameters = new CoordinatorLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)
        };

        var songCard = UiBuilder.CreateCard(ctx);
        songCard.Radius = AppUtil.DpToPx(25);
            songCard.CardElevation = AppUtil.DpToPx(4);
        songCard.LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1f);

        ((LinearLayout.LayoutParams)songCard.LayoutParameters).SetMargins(10, 0, 10, 0);



        return root;
    }

    public override void OnResume()
    {
        MyViewModel.WhenPropertyChange(nameof(MyViewModel.CurrentPlayingSongView), v => MyViewModel.CurrentPlayingSongView)
            .ObserveOn(RxSchedulers.UI)
            .Subscribe(song =>
            {

                var androidDomColor = MyViewModel.SelectedSecondDominantColor;
                if (androidDomColor is not null)
                {
                    var domCol = androidDomColor.ToHex();
                    root.SetBackgroundColor(Color.ParseColor(domCol));
                    
                }


            });
    }
}