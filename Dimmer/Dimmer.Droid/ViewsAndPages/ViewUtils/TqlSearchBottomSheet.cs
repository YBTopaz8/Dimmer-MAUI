using Android.Views.InputMethods;

using Dimmer.DimmerSearch.TQL;

using Google.Android.Material.Chip;

namespace Dimmer.ViewsAndPages.ViewUtils;


public class TqlSearchBottomSheet : BottomSheetDialogFragment
{
    private readonly BaseViewModelAnd _viewModel;
    private TextInputEditText _searchInput = null!;
    private ObservableCollection<string> _suggestions = new();

    public TqlSearchBottomSheet(BaseViewModelAnd viewModel)
    {
        _viewModel = viewModel;
    }

    public override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        // Style to ensure rounded corners and proper keyboard handling
        SetStyle(StyleNormal, Resource.Style.ThemeOverlay_Material3_BottomSheetDialog);
    }

    public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
    {
        var ctx = Context;
        var root = new LinearLayout(ctx)
        {
            Orientation = Orientation.Vertical,
            LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)
        };
        root.SetPadding(0, 20, 0, 0);

        // 1. Drag Handle (Visual cue)
        var handle = new View(ctx)
        {
            Background = new Android.Graphics.Drawables.ColorDrawable(Color.ParseColor("#E0E0E0")),
            LayoutParameters = new LinearLayout.LayoutParams(100, 12) { Gravity = GravityFlags.CenterHorizontal }
        };
        ((LinearLayout.LayoutParams)handle.LayoutParameters).SetMargins(0, 0, 0, 40);
        // Make it rounded
        var shape = new Android.Graphics.Drawables.GradientDrawable();
        shape.SetShape(Android.Graphics.Drawables.ShapeType.Rectangle);
        shape.SetCornerRadius(10);
        shape.SetColor(Color.LightGray);
        handle.Background = shape;
        root.AddView(handle);

        // 2. Search Bar Layout
        var inputLayout = new TextInputLayout(ctx)
        {
            Hint = "Type your query (e.g., artist:Tool)...",
            LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        };
        ((LinearLayout.LayoutParams)inputLayout.LayoutParameters).SetMargins(30, 0, 30, 20);

        _searchInput = new TextInputEditText(ctx) { ImeOptions = ImeAction.Search, InputType = Android.Text.InputTypes.ClassText };
        _searchInput.SetSingleLine(true);
        inputLayout.AddView(_searchInput);
        root.AddView(inputLayout);

        // 3. Chip Group (Quick Filters)
        var hScroll = new HorizontalScrollView(ctx)
        {
            LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent),
            ScrollBarSize = 0
        };
        var chipGroup = new ChipGroup(ctx) { SingleLine = true };
        chipGroup.SetPadding(30, 0, 30, 20);

        AddChip(chipGroup, "Shuffle", "shuffle ");
        AddChip(chipGroup, "Artist", "artist:");
        AddChip(chipGroup, "Year", "year:>");
        AddChip(chipGroup, "Favs", "fav:true ");
        AddChip(chipGroup, "Recently Added", "added:today ");

        hScroll.AddView(chipGroup);
        root.AddView(hScroll);

        // 4. Suggestions RecyclerView
        var recycler = new RecyclerView(ctx)
        {
            LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)
        };
        recycler.SetLayoutManager(new LinearLayoutManager(ctx));
        recycler.SetAdapter(new SimpleStringAdapter(_suggestions, OnSuggestionClicked));
        root.AddView(recycler);

        // --- Event Wiring ---

        // Text Changed -> Autocomplete
        _searchInput.TextChanged += (s, e) =>
        {
            var text = e.Text?.ToString() ?? "";
            var cursor = _searchInput.SelectionStart;

            // Call your existing logic
            // Note: You might need to expose your live data collections from ViewModel or pass them in
            // For now, assuming you have access to the data needed for AutocompleteEngine
            _viewModel.SearchSongForSearchResultHolder(text); // Optional: live search as you type
            UpdateSuggestions(text, cursor);
        };

        // Editor Action (Enter/Search key)
        _searchInput.EditorAction += (s, e) =>
        {
            if (e.ActionId == ImeAction.Search || e.ActionId == ImeAction.Done)
            {
                ExecuteSearch(_searchInput.Text);
                Dismiss();
            }
        };

        return root;
    }

    // Logic to force the keyboard open when the sheet appears
    public override void OnStart()
    {
        base.OnStart();

        var dialog = Dialog as BottomSheetDialog;
        if (dialog != null)
        {
            // 1. Force Expand
            var bottomSheet = dialog.FindViewById<View>(Resource.Id.design_bottom_sheet);
            if (bottomSheet != null)
            {
                var behavior = BottomSheetBehavior.From(bottomSheet);
                behavior.State = BottomSheetBehavior.StateExpanded;
                behavior.SkipCollapsed = true;
            }

            // 2. Force Keyboard (Post it to ensure view is drawn)
            _searchInput.Post(() =>
            {
                _searchInput.RequestFocus();
                var imm = (InputMethodManager?)Context?.GetSystemService(Context.InputMethodService);
                imm?.ShowSoftInput(_searchInput, ShowFlags.Implicit);
            });
        }
    }

    private void AddChip(ChipGroup group, string label, string appendText)
    {
        var chip = new Chip(Context);
        chip.Text = label;
        chip.Click += (s, e) =>
        {
            // Insert text at cursor or append
            var start = Math.Max(_searchInput.SelectionStart, 0);
            var end = Math.Max(_searchInput.SelectionEnd, 0);
            _searchInput.Text = _searchInput.Text?.Remove(Math.Min(start, end), Math.Abs(start - end)).Insert(Math.Min(start, end), appendText);
            _searchInput.SetSelection(Math.Min(start, end) + appendText.Length);
        };
        group.AddView(chip);
    }

    private void UpdateSuggestions(string text, int cursor)
    {
        // NOTE: In a real scenario, pass the live collections (Artists/Albums) from ViewModel
        // Here we mock empty collections just to show the wiring to your Engine
        var dummy = new ObservableCollection<string>();

        var results = AutocompleteEngine.GetSuggestions(
            dummy, dummy, dummy, // You should inject _viewModel.AllArtists, etc. here
            text,
            cursor);

        _suggestions.Clear();
        foreach (var item in results) _suggestions.Add(item);
    }

    private void OnSuggestionClicked(string suggestion)
    {
        // Replace current word with suggestion
        // This is a simplified replacement logic
        var text = _searchInput.Text ?? "";
        var cursor = _searchInput.SelectionStart;
        int lastSpace = text.LastIndexOf(' ', Math.Max(0, cursor - 1));

        string newText = text.Substring(0, lastSpace + 1) + suggestion + " ";
        _searchInput.Text = newText;
        _searchInput.SetSelection(_searchInput.Text.Length);
    }

    private void ExecuteSearch(string? query)
    {
        if (string.IsNullOrWhiteSpace(query)) return;
        _viewModel.SearchSongForSearchResultHolder(query);
    }
}