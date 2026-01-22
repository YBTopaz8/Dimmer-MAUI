using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AndroidX.RecyclerView.Widget;
using Dimmer.UiUtils;
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
        _eventsRecycler.AddOnScrollListener(new HistoryScrollListener(MyViewModel));
        root.AddView(_eventsRecycler);
        return root;
    }
}

internal class HistoryScrollListener : RecyclerView.OnScrollListener
{
    private readonly BaseViewModelAnd _vm;
    public HistoryScrollListener(BaseViewModelAnd vm) => _vm = vm;

    public override void OnScrolled(RecyclerView recyclerView, int dx, int dy)
    {
        base.OnScrolled(recyclerView, dx, dy);

        var layoutManager = (LinearLayoutManager)recyclerView.GetLayoutManager();
        if (layoutManager == null) return;

        // Get the first visible item index
        int firstVisible = layoutManager.FindFirstVisibleItemPosition();
        int visibleCount = layoutManager.ChildCount;

        // Update the virtual window. 
        // We add a "buffer" (e.g., 20) so the user doesn't see blank spaces while scrolling
        if (firstVisible >= 0)
        {
            _vm.UpdateHistoryVirtualRange(firstVisible, visibleCount + 20);
        }
    }
}