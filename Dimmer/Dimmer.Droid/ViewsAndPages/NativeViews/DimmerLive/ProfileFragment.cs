using Bumptech.Glide;
using Dimmer.DimmerLive.Models;
using Dimmer.UiUtils;
using Google.Android.Material.Dialog;
using Google.Android.Material.Tabs;
using System.Reactive.Disposables;
using ScrollView = Android.Widget.ScrollView;

namespace Dimmer.ViewsAndPages.NativeViews.DimmerLive;



public class ProfileFragment : Fragment
{
    private readonly string _transitionName;
    public LoginViewModelAnd LoginVM { get; private set; }
    private readonly CompositeDisposable _disposables = new();

    // UI Elements
    private TabLayout _tabLayout;
    private FrameLayout _contentFrame;
    private LinearLayout _overviewTab, _securityTab, _premiumTab;

    // Overview Controls
    private ImageView _avatar;
    private TextView _username, _email, _bio, _premiumBadge;
    private TextView _statJoined, _statLastFm, _deviceInfoText;
    private MaterialButton _editBtn, _shareBtn, _cloudBtn, _logoutBtn;

    public ProfileFragment(string transitionName, LoginViewModelAnd viewModel)
    {
        _transitionName = transitionName;
        LoginVM = viewModel;
    }

    public override View OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        var ctx = Context;
        var root = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        root.SetBackgroundColor(UiBuilder.IsDark(ctx) ? Color.ParseColor("#121212") : Color.White);

        // 1. Tab Header (Pivot Parity)
        _tabLayout = new TabLayout(ctx);
        _tabLayout.AddTab(_tabLayout.NewTab().SetText("Overview"));
        _tabLayout.AddTab(_tabLayout.NewTab().SetText("Security"));
        _tabLayout.AddTab(_tabLayout.NewTab().SetText("Premium"));
        _tabLayout.TabSelected += (s, e) => SwitchTab(e.Tab.Position);
        root.AddView(_tabLayout);

