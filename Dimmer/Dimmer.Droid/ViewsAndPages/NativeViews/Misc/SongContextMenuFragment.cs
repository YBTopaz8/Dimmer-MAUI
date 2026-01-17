using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dimmer.UiUtils;
using Org.Apache.Http.Conn;

namespace Dimmer.ViewsAndPages.NativeViews.Misc;

public partial class SongContextMenuFragment : DialogFragment
{
    BaseViewModelAnd MyViewModel;
    public SongContextMenuFragment(BaseViewModelAnd vm)
    {
        MyViewModel = vm;
    }
    public override Dialog? OnCreateDialog(Bundle? savedInstanceState)
    {
        
    
        
        var ctx = Context!;

        var root = new LinearLayout(ctx)
        {
            Orientation = Android.Widget.Orientation.Vertical
        };
        root.SetPadding(32,32,32, 32);
        root.AddView( Title(ctx, MyViewModel.SelectedSong.Title));
        root.AddView( Sub(ctx, MyViewModel.SelectedSong.OtherArtistsName));

        root.AddView( Divider(ctx));

        root.AddView( Text(ctx, $"Album: {MyViewModel.SelectedSong.AlbumName}"));
        root.AddView( Text(ctx, $"Genre: {MyViewModel.SelectedSong.GenreName}"));

        root.AddView(Divider(ctx));


        root.AddView(Button(ctx, "Edit",Resource.Drawable.edit, () =>
        {
            EditSong();
        }));

        root.AddView(Button(ctx, "Delete", Resource.Drawable.deletesonginterfacesymbol,() =>
        {
            DeleteSong();
        }));

        root.AddView(Button(ctx, "Stats", Resource.Drawable.stats, () =>
        {
            ShowStats();
        }));
        root.AddView(Divider(ctx));

        var dialog = new Dialog(ctx)
            ;
        dialog.SetContentView(root);

        dialog.Window!.SetBackgroundDrawableResource(Resource.Drawable.mr_dialog_material_background_dark);
        return dialog;

    }

    public void ShowStats()
    {

    }

    public void DeleteSong()
    {

    }

    public void EditSong()
    {

    }
    private TextView? Title(Context ctx, string title)
    {
        return new TextView(ctx)
        {
            Text = title,
            TextSize = 18,
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
        btn.Click +=(s,e)=> onClick();
        btn.IconSize = AppUtil.DpToPx(12);
        return btn;
    }

    private Button? Destructive(Context ctx, string title, int iconResourceInt, Action onClick)
    {
        var btn = new Button(ctx);
        btn.Text = title;
        btn.SetIconResource(iconResourceInt);
        btn.SetTextColor(Color.Red);
        btn.Typeface = Typeface.DefaultBold;
        btn.Click += (s, e) => onClick();
        btn.IconSize = AppUtil.DpToPx(12);
        return btn;
    }
}
