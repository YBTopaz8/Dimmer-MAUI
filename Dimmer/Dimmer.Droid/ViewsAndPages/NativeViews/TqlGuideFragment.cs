using Dimmer.DimmerSearch.TQLDoc;
using Dimmer.TQL;

using Google.Android.Material.Chip;

namespace Dimmer.ViewsAndPages.NativeViews;


public class TqlGuideFragment : Fragment
{
    private readonly BaseViewModelAnd _viewModel;
    private RecyclerView _recyclerView;
    private ChipGroup _filterGroup;
    private List<TqlHelpItem> _displayedItems;

    public TqlGuideFragment(BaseViewModelAnd viewModel)
    {
        _viewModel = viewModel;
        _displayedItems = TqlDocumentation.AllItems;
    }

    public override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        // Setup transition for entering this guide (Slide Up looks best)
        EnterTransition = new MaterialSharedAxis(MaterialSharedAxis.Y, true);
        ReturnTransition = new MaterialSharedAxis(MaterialSharedAxis.Y, false);
    }

    public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
    {
        var ctx = Context!;

        var root = new LinearLayout(ctx)
        {
            Orientation = Orientation.Vertical,
            Background = new Android.Graphics.Drawables.ColorDrawable(Android.Graphics.Color.White), // Set your theme background
            Clickable = true // Prevents clicks passing through to fragment below
        };
        root.LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
        root.SetPadding(AppUtil.DpToPx(20), AppUtil.DpToPx(40), AppUtil.DpToPx(20), 0);

        // 1. Header
        var header = new TextView(ctx)
        {
            Text = "TQL Guide",
            TextSize = 28,
            Typeface = Android.Graphics.Typeface.DefaultBold
        };
        root.AddView(header);

        var subHeader = new TextView(ctx)
        {
            Text = "Tap an example to execute it.",
            TextSize = 14
        };
        subHeader.SetPadding(0, 0, 0, AppUtil.DpToPx(16));
        root.AddView(subHeader);

        // 2. Filter Chips (Horizontal Scroll)
        var scroll = new HorizontalScrollView(ctx)
        {
            LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        };
        scroll.ScrollBarSize = 0;

        _filterGroup = new ChipGroup(ctx) { SingleSelection = true };
        _filterGroup.SetPadding(0, 0, 0, AppUtil.DpToPx(16));

        // Add "All" Chip
        AddFilterChip(ctx, "All", true);

        // Add Category Chips
        foreach (var cat in Enum.GetValues<TqlCategory>())
        {
            AddFilterChip(ctx, cat.ToString(), false);
        }

        scroll.AddView(_filterGroup);
        root.AddView(scroll);

        // 3. RecyclerView
        _recyclerView = new RecyclerView(ctx)
        {
            LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)
        };
        _recyclerView.SetLayoutManager(new LinearLayoutManager(ctx));
        UpdateList(); // Initial load

        root.AddView(_recyclerView);

        return root;
    }

    private void AddFilterChip(Context ctx, string text, bool isChecked)
    {
        var chip = new Chip(ctx)
        {
            Text = text,
            Checkable = true,
            Checked = isChecked,
            Clickable = true
        };
        chip.CheckedChange += (s, e) =>
        {
            if (e.IsChecked) FilterContent(text);
        };
        _filterGroup.AddView(chip);
    }

    private void FilterContent(string category)
    {
        if (category == "All")
        {
            _displayedItems = TqlDocumentation.AllItems;
        }
        else
        {
            if (Enum.TryParse<TqlCategory>(category, out var catEnum))
            {
                _displayedItems = TqlDocumentation.AllItems.Where(x => x.Category == catEnum).ToList();
            }
        }
        UpdateList();
    }

    private void UpdateList()
    {
        // When an item is clicked:
        var adapter = new TqlGuideAdapter(_displayedItems, (query) =>
        {
            // 1. Push query to ViewModel
            //_viewModel.SearchQuery = query; // Assuming you bind this to your Subject

            _viewModel.SearchToTQL(query);

            // 2. If your VM uses a Subject directly, do this:
            // _viewModel.UpdateSearchQuery(query);

            // 3. Close the guide
            ParentFragmentManager.PopBackStack();
        });
        _recyclerView.SetAdapter(adapter);
    }
}