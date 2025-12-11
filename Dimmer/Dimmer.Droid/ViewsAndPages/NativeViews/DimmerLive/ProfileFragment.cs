using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;

using Bumptech.Glide;

using Google.Android.Material.Dialog;

namespace Dimmer.ViewsAndPages.NativeViews.DimmerLive;


public class ProfileFragment : Fragment
{
    private readonly string _transitionName;
    private readonly LoginViewModel LoginVM;
    private readonly CompositeDisposable _disposables = new();

    private ImageView _avatar;
    private TextView _username, _email, _bio;
    private TextView _statBackup, _statJoined; // Stats

    public ProfileFragment(string transitionName, LoginViewModel viewModel)
    {
        _transitionName = transitionName;
        LoginVM = viewModel;
    }

    public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
    {
        var ctx = Context;
        var scroll = new Android.Widget.ScrollView(ctx);
        var root = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        root.SetPadding(40, 60, 40, 200);

        // Header Card
        var card = AppUtil.CreateCard(ctx);
        var cardContent = new LinearLayout(ctx) { Orientation = Orientation.Horizontal, WeightSum = 3 };
        cardContent.SetPadding(30, 30, 30, 30);

        _avatar = new ImageView(ctx);
        _avatar.LayoutParameters = new LinearLayout.LayoutParams(150, 150);

        var infoStack = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        infoStack.LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 2);
        infoStack.SetPadding(20, 0, 0, 0);

        _username = new TextView(ctx) { TextSize = 22, Typeface = Typeface.DefaultBold };
        _email = new TextView(ctx) { TextSize = 14 };
        _bio = new TextView(ctx) { TextSize = 12 };

        infoStack.AddView(_username);
        infoStack.AddView(_email);
        infoStack.AddView(_bio);

        cardContent.AddView(_avatar);
        cardContent.AddView(infoStack);
        card.AddView(cardContent);
        root.AddView(card);

        // Edit Button
        var editBtn = new MaterialButton(ctx) { Text = "Edit Profile", Icon = Context.GetDrawable(Resource.Drawable.material_ic_edit_black_24dp) }; // Use your icon
        editBtn.Click += ShowEditDialog;
        root.AddView(editBtn);

        // Stats Row
        root.AddView(AppUtil.CreateSectionTitle(ctx, "Statistics"));
        var statsRow = new LinearLayout(ctx) { Orientation = Orientation.Horizontal };
        _statJoined = AppUtil.CreateStatItem(ctx, "Joined", "...");
        _statBackup = AppUtil.CreateStatItem(ctx, "Last Backup", "...");
        statsRow.AddView(_statJoined);
        statsRow.AddView(_statBackup);
        root.AddView(statsRow);

        // Change Password
        root.AddView(AppUtil.CreateSectionTitle(ctx, "Security"));
        var passBtn = new MaterialButton(ctx) { Text = "Change Password" };
        passBtn.Click += ShowChangePassDialog;
        root.AddView(passBtn);

        // Logout
        var logoutBtn = new MaterialButton(ctx) { Text = "Sign Out" };
        logoutBtn.SetBackgroundColor(Color.DarkRed);
        logoutBtn.Click += async (s, e) => await LoginVM.LogoutCommand.ExecuteAsync(null);
        root.AddView(logoutBtn);

        scroll.AddView(root);
        return scroll;
    }

    public override void OnResume()
    {
        base.OnResume();

        LoginVM.PropertyChanged += OnViewModelPropertyChanged;
        UpdateProfileUI();
    }

    public override void OnPause()
    {
        base.OnPause(); _disposables.Clear();
        LoginVM.PropertyChanged -= OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        RxSchedulers.UI.Schedule(() =>
        {
            if (e.PropertyName == nameof(LoginViewModel.CurrentUser) ||
                e.PropertyName == nameof(LoginViewModel.CurrentUserOnline))
            {
                UpdateProfileUI();
            }
            // Add case for LatestBackup if you implemented that property in VM
        });
    }

    private void UpdateProfileUI()
    {
        var user = LoginVM.CurrentUserOnline; // Or CurrentUser depending on your VM
        if (user == null) return;

        _username.Text = user.Username;
        _email.Text = user.Email;
        _bio.Text = user.Bio ?? "No bio set";

        if (!string.IsNullOrEmpty(user.ProfileImagePath))
        {
            Glide.With(this).Load(user.ProfileImagePath).CircleCrop().Into(_avatar);
        }

        _statJoined.Text = user.CreatedAt.HasValue ? user.CreatedAt.Value.ToString("MMM yyyy") : "-";

        // Handle Backup Stats if property exists
        // _statBackup.Text = ...
    }


    private void ShowEditDialog(object? sender, EventArgs e)
    {
        var ctx = Context;
        var dialogView = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        dialogView.SetPadding(50, 50, 50, 50);

        var editBio = new TextInputEditText(ctx) { Hint = "Bio", Text = LoginVM.CurrentUser.UserBio };
        dialogView.AddView(editBio);

        new MaterialAlertDialogBuilder(ctx)
            .SetTitle("Edit Profile")
            .SetView(dialogView)
            .SetPositiveButton("Save", async (s, a) => {
                LoginVM.CurrentUser.UserBio = editBio.Text;
                await LoginVM.SaveProfileChangesCommand.ExecuteAsync(null);
            })
            .SetNegativeButton("Cancel", (s, a) => { })
            .Show();
    }

    private void ShowChangePassDialog(object sender, EventArgs e)
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
                await LoginVM.ChangePasswordCommand.ExecuteAsync(editPass.Text);
            })
            .SetNegativeButton("Cancel", (s, a) => { })
            .Show();
    }

}