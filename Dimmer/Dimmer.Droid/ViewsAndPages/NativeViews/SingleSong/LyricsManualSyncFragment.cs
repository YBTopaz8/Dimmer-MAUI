using Android.Content;
using Android.Views;
using Android.Widget;

using AndroidX.Fragment.App;
using AndroidX.RecyclerView.Widget;

using Dimmer.Data.ModelView;
using Dimmer.WinUI.UiUtils;

using Google.Android.Material.Button;
using Google.Android.Material.Card;

using System.Collections.Specialized;

namespace Dimmer.ViewsAndPages.NativeViews.SingleSong;

public class LyricsManualSyncFragment : Fragment
{
    private BaseViewModelAnd _viewModel;
    private SongModelView _selectedSong;
    private RecyclerView _lyricsRecyclerView;
    private LyricsManualSyncAdapter _adapter;
    private MaterialButton _syncButton;
    private MaterialButton _pasteButton;
    private MaterialButton _playPauseButton;
    private LinearLayout _batchActionsToolbar;
    private MaterialButton _repeatSelectedButton;
    private MaterialButton _deleteSelectedButton;
    private MaterialButton _clearSelectionButton;
    private ImageView _coverImage;
    private TextView _songTitle;
    private TextView _artistName;

    public LyricsManualSyncFragment(BaseViewModelAnd viewModel, SongModelView selectedSong)
    {
        _viewModel = viewModel;
        _selectedSong = selectedSong;
    }

