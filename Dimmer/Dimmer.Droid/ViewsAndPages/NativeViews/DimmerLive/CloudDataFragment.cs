using Google.Android.Material.ProgressIndicator;

using static Dimmer.Utils.AppUtil;

namespace Dimmer.ViewsAndPages.NativeViews.DimmerLive;


public class CloudDataFragment : Fragment
{
    private readonly string _transitionName;
    private readonly SessionManagementViewModel _viewModel;

    // UI References
    private MaterialTextView _statusText;
    private CircularProgressIndicator _loadingIndicator;
    private RecyclerView _devicesRecycler;
    private MaterialButton _backupBtn, _restoreBtn;
    private DevicesAdapter _adapter;

    public CloudDataFragment(string transitionName, SessionManagementViewModel viewModel)
    {
        _transitionName = transitionName;
        _viewModel = viewModel;
    }

    public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
    {
        var ctx = Context;
        var scroll = new Android.Widget.ScrollView(ctx) { FillViewport = true };

        var root = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        root.SetPadding(40, 60, 40, 200);

        // Header
        root.AddView(AppUtil.CreateHeader(ctx, "Cloud & Sync"));

        // Status Card
        var statusCard = AppUtil.CreateCard(ctx);
        var statusLayout = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        statusLayout.SetPadding(30, 30, 30, 30);

        _statusText = new MaterialTextView(ctx) { Text = "Ready" };
        _loadingIndicator = new CircularProgressIndicator(ctx) { Indeterminate = true, Visibility = ViewStates.Gone };

        var registerBtn = new MaterialButton(ctx) { Text = "Register This Device" };
        registerBtn.Click += async (s, e) => await _viewModel.RegisterCurrentDeviceCommand.ExecuteAsync(null);

        statusLayout.AddView(_statusText);
        statusLayout.AddView(_loadingIndicator);
        statusLayout.AddView(registerBtn);
        statusCard.AddView(statusLayout);
        root.AddView(statusCard);

        // Backup Actions
        root.AddView(AppUtil.CreateSectionTitle(ctx, "Data Management"));
        var actionLayout = new LinearLayout(ctx) { Orientation = Orientation.Horizontal, WeightSum = 2 };

        _backupBtn = new MaterialButton(ctx) { Text = "Backup" };
        _backupBtn.LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1);
        _backupBtn.Click += async (s, e) => await _viewModel.BackUpDataToCloudCommand.ExecuteAsync(null);

        _restoreBtn = new MaterialButton(ctx) { Text = "Restore" };
        _restoreBtn.LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1);
        _restoreBtn.Click += async (s, e) => await _viewModel.RestoreBackupAsync(null);

        actionLayout.AddView(_backupBtn);
        actionLayout.AddView(_restoreBtn);
        root.AddView(actionLayout);

        // Devices List
        root.AddView(AppUtil.CreateSectionTitle(ctx, "Nearby Devices"));
        _devicesRecycler = new RecyclerView(ctx);
        _devicesRecycler.SetLayoutManager(new LinearLayoutManager(ctx));
        _adapter = new DevicesAdapter(ctx, _viewModel);
        _devicesRecycler.SetAdapter(_adapter);
        root.AddView(_devicesRecycler);

        scroll.AddView(root);
        return scroll;
    }

    public override void OnResume()
    {
        base.OnResume();
        // Subscribe to PropertyChanged
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        // Initial State
        UpdateStatus(_viewModel.StatusMessage);
        UpdateBusy(_viewModel.IsBusy);
        _adapter.NotifyDataSetChanged();
    }

    public override void OnPause()
    {
        base.OnPause();
        // Unsubscribe to prevent leaks
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Run on UI Thread to be safe
        Activity?.RunOnUiThread(() =>
        {
            if (e.PropertyName == nameof(SessionManagementViewModel.StatusMessage))
            {
                UpdateStatus(_viewModel.StatusMessage);
            }
            else if (e.PropertyName == nameof(SessionManagementViewModel.IsBusy))
            {
                UpdateBusy(_viewModel.IsBusy);
            }
            else if (e.PropertyName == nameof(SessionManagementViewModel.OtherDevices))
            {
                _adapter.NotifyDataSetChanged();
            }
        });
    }

    private void UpdateStatus(string msg) => _statusText.Text = msg;

    private void UpdateBusy(bool isBusy)
    {
        _loadingIndicator.Visibility = isBusy ? ViewStates.Visible : ViewStates.Gone;
        _backupBtn.Enabled = !isBusy;
        _restoreBtn.Enabled = !isBusy;
    }
    class DevicesAdapter : RecyclerView.Adapter
    {
        Context ctx; SessionManagementViewModel vm;
        public DevicesAdapter(Context c, SessionManagementViewModel v) { ctx = c; vm = v; }
        public override int ItemCount => vm.OtherDevices.Count;
        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var vh = holder as SimpleVH;
            var dev = vm.OtherDevices[position];
            vh.Title.Text = dev.DeviceName;
            vh.Subtitle.Text = dev.DevicePlatform;
            vh.Btn.Text = "Transfer";
            vh.Btn.Click += async (s, e) => await vm.TransferToDeviceCommand.ExecuteAsync(dev);
        }
        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType) => AppUtil.CreateListItemVH(ctx);
    }
}