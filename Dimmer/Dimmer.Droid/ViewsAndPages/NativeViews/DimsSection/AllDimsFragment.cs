using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AndroidX.RecyclerView.Widget;

using Dimmer.WinUI.UiUtils;
namespace Dimmer.ViewsAndPages.NativeViews.DimsSection;


public partial class AllDimsFragment : Fragment
{
    private RecyclerView _eventsRecycler = null!;
    private PlayEventAdapter _adapter = null!;
    public BaseViewModelAnd MyViewModel { get; private set; } = null!;

    public AllDimsFragment(BaseViewModelAnd viewModel)
    {
        MyViewModel = viewModel;
    }

    public override View OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        var ctx = Context!;
        var root = new LinearLayout(ctx)
        {
            Orientation = Orientation.Vertical,
            LayoutParameters = new ViewGroup.LayoutParams(-1, -1)
        };

        // Header (Empty StackPanel from your XAML)
        var header = UiBuilder.CreateHeader(ctx, "Play History");
        header.SetPadding(AppUtil.DpToPx(16), AppUtil.DpToPx(16), AppUtil.DpToPx(16), AppUtil.DpToPx(8));
        root.AddView(header);


        // TableView Equivalent
        _eventsRecycler = new RecyclerView(ctx);
        _eventsRecycler.SetLayoutManager(new LinearLayoutManager(ctx));
        _eventsRecycler.LayoutParameters = new LinearLayout.LayoutParams(-1, -1);

        _adapter = new PlayEventAdapter(ctx, MyViewModel, this);
        _eventsRecycler.SetAdapter(_adapter);

        root.AddView(_eventsRecycler);
        return root;
    }
}