    public override View OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        var context = Context;
        var scrollView = new ScrollView(context)
        {
            LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent),
            FillViewport = true
        };

        var root = new LinearLayout(context) { Orientation = Orientation.Vertical };
        root.LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
        root.SetPadding(16, 16, 16, 16);

        // --- HEADER ---
        var header = CreateHeader(context);
        root.AddView(header);

        // --- SONG INFO CARD ---
        var songInfoCard = CreateSongInfoCard(context);
        root.AddView(songInfoCard);

        // --- PASTE BUTTON ---
        var pasteCard = CreatePasteSection(context);
        root.AddView(pasteCard);

        // --- BATCH ACTIONS TOOLBAR (Initially Hidden) ---
        _batchActionsToolbar = CreateBatchActionsToolbar(context);
        root.AddView(_batchActionsToolbar);

        // --- LYRICS RECYCLER VIEW ---
        var lyricsCard = CreateLyricsRecyclerView(context);
        root.AddView(lyricsCard);

        // --- CONTROL BUTTONS ---
        var controlsCard = CreateControlButtons(context);
        root.AddView(controlsCard);

        // --- FOOTER ACTIONS ---
        var footer = CreateFooter(context);
        root.AddView(footer);

        scrollView.AddView(root);
        return scrollView;
    }

    private LinearLayout CreateHeader(Context context)
    {
        var header = new LinearLayout(context) { Orientation = Orientation.Horizontal };
        header.SetPadding(8, 8, 8, 16);
        header.SetGravity(GravityFlags.CenterVertical);

        var backBtn = new MaterialButton(context, null, Resource.Style.Widget_Material3_Button_IconButton);
        backBtn.SetIconResource(Resource.Drawable.ic_arrow_back_black_24);
        backBtn.Click += (s, e) => ParentFragmentManager.PopBackStack();

        var title = new TextView(context) 
        { 
            Text = "Manual Sync Studio", 
            TextSize = 24 
        };
        title.SetTypeface(null, Android.Graphics.TypefaceStyle.Bold);
        title.LayoutParameters = new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.WrapContent, 
            ViewGroup.LayoutParams.WrapContent) { LeftMargin = 16 };

        header.AddView(backBtn);
        header.AddView(title);
        return header;
    }

    private MaterialCardView CreateSongInfoCard(Context context)
    {
        var card = new MaterialCardView(context) { Radius = 12, Elevation = 2 };
        var cardParams = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
        cardParams.SetMargins(0, 8, 0, 16);
        card.LayoutParameters = cardParams;

        var infoLayout = new LinearLayout(context) { Orientation = Orientation.Horizontal };
        infoLayout.SetPadding(16, 16, 16, 16);

        _coverImage = new ImageView(context);
        _coverImage.LayoutParameters = new LinearLayout.LayoutParams(120, 120);
        _coverImage.SetBackgroundColor(Android.Graphics.Color.DarkGray);

        var textLayout = new LinearLayout(context) { Orientation = Orientation.Vertical };
        textLayout.LayoutParameters = new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent, 
            ViewGroup.LayoutParams.WrapContent) { LeftMargin = 16 };

        _songTitle = new TextView(context) 
        { 
            Text = _selectedSong.Title, 
            TextSize = 18 
        };
        _songTitle.SetTypeface(null, Android.Graphics.TypefaceStyle.Bold);

        _artistName = new TextView(context) 
        { 
            Text = _selectedSong.ArtistName, 
            TextSize = 14 
        };
        _artistName.SetTextColor(Android.Graphics.Color.Gray);

        textLayout.AddView(_songTitle);
        textLayout.AddView(_artistName);

        infoLayout.AddView(_coverImage);
        infoLayout.AddView(textLayout);
        card.AddView(infoLayout);

        return card;
    }

    private MaterialCardView CreatePasteSection(Context context)
    {
        var card = new MaterialCardView(context) { Radius = 12, Elevation = 2 };
        var cardParams = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
        cardParams.SetMargins(0, 0, 0, 16);
        card.LayoutParameters = cardParams;

        var layout = new LinearLayout(context) { Orientation = Orientation.Vertical };
        layout.SetPadding(16, 16, 16, 16);

        var instructionText = new TextView(context)
        {
            Text = "Paste lyrics from clipboard or start syncing existing lyrics.",
            TextSize = 14
        };
        instructionText.SetTextColor(Android.Graphics.Color.Gray);

        _pasteButton = UiBuilder.CreateMaterialButton(
            context, 
            Resources?.Configuration, 
            async (s, e) => await _viewModel.PasteLyricsFromClipboardCommand.ExecuteAsync(null),
            true,
            50,
            Resource.Drawable.clipboard);
        _pasteButton.Text = "Paste Lyrics";
        _pasteButton.LayoutParameters = new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent, 
            ViewGroup.LayoutParams.WrapContent) { TopMargin = 8 };

        layout.AddView(instructionText);
        layout.AddView(_pasteButton);
        card.AddView(layout);

        return card;
    }

    private LinearLayout CreateBatchActionsToolbar(Context context)
    {
        var toolbar = new LinearLayout(context) { Orientation = Orientation.Horizontal };
        toolbar.SetPadding(16, 8, 16, 8);
        toolbar.SetBackgroundColor(Android.Graphics.Color.Argb(50, 100, 100, 255));
        toolbar.Visibility = ViewStates.Gone;
        var toolbarParams = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
        toolbarParams.SetMargins(0, 0, 0, 8);
        toolbar.LayoutParameters = toolbarParams;

        _repeatSelectedButton = UiBuilder.CreateMaterialButton(
            context,
            Resources?.Configuration,
            (s, e) => RepeatSelected(),
            false,
            40,
            Resource.Drawable.repeat);
        _repeatSelectedButton.Text = "Repeat";

        _deleteSelectedButton = UiBuilder.CreateMaterialButton(
            context,
            Resources?.Configuration,
            (s, e) => DeleteSelected(),
            false,
            40,
            Resource.Drawable.delete_icon);
        _deleteSelectedButton.Text = "Delete";

        _clearSelectionButton = UiBuilder.CreateMaterialButton(
            context,
            Resources?.Configuration,
            (s, e) => ClearSelection(),
            false,
            40);
        _clearSelectionButton.Text = "Clear";

        toolbar.AddView(_repeatSelectedButton);
        toolbar.AddView(_deleteSelectedButton);
        toolbar.AddView(_clearSelectionButton);

        return toolbar;
    }

    private MaterialCardView CreateLyricsRecyclerView(Context context)
    {
        var card = new MaterialCardView(context) { Radius = 12, Elevation = 2 };
        var cardParams = new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent, 
            0, 
            1f); // weight=1 to fill remaining space
        cardParams.SetMargins(0, 0, 0, 16);
        card.LayoutParameters = cardParams;

        _lyricsRecyclerView = new RecyclerView(context);
        _lyricsRecyclerView.SetLayoutManager(new LinearLayoutManager(context));
        _lyricsRecyclerView.SetPadding(8, 8, 8, 8);

        _adapter = new LyricsManualSyncAdapter(context, _viewModel, OnSelectionChanged);
        _lyricsRecyclerView.SetAdapter(_adapter);

        card.AddView(_lyricsRecyclerView);
        return card;
    }

    private LinearLayout CreateControlButtons(Context context)
    {
        var layout = new LinearLayout(context) { Orientation = Orientation.Vertical };
        layout.SetPadding(8, 0, 8, 0);
        var layoutParams = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
        layoutParams.SetMargins(0, 0, 0, 16);
        layout.LayoutParameters = layoutParams;

        // SYNC BUTTON
        _syncButton = UiBuilder.CreateMaterialButton(
            context,
            Resources?.Configuration,
            (s, e) => SyncNextLine(),
            true,
            60,
            Resource.Drawable.timestamp_icon);
        _syncButton.Text = "SYNC NEXT LINE (Spacebar)";
        _syncButton.LayoutParameters = new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent) { BottomMargin = 8 };

        // PLAY/PAUSE BUTTON
        _playPauseButton = UiBuilder.CreateMaterialButton(
            context,
            Resources?.Configuration,
            async (s, e) => await _viewModel.PlayPauseToggleCommand.ExecuteAsync(null),
            false,
            50);
        _playPauseButton.Text = _viewModel.IsDimmerPlaying ? "Pause" : "Play";
        _playPauseButton.LayoutParameters = new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent);

        layout.AddView(_syncButton);
        layout.AddView(_playPauseButton);

        return layout;
    }

    private LinearLayout CreateFooter(Context context)
    {
        var footer = new LinearLayout(context) { Orientation = Orientation.Horizontal };
        footer.SetGravity(GravityFlags.End);
        footer.SetPadding(8, 8, 8, 8);

        var cancelButton = UiBuilder.CreateMaterialButton(
            context,
            Resources?.Configuration,
            (s, e) => CancelSession(),
            false,
            45);
        cancelButton.Text = "Cancel";
        cancelButton.LayoutParameters = new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.WrapContent,
            ViewGroup.LayoutParams.WrapContent) { RightMargin = 8 };

        var saveButton = UiBuilder.CreateMaterialButton(
            context,
            Resources?.Configuration,
            async (s, e) => await SaveLyrics(),
            true,
            45,
            Resource.Drawable.savea);
        saveButton.Text = "Save to LRC";

        footer.AddView(cancelButton);
        footer.AddView(saveButton);

        return footer;
    }

    private void SyncNextLine()
    {
        var currentLine = _viewModel.LyricsInEditor?.FirstOrDefault(x => x.IsCurrentLine);
        if (currentLine != null)
        {
            _viewModel.TimestampCurrentLyricLineCommand.Execute(currentLine);
        }
    }

    private async Task SaveLyrics()
    {
        await _viewModel.SaveTimestampedLyricsCommand.ExecuteAsync(_viewModel.CurrentSongPlainLyricsEdit);
        ParentFragmentManager.PopBackStack();
    }

    private void CancelSession()
    {
        _viewModel.CancelLyricsEditingSessionCommand.Execute(null);
        ParentFragmentManager.PopBackStack();
    }

    private void OnSelectionChanged(int selectedCount)
    {
        if (_batchActionsToolbar != null)
        {
            _batchActionsToolbar.Visibility = selectedCount > 0 ? ViewStates.Visible : ViewStates.Gone;
        }
    }

    private void RepeatSelected()
    {
        var selectedLines = _adapter.GetSelectedItems();
        if (selectedLines.Any())
        {
            _viewModel.RepeatSelectedLinesCommand.Execute(selectedLines);
            _adapter.ClearSelection();
        }
    }

    private void DeleteSelected()
    {
        var selectedLines = _adapter.GetSelectedItems();
        if (selectedLines.Any())
        {
            _viewModel.DeleteSelectedLinesCommand.Execute(selectedLines);
            _adapter.ClearSelection();
        }
    }

    private void ClearSelection()
    {
        _adapter.ClearSelection();
    }
}

