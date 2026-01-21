using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using AndroidX.Fragment.App;
using Dimmer.UiUtils;
using Org.Apache.Http.Conn;

namespace Dimmer.ViewsAndPages.NativeViews.Misc;

public partial class SongContextMenuFragment :BottomSheetDialogFragment
{
    readonly BaseViewModelAnd MyViewModel;
    SongModelView SelectedSong => MyViewModel.SelectedSong!;


    public SongContextMenuFragment(BaseViewModelAnd vm)
    {
        MyViewModel = vm;

    }

    public override View? OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
     

        var ctx = Context;
        if (ctx == null)
            return null;

        var root = new LinearLayout(ctx)
        {
            Orientation = Android.Widget.Orientation.Vertical
        };
        root.SetPadding(32,32,32, 32);
        root.AddView( Title(ctx, SelectedSong!.Title));
        root.AddView( Sub(ctx, SelectedSong.OtherArtistsName));

        root.AddView( Divider(ctx));

        root.AddView( Text(ctx, $"Album: {SelectedSong.AlbumName}"));
        root.AddView( Text(ctx, $"Genre: {SelectedSong.Genre.Name}"));

        root.AddView(Divider(ctx));


        root.AddView(Button(ctx, "Edit",Resource.Drawable.edit, () =>
        {
            EditSong();

            DismissNow();
        }));

        root.AddView(Button(ctx, "Delete", Resource.Drawable.deletesonginterfacesymbol,async () =>
        {
            await DeleteSong();
            DismissNow();
        }));

        root.AddView(Button(ctx, "Stats", Resource.Drawable.stats, () =>
        {
            ShowStats();
        }));

        return root;

    }

    public void ShowStats()
    {

    }

    public async Task DeleteSong()
    {
        await MyViewModel.DeleteSongs(new List<SongModelView>() { MyViewModel.SelectedSong! });
    }

    public void EditSong()
    {

    }
    private TextView? Title(Context ctx, string title)
    {
        return new TextView(ctx)
        {
            Text = title,
            TextSize = 21,
            Typeface = Typeface.DefaultBold
        };
    }

    private View? Sub(Context ctx, string title)
    {
        return new TextView(ctx)
        {
            Text = title,
            TextSize = 16,
            Typeface = Typeface.DefaultBold
        };
    }

    private View? Text(Context ctx, string title)
    {
        return new TextView(ctx)
        {
            Text = title,
            TextSize = 14
            
        };
    }

    private View? Divider(Context ctx)
    {

        var v = new View(ctx);
            v.SetBackgroundColor(Color.DarkSlateBlue);
        v.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 2);
        return v;
    }

    private Button? Button(Context ctx, string title, int iconResourceInt, Action onClick)
    {

        var btn = new Button(ctx);
        btn.Text = title;
        btn.SetIconResource(iconResourceInt);
        btn.Click += (s, e) =>
        {
            onClick();
            DismissNow();
        };
        btn.IconSize = AppUtil.DpToPx(18);
        return btn;
    }

    private Button? Destructive(Context ctx, string title, int iconResourceInt, Action onClick)
    {
        var btn = new Button(ctx);
        btn.Text = title;
        btn.SetIconResource(iconResourceInt);
        btn.SetTextColor(Color.Red);
        btn.Typeface = Typeface.DefaultBold; 
        btn.Click += (s, e) =>
        {
            onClick();
            DismissNow();
        };

        btn.IconSize = AppUtil.DpToPx(18);
        return btn;
    }

    public override void OnDismiss(IDialogInterface dialog)
    {
        base.OnDismiss(dialog);
    }

    
}
