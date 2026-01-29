using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AndroidX.CoordinatorLayout.Widget;

namespace Dimmer.ViewsAndPages.NativeViews.DeviceTransfer;

public partial class DevTransferFragment :Fragment
{
    private CoordinatorLayout root;

    public DevTransferFragment()
    {
        

    }
    public DevTransferFragment(DeviceTransferViaBTViewModel vm)
    {
        deviceTransferViewModel = vm;
        MyViewModel = vm.MyViewModel;
    }

    public DeviceTransferViaBTViewModel deviceTransferViewModel { get; }
    public BaseViewModelAnd MyViewModel  { get; }

    public override View? OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        var ctx = Context;

        root = new CoordinatorLayout(ctx)
        {
            LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.MatchParent)
        };

        var contentLinear = new LinearLayout(ctx)
        {
            Orientation = Orientation.Vertical,
            LayoutParameters = new CoordinatorLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)
        };




        return root;

    }

    public override void OnResume()
    {
        base.OnResume();
    }
}