// Adapter for the RecyclerView
public class LyricsManualSyncAdapter : RecyclerView.Adapter
{
    private readonly Context _context;
    private readonly BaseViewModelAnd _viewModel;
    private readonly Action<int> _onSelectionChanged;
    private readonly HashSet<int> _selectedPositions = new HashSet<int>();

    public LyricsManualSyncAdapter(Context context, BaseViewModelAnd viewModel, Action<int> onSelectionChanged)
    {
        _context = context;
        _viewModel = viewModel;
        _onSelectionChanged = onSelectionChanged;

        if (_viewModel.LyricsInEditor != null)
        {
            _viewModel.LyricsInEditor.CollectionChanged += OnCollectionChanged;
        }
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        NotifyDataSetChanged();
    }

    public override int ItemCount => _viewModel.LyricsInEditor?.Count ?? 0;

    public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
    {
        if (holder is LyricLineViewHolder vh && _viewModel.LyricsInEditor != null)
        {
            var line = _viewModel.LyricsInEditor[position];
            vh.Bind(line, _selectedPositions.Contains(position));
        }
    }

    public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
    {
        var card = new MaterialCardView(parent.Context) { Elevation = 1, Radius = 8 };
        var cardParams = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
        cardParams.SetMargins(4, 4, 4, 4);
        card.LayoutParameters = cardParams;

        var layout = new LinearLayout(parent.Context) { Orientation = Orientation.Horizontal };
        layout.SetPadding(12, 8, 12, 8);
        layout.SetGravity(GravityFlags.CenterVertical);

        // Checkbox for selection
        var checkBox = new CheckBox(parent.Context);
        checkBox.LayoutParameters = new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.WrapContent,
            ViewGroup.LayoutParams.WrapContent) { RightMargin = 8 };

