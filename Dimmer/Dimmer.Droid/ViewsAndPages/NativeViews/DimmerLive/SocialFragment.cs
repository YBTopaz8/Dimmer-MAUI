using System.Reactive.Disposables;

using Dimmer.DimmerLive.Models;
using Dimmer.ViewModel.DimmerLiveVM;

using Google.Android.Material.Tabs;

namespace Dimmer.ViewsAndPages.NativeViews.DimmerLive;


public class SocialFragment : Fragment, IOnBackInvokedCallback
{
    private readonly string _transitionName;
    private readonly BaseViewModelAnd _viewModel;
    private readonly CompositeDisposable _disposables = new();

    public ChatViewModel ChatVM { get; set; }
    public SocialViewModel SocialVM { get; set; }
    // UI Refs
    private ViewFlipper _viewFlipper;
    private TabLayout _tabLayout;
    private TextInputEditText _searchBar;
    private RecyclerView _listRecycler; // Used for Friends/Search
    private RecyclerView _chatRecycler; // Used for Messages
    private TextInputEditText _msgInput;
    private MaterialButton _sendBtn;
    private TextView _chatHeaderTitle;

    private FriendsAdapter _friendsAdapter;
    private ChatAdapter _chatAdapter;

    public SocialFragment(string transitionName, BaseViewModelAnd viewModel)
    {
        _transitionName = transitionName;
        _viewModel = viewModel;
        ChatVM = MainApplication.ServiceProvider.GetRequiredService<ChatViewModel>();
        SocialVM = MainApplication.ServiceProvider.GetRequiredService<SocialViewModel>();
    }
    public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
    {
        var ctx = Context;
        var root = new LinearLayout(ctx)
        {
            Orientation = Orientation.Vertical,
            LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)
        };
        root.SetBackgroundColor(IsDark() ? Color.ParseColor("#121212") : Color.White);

        // 1. Top Bar (Tabs + Search)
        var topContainer = new LinearLayout(ctx) { Orientation = Orientation.Vertical };

        _tabLayout = new TabLayout(ctx);
        _tabLayout.AddTab(_tabLayout.NewTab().SetText("Friends"));
        _tabLayout.AddTab(_tabLayout.NewTab().SetText("Chat"));
        topContainer.AddView(_tabLayout);

        var searchLayout = new TextInputLayout(ctx)
        {
            Hint = "Search users...",
            LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        };
        ((LinearLayout.LayoutParams)searchLayout.LayoutParameters).SetMargins(20, 10, 20, 10);
        _searchBar = new TextInputEditText(ctx);
        searchLayout.AddView(_searchBar);
        topContainer.AddView(searchLayout);

        root.AddView(topContainer);

