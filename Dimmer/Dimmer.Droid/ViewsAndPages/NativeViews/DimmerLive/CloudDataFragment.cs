using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;

using Google.Android.Material.ProgressIndicator;

namespace Dimmer.ViewsAndPages.NativeViews.DimmerLive;

public class CloudDataFragment : Fragment, IOnBackInvokedCallback
{
    private readonly string _transitionName;
    private readonly BaseViewModelAnd _viewModel;
    private readonly CompositeDisposable _disposables = new();

    // UI References
    private TextView _statusMessage;
    private Button _backupBtn;
    private Button _registerBtn;
    private CircularProgressIndicator _busyIndicator;
    private RecyclerView _devicesRecycler;
    private DevicesAdapter _adapter;

    public CloudDataFragment(string transitionName, BaseViewModelAnd viewModel)
    {
        _transitionName = transitionName;
        _viewModel = viewModel;
        // Material Shared Axis Transitions
        EnterTransition = new Google.Android.Material.Transition.MaterialSharedAxis(Google.Android.Material.Transition.MaterialSharedAxis.Z, true);
        ReturnTransition = new Google.Android.Material.Transition.MaterialSharedAxis(Google.Android.Material.Transition.MaterialSharedAxis.Z, false);
    }

    public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
    {
        var ctx = Context;
        var scroll = new Android.Widget.ScrollView(ctx)
        {
            LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent),
            FillViewport = true,
            Background = new Android.Graphics.Drawables.ColorDrawable(IsDark() ? Color.ParseColor("#121212") : Color.ParseColor("#F5F5F5"))
        };

        var root = new LinearLayout(ctx)
        {
            Orientation = Orientation.Vertical,
            LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        };
        root.SetPadding(40, 60, 40, 200);

        // Header
        var header = new MaterialTextView(ctx)
        {
            Text = "Cloud & Devices",
            TextSize = 32,
            Typeface = Typeface.DefaultBold
        };
        header.TransitionName = _transitionName;
        root.AddView(header);

        var subHeader = new TextView(ctx)
        {
            Text = "Manage backups and sync playback.",
            Alpha = 0.7f
        };
        root.AddView(subHeader);

        // --- SECTION 1: This Device ---
        root.AddView(CreateSectionTitle(ctx, "This Device"));
        var deviceCard = CreateCard(ctx);
        var deviceLinear = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        deviceLinear.SetPadding(30, 30, 30, 30);

        _statusMessage = new TextView(ctx) { Text = "Status: Unknown", TextSize = 14 };
        _statusMessage.SetPadding(0, 0, 0, 20);

        _registerBtn = new MaterialButton(ctx) { Text = "Register / Refresh Device" };
        _registerBtn.Click += async (s, e) =>
        {
            // Assumes SessionManagementViewModel is accessible via BaseViewModelAnd
            // Adjust specific casting based on your ViewModel structure
            //await _viewModel.SessionMgmtVM.RegisterCurrentDeviceAsyncCommand.ExecuteAsync(null);
        };

        deviceLinear.AddView(_statusMessage);
        deviceLinear.AddView(_registerBtn);
        deviceCard.AddView(deviceLinear);
        root.AddView(deviceCard);

        // --- SECTION 2: Cloud Backup ---
        root.AddView(CreateSectionTitle(ctx, "Cloud Backup"));
        var backupCard = CreateCard(ctx);
        var backupLinear = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        backupLinear.SetPadding(30, 30, 30, 30);

        var backupDesc = new TextView(ctx) { Text = "Save play history and settings." };

        _busyIndicator = new CircularProgressIndicator(ctx) { Indeterminate = true, Visibility = ViewStates.Gone };

        _backupBtn = new MaterialButton(ctx) { Text = "Backup Now" };
        _backupBtn.SetBackgroundColor(Color.ParseColor("#6200EE")); // Accent
        _backupBtn.Click += async (s, e) =>
        {
            //await _viewModel.SessionMgmtVM.BackUpDataToCloudCommand.ExecuteAsync(null);
        };

        backupLinear.AddView(backupDesc);
        backupLinear.AddView(_busyIndicator);
        backupLinear.AddView(_backupBtn);
        backupCard.AddView(backupLinear);
        root.AddView(backupCard);

        // --- SECTION 3: Remote Devices List ---
        root.AddView(CreateSectionTitle(ctx, "Available Devices"));

