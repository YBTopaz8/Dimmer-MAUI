using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;

using Google.Android.Material.Tabs;

namespace Dimmer.ViewsAndPages.NativeViews.DimmerLive;


public class SocialFragment : Fragment, IOnBackInvokedCallback
{
    private readonly string _transitionName;
    private readonly BaseViewModelAnd _viewModel;
    private readonly CompositeDisposable _disposables = new();

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
        var backBtn = new ImageView(ctx) { }; // Simple back icon logic could go here
        headerFrame.AddView(_chatHeaderTitle);
        chatLayout.AddView(headerFrame);

        // Messages
        _chatRecycler = new RecyclerView(ctx);
        _chatRecycler.SetLayoutManager(new LinearLayoutManager(ctx) { StackFromEnd = true }); // Chat style
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
        _friendsAdapter = new FriendsAdapter(ctx, _viewModel, (user) => {
            // On Friend Click -> Open Chat
            //_viewModel.ChatVM.FindAndStartChatCommand.Execute(user);
            _viewFlipper.DisplayedChild = 1; // Show Chat
        });
        _listRecycler.SetAdapter(_friendsAdapter);

        _chatAdapter = new ChatAdapter(ctx, _viewModel);
        _chatRecycler.SetAdapter(_chatAdapter);

        return root;
    }

    public override void OnResume()
    {
        base.OnResume();

        // Bind Search
        //_searchBar.TextChanged += (s, e) => _viewModel.ChatVM.UserSearchTerm = e.Text.ToString();

        // Bind Tab Selection
        _tabLayout.TabSelected += (s, e) =>
        {
            // Simple logic: Tab 0 shows list, Tab 1 shows active chat (if selected)
            if (e.Tab.Position == 0) _viewFlipper.DisplayedChild = 0;
            //else if (_viewModel.ChatVM.SelectedConversation != null)
            //{
            //    _viewFlipper.DisplayedChild = 1;
            //}
        };

        // Bind Send Message
        _sendBtn.Click += async (s, e) =>
        {
            //_viewModel.ChatVM.NewMessageText = _msgInput.Text;
            //await _viewModel.ChatVM.SendMessageCommand.ExecuteAsync(null);
            _msgInput.Text = "";
            _chatRecycler.SmoothScrollToPosition(_chatAdapter.ItemCount - 1);
        };

        // Reactive Bindings for Lists
        //_viewModel.ChatVM.Conversations.Connect()
        //    .ObserveOn(RxSchedulers.UI)
        //    .Subscribe(_ => _friendsAdapter.NotifyDataSetChanged())
        //    .DisposeWith(_disposables);

        //_viewModel.ChatVM.Messages.Connect()
        //    .ObserveOn(RxSchedulers.UI)
        //    .Subscribe(_ =>
        //    {
        //        _chatAdapter.NotifyDataSetChanged();
        //        if (_chatAdapter.ItemCount > 0)
        //            _chatRecycler.SmoothScrollToPosition(_chatAdapter.ItemCount - 1);
        //    })
        //    .DisposeWith(_disposables);

        //// Bind Conversation Title
        //_viewModel.ChatVM.WhenAnyValue(vm => vm.SelectedConversation)
        //    .ObserveOn(RxSchedulers.UI)
        //    .Subscribe(c =>
        //    {
        //        if (c != null)
        //        {
        //            _chatHeaderTitle.Text = c.Name ?? "Chat";
        //            _viewFlipper.DisplayedChild = 1; // Auto switch
        //            _tabLayout.GetTabAt(1).Select();
        //        }
        //    })
        //    .DisposeWith(_disposables);
    }

    public override void OnPause()
    {
        base.OnPause();
        _disposables.Clear();
    }

    private bool IsDark() => (Resources.Configuration.UiMode & Android.Content.Res.UiMode.NightMask) == Android.Content.Res.UiMode.NightYes;
    public void OnBackInvoked()
    {
        if (_viewFlipper.DisplayedChild == 1)
        {
            _viewFlipper.DisplayedChild = 0; // Go back to list
            _tabLayout.GetTabAt(0).Select();
        }
    }

    // --- ADAPTERS --- (Simplified for brevity)
    class FriendsAdapter : RecyclerView.Adapter
    {
        // Implementation similar to DeviceAdapter, binds ViewModel.ChatVM.Conversations or SearchResults
        Context ctx; BaseViewModelAnd vm; Action<object> onClick;
        public FriendsAdapter(Context c, BaseViewModelAnd v, Action<object> click) { ctx = c; vm = v; onClick = click; }
        public override int ItemCount => 2;
        //public override int ItemCount => vm.ChatVM.Conversations.Count;
        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var vh = holder as SimpleVH;
            //var item = vm.ChatVM.Conversations.ElementAt(position);
            //vh.Text.Text = item.Name;
            //vh.ItemView.Click += (s, e) => onClick(item);
        }
        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var tv = new TextView(ctx) { TextSize = 18 }; tv.SetPadding(30, 30, 30, 30);
            return new SimpleVH(tv, tv);
        }
    }

    class ChatAdapter : RecyclerView.Adapter
    {
        Context ctx; BaseViewModelAnd vm;
        public ChatAdapter(Context c, BaseViewModelAnd v) { ctx = c; vm = v; }
        public override int ItemCount => 2;
        //public override int ItemCount => vm.ChatVM.Messages.Count;

        public override int GetItemViewType(int position)
        {
            //var msg = vm.ChatVM.Messages.ElementAt(position);
            //// 0 = Me, 1 = Them, 2 = SongShare
            //if (msg.MessageType == "SongShare") return 2;
            //// Simple check: assuming Username on VM matches msg sender
            //return msg.UserName == vm.ChatVM.ChatService.Username ? 0 : 1;
            return 1;
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            //var msg = vm.ChatVM.Messages.ElementAt(position);
            //var vh = holder as SimpleVH;

            //if (holder.ItemViewType == 2) // Song Share
            //{
            //    vh.Text.Text = $"🎵 Shared: {msg.SharedSong?.Title}\n{msg.SharedSong?.ArtistName}";
            //    vh.Text.SetBackgroundColor(Color.ParseColor("#6200EE"));
            //    vh.Text.SetTextColor(Color.White);
            //}
            //else
            //{
            //    vh.Text.Text = msg.Text;
            //}
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var layout = new LinearLayout(ctx) { Orientation = Orientation.Vertical, LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent) };
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
            else if (viewType == 1) // Them (Left)
            {
                layout.SetGravity(GravityFlags.Left);
                ((Android.Graphics.Drawables.GradientDrawable)bubble.Background).SetColor(Color.ParseColor("#333333"));
                bubble.SetTextColor(Color.White);
                lp.SetMargins(20, 10, 100, 10);
            }
            else // Song Share (Center)
            {
                layout.SetGravity(GravityFlags.Center);
                lp.SetMargins(20, 10, 20, 10);
            }

            bubble.LayoutParameters = lp;
            layout.AddView(bubble);
            return new SimpleVH(layout, bubble);
        }
    }

    class SimpleVH : RecyclerView.ViewHolder { public TextView Text; public SimpleVH(View v, TextView t) : base(v) { Text = t; } }
}