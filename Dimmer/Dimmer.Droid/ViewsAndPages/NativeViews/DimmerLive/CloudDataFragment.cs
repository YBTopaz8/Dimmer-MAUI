using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;

using Dimmer.DimmerLive.Models;
using Dimmer.UiUtils;
using Dimmer.ViewModel;
using DynamicData;

using Google.Android.Material.ProgressIndicator;

using static Dimmer.Utils.AppUtil;

namespace Dimmer.ViewsAndPages.NativeViews.DimmerLive;


public class CloudDataFragment : Fragment, IOnBackInvokedCallback
{
    private readonly string _transitionName;
    private readonly SessionManagementViewModel _viewModel;

    CompositeDisposable _disposables = new();

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

    public override View OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        var ctx = Context;
        var scroll = new Android.Widget.ScrollView(ctx) { FillViewport = true };

        var root = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        root.SetPadding(40, 60, 40, 200);

        // Header
        root.AddView(UiBuilder.CreateHeader(ctx, "Cloud & Sync"));

        // Status Card
        var statusCard = UiBuilder.CreateCard(ctx);
        var statusLayout = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        statusLayout.SetPadding(30, 30, 30, 30);

        _statusText = new MaterialTextView(ctx) { Text = "Ready" };
        _loadingIndicator = new CircularProgressIndicator(ctx) { Indeterminate = true, Visibility = ViewStates.Gone };

        var registerBtn = new MaterialButton(ctx) { Text = "Register This Device" };
        registerBtn.Click += async (s, e) =>
        {
            
            await _viewModel.RegisterCurrentDeviceCommand.ExecuteAsync(null);
        };

        statusLayout.AddView(_statusText);
        statusLayout.AddView(_loadingIndicator);
        statusLayout.AddView(registerBtn);
        statusCard.AddView(statusLayout);
        root.AddView(statusCard);

        // Backup Actions
        root.AddView(UiBuilder.CreateSectionTitle(ctx, "Data Management"));
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
        root.AddView(UiBuilder.CreateSectionTitle(ctx, "Nearby Devices"));
        _devicesRecycler = new RecyclerView(ctx);
        _devicesRecycler.SetLayoutManager(new LinearLayoutManager(ctx));

        _adapter = new DevicesAdapter(ctx, _viewModel,  _disposables);
        _devicesRecycler.SetAdapter(_adapter);
        root.AddView(_devicesRecycler);

        scroll.AddView(root);
        return scroll;
    }
    

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _disposables.Dispose();
        }
        base.Dispose(disposing);
    }

    public override async void OnResume()
    {
        base.OnResume();
            var loginVM = MainApplication.ServiceProvider.GetService<LoginViewModelAnd>();
        if(loginVM is not null) 
            await loginVM.InitAsync();

        if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
        {
            Activity?.OnBackInvokedDispatcher.RegisterOnBackInvokedCallback(
                (int)IOnBackInvokedDispatcher.PriorityDefault, this);
        }

        if (!LoginViewModel.IsAuthenticated)
        {
            var vm = MainApplication.ServiceProvider.GetService<BaseViewModelAnd>();
            if (vm is not null)
            {
                vm.NavigateToAnyPageOfGivenType(this, new LoginFragment("IntoLoginFromCloud", vm), "loginPageTag");
                return;
            }
        }
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

    public void OnBackInvoked()
    {
        var myAct = this.Activity as TransitionActivity;
        if (myAct != null)
        {
            myAct.OnBackPressed();
        }
    }

    class DevicesAdapter : RecyclerView.Adapter
    {
        Context ctx; SessionManagementViewModel vm;
        ReadOnlyObservableCollection<UserDeviceSession> sourceList;
        public DevicesAdapter(Context c, SessionManagementViewModel v, CompositeDisposable _disposables) 
        { 
            ctx = c; vm = v;
            vm.SessionManager.OtherAvailableDevices.
                ObserveOn(RxSchedulers.UI)
                .Bind(out sourceList)
                .Subscribe(chng
                =>
                {
                    NotifyDataSetChanged(); 
                })
                .DisposeWith(_disposables);

        }
        public override int ItemCount => vm.OtherDevices.Count;
        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var vh = holder as SimpleVH;
            if (vh != null)
            {
                var dev = sourceList[position];
                vh.Title.Text = dev.DeviceName;
                vh.Subtitle.Text = dev.DevicePlatform;
                vh.Btn.Text = "Transfer";
                vh.Btn.Click += async (s, e) =>
                {
                    await vm.TransferToDevice(dev);
                };
            }
        }
        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType) => UiBuilder.CreateListItemVH(ctx);
    }
}