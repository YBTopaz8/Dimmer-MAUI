using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.ViewsAndPages.NativeViews.AlbumSection;

public partial class AlbumFragment : Fragment, IOnBackInvokedCallback
{
    public AlbumFragment()
    {
        
    }
    public AlbumFragment(BaseViewModelAnd vm)
    {
        MyViewModel = vm;
        SelectedAlbum = vm.SelectedAlbum;
    }

    public BaseViewModelAnd MyViewModel { get; }
    public AlbumModelView? SelectedAlbum { get; }

    public override View? OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        var ctx = Context;
        var root = new FrameLayout(ctx);
        root.SetBackgroundColor(Color.DarkSlateBlue);


        return root;


    }

    public override void OnResume()
    {
        base.OnResume();
        if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
        {
            Activity?.OnBackInvokedDispatcher.RegisterOnBackInvokedCallback(
                (int)IOnBackInvokedDispatcher.PriorityDefault, this);
        }
    }
    public void OnBackInvoked()
    {
        TransitionActivity myAct = (Activity as TransitionActivity)!;
        myAct?.HandleBackPressInternal();
    }
}