        _devicesRecycler = new RecyclerView(ctx);
        _devicesRecycler.SetLayoutManager(new LinearLayoutManager(ctx));
        _devicesRecycler.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);

        _adapter = new DevicesAdapter(ctx, _viewModel);
        _devicesRecycler.SetAdapter(_adapter);

        root.AddView(_devicesRecycler);

        scroll.AddView(root);
        return scroll;
    }

    public override void OnResume()
    {
        base.OnResume();

        // Bind ViewModel Properties
        // Note: Replacing explicit x:Bind with Rx Subscriptions

        // Bind Status Message
        //_viewModel.SessionMgmtVM.WhenAnyValue(vm => vm.StatusMessage)
        //    .ObserveOn(RxSchedulers.UI)
        //    .Subscribe(msg => _statusMessage.Text = $"Status: {msg}")
        //    .DisposeWith(_disposables);

        //// Bind IsBusy (Toggle Backup Button and Spinner)
        //_viewModel.SessionMgmtVM.WhenAnyValue(vm => vm.IsBusy)
        //    .ObserveOn(RxSchedulers.UI)
        //    .Subscribe(busy =>
        //    {
        //        _backupBtn.Enabled = !busy;
        //        _busyIndicator.Visibility = busy ? ViewStates.Visible : ViewStates.Gone;
        //    })
        //    .DisposeWith(_disposables);

        // Bind Devices Collection
        // Assuming OtherDevices is an ObservableCollection. 
        // For DynamicData ReadOnlyObservableCollection, we might need a different trigger or simple polling for this UI demo.
        // Here we just notify adapter on resume, but ideally subscription to collection changes is needed.
        _adapter.NotifyDataSetChanged();
    }

    public override void OnPause()
    {
        base.OnPause();
        _disposables.Clear();
    }

    // --- Helpers ---
    private MaterialCardView CreateCard(Context ctx)
    {
        var card = new MaterialCardView(ctx)
        {
            Radius = AppUtil.DpToPx(16),
            CardElevation = AppUtil.DpToPx(2),
            LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        };
        ((LinearLayout.LayoutParams)card.LayoutParameters).SetMargins(0, 10, 0, 20);
        card.SetBackgroundColor(IsDark() ? Color.ParseColor("#202020") : Color.White);
        return card;
    }

    private TextView CreateSectionTitle(Context ctx, string title)
    {
        var tv = new TextView(ctx) { Text = title, TextSize = 16, Typeface = Typeface.DefaultBold };
        tv.SetTextColor(IsDark() ? Color.LightGray : Color.DarkGray);
        tv.SetPadding(10, 30, 0, 10);
        return tv;
    }

    private bool IsDark() => (Resources.Configuration.UiMode & Android.Content.Res.UiMode.NightMask) == Android.Content.Res.UiMode.NightYes;

    public void OnBackInvoked() => Toast.MakeText(Context!, "Back invoked", ToastLength.Short)?.Show();

    // --- INNER ADAPTER ---
    private class DevicesAdapter : RecyclerView.Adapter
    {
        Context context;
        BaseViewModelAnd vm;
        public DevicesAdapter(Context context, BaseViewModelAnd vm) { this.context = context; this.vm = vm; }

        public override int ItemCount => 3;
        //public override int ItemCount => vm.SessionMgmtVM.OtherDevices.Count;

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var vh = holder as DeviceVH;
            //var device = vm.SessionMgmtVM.OtherDevices[position];
            //vh.NameTxt.Text = device.DeviceName;
            //vh.PlatformTxt.Text = $"{device.DevicePlatform} • {device.DeviceManufacturer}";

            //vh.TransferBtn.Click -= vh.LastClickHandler; // Remove old handler
            //vh.LastClickHandler = async (s, e) =>
            //{
            //    await vm.SessionMgmtVM.TransferToDeviceCommand.ExecuteAsync(device);
            //};
            //vh.TransferBtn.Click += vh.LastClickHandler;
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var layout = new LinearLayout(context) { Orientation = Orientation.Horizontal, WeightSum = 10 };
            layout.SetPadding(20, 20, 20, 20);
            layout.LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);

            var infoLayout = new LinearLayout(context) { Orientation = Orientation.Vertical };
            infoLayout.LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 7);

            var name = new TextView(context) { TextSize = 16, Typeface = Typeface.DefaultBold };
            var platform = new TextView(context) { TextSize = 12 };
            infoLayout.AddView(name);
            infoLayout.AddView(platform);

            var btn = new MaterialButton(context) { Text = "Transfer" };
            btn.LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 3);
            btn.SetTextSize(Android.Util.ComplexUnitType.Sp, 10);

            layout.AddView(infoLayout);
            layout.AddView(btn);

            return new DeviceVH(layout, name, platform, btn);
        }
    }

    private class DeviceVH : RecyclerView.ViewHolder
    {
        public TextView NameTxt, PlatformTxt;
        public Button TransferBtn;
        public EventHandler LastClickHandler;
        public DeviceVH(View v, TextView n, TextView p, Button b) : base(v) { NameTxt = n; PlatformTxt = p; TransferBtn = b; }
    }
}