        // Timestamp
        var timestamp = new TextView(parent.Context) { TextSize = 12 };
        timestamp.SetTypeface(Android.Graphics.Typeface.Monospace, Android.Graphics.TypefaceStyle.Normal);
        timestamp.LayoutParameters = new LinearLayout.LayoutParams(100, ViewGroup.LayoutParams.WrapContent);

        // Text input
        var textEdit = new EditText(parent.Context) { TextSize = 14 };
        textEdit.SetBackgroundColor(Android.Graphics.Color.Transparent);
        textEdit.LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1f);

        // Repeat button
        var repeatBtn = new MaterialButton(parent.Context, null, Resource.Style.Widget_Material3_Button_IconButton);
        repeatBtn.SetIconResource(Resource.Drawable.repeat);

        // Delete button
        var deleteBtn = new MaterialButton(parent.Context, null, Resource.Style.Widget_Material3_Button_IconButton);
        deleteBtn.SetIconResource(Resource.Drawable.delete_icon);

        layout.AddView(checkBox);
        layout.AddView(timestamp);
        layout.AddView(textEdit);
        layout.AddView(repeatBtn);
        layout.AddView(deleteBtn);
        card.AddView(layout);

        return new LyricLineViewHolder(card, checkBox, timestamp, textEdit, repeatBtn, deleteBtn, _viewModel, this);
    }

    public void ToggleSelection(int position)
    {
        if (_selectedPositions.Contains(position))
        {
            _selectedPositions.Remove(position);
        }
        else
        {
            _selectedPositions.Add(position);
        }
        NotifyItemChanged(position);
        _onSelectionChanged?.Invoke(_selectedPositions.Count);
    }

    public void ClearSelection()
    {
        var positions = _selectedPositions.ToList();
        _selectedPositions.Clear();
        foreach (var pos in positions)
        {
            NotifyItemChanged(pos);
        }
        _onSelectionChanged?.Invoke(0);
    }

    public List<LyricEditingLineViewModel> GetSelectedItems()
    {
        var selectedItems = new List<LyricEditingLineViewModel>();
        if (_viewModel.LyricsInEditor != null)
        {
            foreach (var position in _selectedPositions.OrderBy(p => p))
            {
                if (position < _viewModel.LyricsInEditor.Count)
                {
                    selectedItems.Add(_viewModel.LyricsInEditor[position]);
                }
            }
        }
        return selectedItems;
    }

    public class LyricLineViewHolder : RecyclerView.ViewHolder
    {
        private readonly CheckBox _checkBox;
        private readonly TextView _timestamp;
        private readonly EditText _textEdit;
        private readonly MaterialButton _repeatBtn;
        private readonly MaterialButton _deleteBtn;
        private readonly BaseViewModelAnd _viewModel;
        private readonly LyricsManualSyncAdapter _adapter;
        private LyricEditingLineViewModel? _currentLine;

        public LyricLineViewHolder(
            View itemView,
            CheckBox checkBox,
            TextView timestamp,
            EditText textEdit,
            MaterialButton repeatBtn,
            MaterialButton deleteBtn,
            BaseViewModelAnd viewModel,
            LyricsManualSyncAdapter adapter) : base(itemView)
        {
            _checkBox = checkBox;
            _timestamp = timestamp;
            _textEdit = textEdit;
            _repeatBtn = repeatBtn;
            _deleteBtn = deleteBtn;
            _viewModel = viewModel;
            _adapter = adapter;

            _checkBox.CheckedChange += (s, e) =>
            {
                _adapter.ToggleSelection(BindingAdapterPosition);
            };

            _repeatBtn.Click += (s, e) =>
            {
                if (_currentLine != null)
                {
                    _viewModel.RepeatLineCommand.Execute(_currentLine);
                }
            };

            _deleteBtn.Click += (s, e) =>
            {
                if (_currentLine != null)
                {
                    _viewModel.DeleteTimestampFromLineCommand.Execute(_currentLine);
                }
            };

            _textEdit.TextChanged += (s, e) =>
            {
                if (_currentLine != null)
                {
                    _currentLine.Text = _textEdit.Text ?? string.Empty;
                }
            };
        }

        public void Bind(LyricEditingLineViewModel line, bool isSelected)
        {
            _currentLine = line;
            _checkBox.Checked = isSelected;
            _timestamp.Text = line.Timestamp;
            _timestamp.Alpha = line.IsTimed ? 1.0f : 0.4f;
            _textEdit.Text = line.Text;

            // Highlight current line
            if (line.IsCurrentLine)
            {
                _textEdit.SetTypeface(null, Android.Graphics.TypefaceStyle.Bold);
                _textEdit.SetTextColor(Android.Graphics.Color.Blue);
            }
            else
            {
                _textEdit.SetTypeface(null, Android.Graphics.TypefaceStyle.Normal);
                _textEdit.SetTextColor(Android.Graphics.Color.Black);
            }
        }
    }
}
