using System.Reactive.Disposables;

using Bumptech.Glide;

using Dimmer.DimmerLive.Models;
using Dimmer.UiUtils;
using Google.Android.Material.Dialog;

namespace Dimmer.ViewsAndPages.NativeViews.DimmerLive;



public class ProfileFragment : Fragment
{
    private readonly string _transitionName;
    public LoginViewModelAnd LoginVM { get; private set; }
    private readonly CompositeDisposable _disposables = new();

    private ImageView _avatar;
    private TextView _username, _email, _bio;
    private TextView _statJoined, _statDevice;
    private MaterialButton _editBtn, _changePassBtn, _logoutBtn, _pickImageBtn;

    public ProfileFragment(string transitionName, LoginViewModelAnd viewModel
        )
    {
        _transitionName = transitionName;
        LoginVM = viewModel;
    }

    public override View OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        var ctx = Context;
        var scroll = new Android.Widget.ScrollView(ctx);
        var root = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        root.SetPadding(40, 60, 40, 200);

        // --- 1. Header Card (Avatar + Info) ---
        var card = UiBuilder.CreateCard(ctx);
        var cardContent = new LinearLayout(ctx) { Orientation = Orientation.Horizontal, WeightSum = 3 };
        cardContent.SetPadding(30, 30, 30, 30);

        // Avatar Layout
        var avatarLayout = new FrameLayout(ctx);
        avatarLayout.LayoutParameters = new LinearLayout.LayoutParams(180, 180);

        _avatar = new ImageView(ctx);
        _avatar.LayoutParameters = new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
        _avatar.SetBackgroundColor(Color.DarkGray); // Placeholder

        // Pick Image Button overlay
        _pickImageBtn = new MaterialButton(ctx, null, Resource.Attribute.materialIconButtonStyle); // Small icon button style if available
        _pickImageBtn.SetIconResource(Resource.Drawable.album); // Ensure you have a camera icon
        _pickImageBtn.LayoutParameters = new FrameLayout.LayoutParams(60, 60) { Gravity = GravityFlags.Bottom | GravityFlags.Right };
        _pickImageBtn.Click += async (s, e) => await LoginVM.PickImageFromDeviceCommand.ExecuteAsync(null);

        avatarLayout.AddView(_avatar);
        avatarLayout.AddView(_pickImageBtn);

        // Text Info Stack
        var infoStack = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        infoStack.LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 2);
        infoStack.SetPadding(30, 0, 0, 0);

        _username = new TextView(ctx) { TextSize = 22, Typeface = Typeface.DefaultBold };
        _email = new TextView(ctx) { TextSize = 14, Alpha = 0.6f };
        _bio = new TextView(ctx) { TextSize = 14, Top = 20 };
        _bio.SetMaxLines(3);
        _bio.Ellipsize = Android.Text.TextUtils.TruncateAt.End;

        infoStack.AddView(_username);
        infoStack.AddView(_email);
        infoStack.AddView(_bio);

        // Edit Button (Right Side)
        _editBtn = new MaterialButton(ctx) { Text = "Edit" };
        _editBtn.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
        _editBtn.Click += ShowEditBioDialog;

        cardContent.AddView(avatarLayout);
        cardContent.AddView(infoStack);
        // We can add the edit button to the stack or separately. 
        // For simplicity, let's put it below the card in the main flow or inside if layout allows.

        card.AddView(cardContent);
        root.AddView(card);

        // --- 2. Stats Row ---
        root.AddView(UiBuilder.CreateSectionTitle(ctx, "Details"));
        var statsRow = new LinearLayout(ctx) { Orientation = Orientation.Horizontal };
        _statJoined = UiBuilder.CreateStatItem(ctx, "Joined", "Loading...");
        _statDevice = UiBuilder.CreateStatItem(ctx, "Device", "Unknown"); // From WinUI parity
        statsRow.AddView(_statJoined);
        statsRow.AddView(_statDevice);
        root.AddView(statsRow);

        // --- 3. Security Section ---
        root.AddView(UiBuilder.CreateSectionTitle(ctx, "Security"));

        _changePassBtn = new MaterialButton(ctx) { Text = "Change Password" };
        _changePassBtn.SetTextColor(AppUtil.ToColorStateList(UiBuilder.IsDark(Context) ?
            Color.White : Color.DarkSlateBlue));
        _changePassBtn.Click += ShowChangePassDialog;
        root.AddView(_changePassBtn);

        // --- 4. Actions ---
        var space = new Space(ctx) { LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 60) };
        root.AddView(space);

        _logoutBtn = new MaterialButton(ctx) { Text = "Sign Out" };
        _logoutBtn.SetBackgroundColor(Color.DarkRed);
        _logoutBtn.Click += async (s, e) => {
            await LoginVM.LogoutCommand.ExecuteAsync(null);
            // Navigate back to LoginFragment or handle navigation in VM
            ParentFragmentManager.PopBackStack();
        };
        root.AddView(_logoutBtn);

        scroll.AddView(root);
        return scroll;
    }

    public override void OnResume()
    {
        base.OnResume();

        // Observe Changes
        LoginVM.PropertyChanged += OnViewModelPropertyChanged;

        // Initial Update
        if (LoginVM.CurrentUserOnline != null)
            UpdateProfileUI(LoginVM.CurrentUserOnline);
    }

    public override void OnPause()
    {
        base.OnPause();
        _disposables.Clear();
        LoginVM.PropertyChanged -= OnViewModelPropertyChanged;
    }
    private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        Activity?.RunOnUiThread(() =>
        {
            if (e.PropertyName == nameof(LoginViewModel.CurrentUserOnline) ||
                e.PropertyName == nameof(LoginViewModel.CurrentUser))
            {
                if (LoginVM.CurrentUserOnline != null)
                    UpdateProfileUI(LoginVM.CurrentUserOnline);
            }
        });
    }

    private void UpdateProfileUI(UserModelOnline user)
    {
        _username.Text = user.Username;
        _email.Text = user.Email;
        // Assuming Bio isn't in UserModelOnline standard fields yet, mapped from View
        _bio.Text = LoginVM.CurrentUser?.UserBio ?? "No bio set";

        // Load Avatar
        if (!string.IsNullOrEmpty(user.ProfileImagePath)) // Assuming this property exists on your Online model or mapped View
        {
            Glide.With(this).Load(user.ProfileImagePath).CircleCrop().Into(_avatar);
        }
        else
        {
            // Fallback
            _avatar.SetBackgroundColor(Color.DarkGray);
        }

        _statJoined.Text = user.CreatedAt.HasValue ? user.CreatedAt.Value.ToString("MMM yyyy") : "-";

        // Device info isn't standard in ParseUser usually, but if you store it:
        // _statDevice.Text = ...
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