        // 2. Content Container
        _contentFrame = new FrameLayout(ctx);
        _contentFrame.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);

        CreateOverviewTab(ctx);
        CreateSecurityTab(ctx);
        CreatePremiumTab(ctx);

        _contentFrame.AddView(_overviewTab);
        _contentFrame.AddView(_securityTab);
        _contentFrame.AddView(_premiumTab);

        root.AddView(_contentFrame);
        SwitchTab(0); // Default to Overview

        return root;
    }

    private void CreateOverviewTab(Context ctx)
    {
        var scroll = new ScrollView(ctx);
        _overviewTab = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        _overviewTab.SetPadding(40, 40, 40, 100);

        // --- Header Card ---
        var card = UiBuilder.CreateCard(ctx);
        var cardContent = new LinearLayout(ctx) { Orientation = Orientation.Horizontal, WeightSum = 3 };
        cardContent.SetPadding(30, 30, 30, 30);

        _avatar = new ImageView(ctx);
        _avatar.LayoutParameters = new LinearLayout.LayoutParams(180, 180);
        _avatar.SetBackgroundColor(Color.DarkGray);
        _avatar.Click += async (s, e) => await LoginVM.PickImageFromDeviceCommand.ExecuteAsync(null);

        var infoStack = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        infoStack.LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 2);
        infoStack.SetPadding(30, 0, 0, 0);

        var nameRow = new LinearLayout(ctx) { Orientation = Orientation.Horizontal };
        _username = new TextView(ctx) { TextSize = 22, Typeface = Typeface.DefaultBold };

        _premiumBadge = new TextView(ctx) { Text = "PREMIUM", TextSize = 10, Typeface = Typeface.DefaultBold };
        _premiumBadge.SetTextColor(Color.Black);
        _premiumBadge.SetBackgroundColor(Color.Gold);
        _premiumBadge.SetPadding(10, 5, 10, 5);
        _premiumBadge.Visibility = ViewStates.Gone; // Managed by logic

        nameRow.AddView(_username);
        nameRow.AddView(_premiumBadge);

        _email = new TextView(ctx) { TextSize = 14, Alpha = 0.6f };
        _bio = new TextView(ctx) { TextSize = 14 };
        _bio.SetPadding(0, 15, 0, 0);

        infoStack.AddView(nameRow);
        infoStack.AddView(_email);
        infoStack.AddView(_bio);

        cardContent.AddView(_avatar);
        cardContent.AddView(infoStack);
        card.AddView(cardContent);
        _overviewTab.AddView(card);

        // --- Action Buttons ---
        _shareBtn = new MaterialButton(ctx) { Text = "Invite / Share Profile" };
        _shareBtn.Click += ShareProfile;
        _overviewTab.AddView(_shareBtn);

        _overviewTab.AddView(UiBuilder.CreateSectionTitle(ctx, "Details"));
        var statsRow = new LinearLayout(ctx) { Orientation = Orientation.Horizontal };
        _statLastFm = UiBuilder.CreateStatItem(ctx, "Last.FM", "No");
        _statJoined = UiBuilder.CreateStatItem(ctx, "Joined", "-");
        statsRow.AddView(_statLastFm);
        statsRow.AddView(_statJoined);
        _overviewTab.AddView(statsRow);

        _overviewTab.AddView(UiBuilder.CreateSectionTitle(ctx, "Current Device"));
        _deviceInfoText = new TextView(ctx) { Alpha = 0.7f };
        _overviewTab.AddView(_deviceInfoText);

        _cloudBtn = new MaterialButton(ctx) { Text = "Cloud Space" };
        _overviewTab.AddView(_cloudBtn);

        _logoutBtn = new MaterialButton(ctx) { Text = "Sign Out" };
        _logoutBtn.SetBackgroundColor(Color.DarkRed);
        _logoutBtn.Click += async (s, e) => {
            await LoginVM.LogoutCommand.ExecuteAsync(null);
            ParentFragmentManager.PopBackStack();
        };
        _overviewTab.AddView(_logoutBtn);

        scroll.AddView(_overviewTab);
        _overviewTab = new LinearLayout(ctx) { Orientation = Orientation.Vertical }; // Proxy to wrapper
        _overviewTab.AddView(scroll);
    }

    private void CreateSecurityTab(Context ctx)
    {
        _securityTab = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        _securityTab.SetPadding(50, 50, 50, 50);

        _securityTab.AddView(UiBuilder.CreateSectionTitle(ctx, "Password Management"));

        var passLayout = new TextInputLayout(ctx) { Hint = "New Password", EndIconMode = TextInputLayout.EndIconPasswordToggle };
        var passEdit = new TextInputEditText(ctx) { InputType = Android.Text.InputTypes.TextVariationPassword | Android.Text.InputTypes.ClassText };
        passLayout.AddView(passEdit);

        var confirmBtn = new MaterialButton(ctx) { Text = "Update Password" };
        confirmBtn.Click += async (s, e) => {
            if (!string.IsNullOrEmpty(passEdit.Text))
            {
                await LoginVM.ChangePasswordCommand.ExecuteAsync(passEdit.Text);
                Toast.MakeText(ctx, "Updated!", ToastLength.Short).Show();
                passEdit.Text = "";
            }
        };

        _securityTab.AddView(passLayout);
        _securityTab.AddView(confirmBtn);
    }

    private void CreatePremiumTab(Context ctx)
    {
        _premiumTab = new LinearLayout(ctx) { Orientation = Orientation.Vertical};
        _premiumTab.SetGravity(GravityFlags.Center);
        _premiumTab.SetPadding(50, 50, 50, 50);

        var title = new TextView(ctx) { Text = "Premium Plan", TextSize = 24, Gravity = GravityFlags.Center };
        var desc = new TextView(ctx) { Text = "Unlock Cloud Backups & Audio Sharing", Gravity = GravityFlags.Center, Alpha = 0.7f };

        var subBtn = new MaterialButton(ctx) { Text = "Subscribe Now" };
        subBtn.SetBackgroundColor(Color.Gold);
        subBtn.SetTextColor(Color.Black);

        _premiumTab.AddView(title);
        _premiumTab.AddView(desc);
        _premiumTab.AddView(subBtn);
    }

    private void SwitchTab(int position)
    {
        _overviewTab.Visibility = position == 0 ? ViewStates.Visible : ViewStates.Gone;
        _securityTab.Visibility = position == 1 ? ViewStates.Visible : ViewStates.Gone;
        _premiumTab.Visibility = position == 2 ? ViewStates.Visible : ViewStates.Gone;
    }

    private void UpdateProfileUI(UserModelOnline user)
    {
        _username.Text = user.Username;
        _email.Text = user.Email;
        _bio.Text = LoginVM.CurrentUser?.UserBio ?? "No bio set";

        // Premium Badge Logic (XAML parity)
        _premiumBadge.Visibility = user.IsPremium ? ViewStates.Visible : ViewStates.Gone;

        // Last.FM Logic (XAML parity)
        var lastFm = LoginVM.CurrentUser?.LastFMAccountInfo?.Name;
        _statLastFm.Text = string.IsNullOrEmpty(lastFm) ? "No" : lastFm;

        // Device Info Logic (XAML parity)
        var device = LoginVM.CurrentUser;
        _deviceInfoText.Text = $"{device?.DeviceName}\n{device?.DeviceModel}\nOS: {device?.DeviceVersion}";

        if (!string.IsNullOrEmpty(user.ProfileImagePath))
            Glide.With(this).Load(user.ProfileImagePath).CircleCrop().Into(_avatar);

        _statJoined.Text = user.CreatedAt?.ToString("MMM yyyy") ?? "-";
    }

    private void ShareProfile(object sender, EventArgs e)
    {
        var user = LoginVM.CurrentUserOnline;
        if (user == null) return;

        var sendIntent = new Intent();
        sendIntent.SetAction(Intent.ActionSend);
        sendIntent.PutExtra(Intent.ExtraText, $"Add me on Dimmer! Username: {user.Username}");
        sendIntent.SetType("text/plain");

        var shareIntent = Intent.CreateChooser(sendIntent, "Share Profile via");
        StartActivity(shareIntent);
    }
    private void ShowEditBioDialog(object? sender, EventArgs e)
    {
        var ctx = Context;
        var dialogView = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        dialogView.SetPadding(50, 50, 50, 50);

        var editBio = new TextInputEditText(ctx) { Hint = "Bio", Text = LoginVM.CurrentUser?.UserBio };
        dialogView.AddView(editBio);

        new MaterialAlertDialogBuilder(ctx)
            .SetTitle("Edit Profile")
            .SetView(dialogView)
            .SetPositiveButton("Save", async (s, a) => {
                if (LoginVM.CurrentUser != null)
                {
                    LoginVM.CurrentUser.UserBio = editBio.Text;
                    // You might need to copy this to CurrentUserOnline if that's what Saves
                    await LoginVM.SaveProfileChangesCommand.ExecuteAsync(null);
                }
            })
            .SetNegativeButton("Cancel", (s, a) => { })
            .Show();
    }

    private void ShowChangePassDialog(object? sender, EventArgs e)
    {
        var ctx = Context;
        var dialogView = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        dialogView.SetPadding(50, 50, 50, 50);

        var editPass = new TextInputEditText(ctx) { Hint = "New Password", InputType = Android.Text.InputTypes.TextVariationPassword };
        dialogView.AddView(editPass);

        new MaterialAlertDialogBuilder(ctx)
            .SetTitle("Change Password")
            .SetView(dialogView)
            .SetPositiveButton("Update", async (s, a) => {
                if (!string.IsNullOrWhiteSpace(editPass.Text))
                {
                    await LoginVM.ChangePasswordCommand.ExecuteAsync(editPass.Text);
                    Toast.MakeText(ctx, "Password updated", ToastLength.Short)?.Show();
                }
            })
            .SetNegativeButton("Cancel", (s, a) => { })
            .Show();
    }
}