        // 2. Main Content (ViewFlipper to switch between List and Active Chat)
        _viewFlipper = new ViewFlipper(ctx)
        {
            LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, 1) // Weight 1
        };

        // VIEW 0: List View (Friends/Conversations)
        _listRecycler = new RecyclerView(ctx);
        _listRecycler.SetLayoutManager(new LinearLayoutManager(ctx));
        _viewFlipper.AddView(_listRecycler);

        // VIEW 1: Active Chat View
        var chatLayout = new LinearLayout(ctx) { Orientation = Orientation.Vertical };

        // Chat Header
        var headerFrame = new FrameLayout(ctx);
        headerFrame.SetPadding(20, 20, 20, 20);
        headerFrame.SetBackgroundColor(IsDark() ? Color.ParseColor("#1E1E1E") : Color.LightGray);
        _chatHeaderTitle = new TextView(ctx) { Text = "Conversation", TextSize = 18, Typeface = Typeface.DefaultBold };
        headerFrame.AddView(_chatHeaderTitle);
        chatLayout.AddView(headerFrame);

        // Messages
        _chatRecycler = new RecyclerView(ctx);
        var chatLayoutMgr = new LinearLayoutManager(ctx) { StackFromEnd = true };
        _chatRecycler.SetLayoutManager(chatLayoutMgr);
        _chatRecycler.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, 1);
        chatLayout.AddView(_chatRecycler);

        // Input Area
        var inputRow = new LinearLayout(ctx) { Orientation = Orientation.Horizontal, WeightSum = 10 };
        inputRow.SetPadding(10, 10, 10, 10);

        var msgInputLayout = new TextInputLayout(ctx) { Hint = "Message..." };
        msgInputLayout.LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 8);
        _msgInput = new TextInputEditText(ctx);
        msgInputLayout.AddView(_msgInput);

        _sendBtn = new MaterialButton(ctx) { Text = ">" };
        _sendBtn.LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 2);

        inputRow.AddView(msgInputLayout);
        inputRow.AddView(_sendBtn);
        chatLayout.AddView(inputRow);

        _viewFlipper.AddView(chatLayout);
        root.AddView(_viewFlipper);

        // Setup Adapters
        _friendsAdapter = new FriendsAdapter(ctx, ChatVM, (conversation) => {
            // User clicked a friend/conversation
            ChatVM.SelectedConversation = conversation; // This will trigger PropertyChanged
        });
        _listRecycler.SetAdapter(_friendsAdapter);

        _chatAdapter = new ChatAdapter(ctx, ChatVM);
        _chatRecycler.SetAdapter(_chatAdapter);

        return root;
    }

    public override void OnResume()
    {
        base.OnResume();

        // 1. Bind Events
        _searchBar.TextChanged += SearchBar_TextChanged;
        _tabLayout.TabSelected += TabLayout_TabSelected;
        _sendBtn.Click += SendBtn_Click;

        // 2. Listen to ViewModel Changes
        ChatVM.PropertyChanged += ChatVM_PropertyChanged;

        // 3. Initial State Check
        UpdateChatView();
    }

    public override void OnPause()
    {
        base.OnPause();
        // Unbind to prevent leaks
        _searchBar.TextChanged -= SearchBar_TextChanged;
        _tabLayout.TabSelected -= TabLayout_TabSelected;
        _sendBtn.Click -= SendBtn_Click;
        ChatVM.PropertyChanged -= ChatVM_PropertyChanged;
    }

    private void SearchBar_TextChanged(object? sender, Android.Text.TextChangedEventArgs e)
    {
        // ChatVM.UserSearchTerm = e.Text.ToString();
        // ChatVM.SearchUsersCommand.Execute(null); 
    }

    private void TabLayout_TabSelected(object? sender, TabLayout.TabSelectedEventArgs e)
    {
        if (e.Tab.Position == 0)
        {
            _viewFlipper.DisplayedChild = 0; // Show Friends List
        }
        else if (e.Tab.Position == 1)
        {
            // Only switch to chat if we actually have one selected
            if (ChatVM.SelectedConversation != null)
                _viewFlipper.DisplayedChild = 1;
        }
    }

    private async void SendBtn_Click(object? sender, EventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(_msgInput.Text))
        {
            ChatVM.NewMessageText = _msgInput.Text;
            await ChatVM.SendMessageCommand.ExecuteAsync(null);
            _msgInput.Text = "";

            // Scroll to bottom
            if (_chatAdapter.ItemCount > 0)
                _chatRecycler.SmoothScrollToPosition(_chatAdapter.ItemCount - 1);
        }
    }

    private void ChatVM_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        Activity?.RunOnUiThread(() =>
        {
            if (e.PropertyName == nameof(ChatViewModel.SelectedConversation))
            {
                UpdateChatView();
            }
            else if (e.PropertyName == nameof(ChatViewModel.Messages))
            {
                _chatAdapter.NotifyDataSetChanged();
                if (_chatAdapter.ItemCount > 0)
                    _chatRecycler.SmoothScrollToPosition(_chatAdapter.ItemCount - 1);
            }
            else if (e.PropertyName == nameof(ChatViewModel.Conversations))
            {
                _friendsAdapter.NotifyDataSetChanged();
            }
        });
    }

    private void UpdateChatView()
    {
        if (ChatVM.SelectedConversation != null)
        {
            _chatHeaderTitle.Text = ChatVM.SelectedConversation.Name ?? "Chat";
            _viewFlipper.DisplayedChild = 1; // Auto switch to chat view
            _tabLayout.GetTabAt(1)?.Select();
        }
        else
        {
            _viewFlipper.DisplayedChild = 0;
            _tabLayout.GetTabAt(0)?.Select();
        }
    }

    private bool IsDark() => (Resources.Configuration.UiMode & Android.Content.Res.UiMode.NightMask) == Android.Content.Res.UiMode.NightYes;

    public void OnBackInvoked()
    {
        
    }

    // --- ADAPTERS ---

    class FriendsAdapter : RecyclerView.Adapter
    {
        Context ctx;
        ChatViewModel vm;
        Action<ChatConversation> onClick; // Pass specific model type here, e.g. Action<ConversationModel>

        public FriendsAdapter(Context c, ChatViewModel v, Action<ChatConversation> click)
        { ctx = c; vm = v; onClick = click; }

        // Use VM data source
        public override int ItemCount => vm.Conversations?.Count ?? 0;

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var vh = holder as SimpleVH;
            Dimmer.DimmerLive.Models.ChatConversation? item = vm.Conversations[position]; // Assuming Conversations is indexable list

            vh.Text.Text = item.Name; // Or whatever property holds the friend name

            vh.ItemView.Click -= vh.ClickHandler; // Avoid duplicate subs
            vh.ClickHandler = (s, e) =>
            {
                onClick(item);
            };
            vh.ItemView.Click += vh.ClickHandler;
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var tv = new TextView(ctx) { TextSize = 18 };
            tv.SetPadding(30, 30, 30, 30);
            return new SimpleVH(tv, tv);
        }
    }

    class ChatAdapter : RecyclerView.Adapter
    {
        Context ctx; ChatViewModel vm;
        public ChatAdapter(Context c, ChatViewModel v) { ctx = c; vm = v; }

        public override int ItemCount => vm.Messages?.Count ?? 0;

        public override int GetItemViewType(int position)
        {
            var msg = vm.Messages[position];
            // 0 = Me, 1 = Them. Logic:
            // return msg.SenderId == vm.CurrentUser.Id ? 0 : 1;
            return 1; // Default for now
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var msg = vm.Messages[position];
            var vh = holder as SimpleVH;
            vh.Text.Text = msg.Text;
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var layout = new LinearLayout(ctx)
            {
                Orientation = Orientation.Vertical,
                LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
            };

            var bubble = new TextView(ctx) { TextSize = 16 };
            bubble.SetPadding(30, 20, 30, 20);
            bubble.Background = new Android.Graphics.Drawables.GradientDrawable();
            ((Android.Graphics.Drawables.GradientDrawable)bubble.Background).SetCornerRadius(40);

            var lp = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);

            if (viewType == 0) // Me (Right)
            {
                layout.SetGravity(GravityFlags.Right);
                ((Android.Graphics.Drawables.GradientDrawable)bubble.Background).SetColor(Color.ParseColor("#0078D7"));
                bubble.SetTextColor(Color.White);
                lp.SetMargins(100, 10, 20, 10);
            }
            else // Them (Left)
            {
                layout.SetGravity(GravityFlags.Left);
                ((Android.Graphics.Drawables.GradientDrawable)bubble.Background).SetColor(Color.ParseColor("#333333"));
                bubble.SetTextColor(Color.White);
                lp.SetMargins(20, 10, 100, 10);
            }

            bubble.LayoutParameters = lp;
            layout.AddView(bubble);
            return new SimpleVH(layout, bubble);
        }
    }

    class SimpleVH : RecyclerView.ViewHolder
    {
        public TextView Text;
        public EventHandler ClickHandler; // To store ref for unsubscribing if needed
        public SimpleVH(View v, TextView t) : base(v) { Text = t; }
    }
}