using System.Reactive.Disposables;

using ProgressBar = Android.Widget.ProgressBar;

namespace Dimmer.ViewsAndPages.NativeViews.DimmerLive;

public class LoginFragment : Fragment
{
    private readonly string _transitionName;
    private readonly BaseViewModelAnd _baseViewModel;
    public LoginViewModelAnd loginViewModel { get; private set; } // Property for binding

    private readonly CompositeDisposable _disposables = new();

    // UI Elements
    private TextInputLayout _userLayout, _passLayout, _emailLayout;
    private TextInputEditText _userEdit, _passEdit, _emailEdit;
    private MaterialButton _actionBtn, _toggleBtn;
    private TextView _errorText;
    private ProgressBar _progressBar;

    public LoginFragment(string transitionName, BaseViewModelAnd baseViewModel)
    {
        _transitionName = transitionName;
        _baseViewModel = baseViewModel;
    }

    public override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        // Resolve ViewModel here if not passed in ctor, assuming DI setup
        loginViewModel = MainApplication.ServiceProvider.GetRequiredService<LoginViewModelAnd>();
    }

    public override View OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        var ctx = Context;

        // Gradient Background
        var gradient = new GradientDrawable(GradientDrawable.Orientation.TlBr, new int[] { Color.ParseColor("#1E1E1E"), Color.ParseColor("#2D2D30") });

        var root = new RelativeLayout(ctx)
        {
            LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent),
            Background = gradient
        };

        var centerStack = new LinearLayout(ctx)
        {
            Orientation = Orientation.Vertical,
            LayoutParameters = new RelativeLayout.LayoutParams(AppUtil.DpToPx(360), ViewGroup.LayoutParams.WrapContent)
        };
        ((RelativeLayout.LayoutParams)centerStack.LayoutParameters).AddRule(LayoutRules.CenterInParent);
        centerStack.SetPadding(20, 20, 20, 20);

        // Title
        var title = new TextView(ctx) { Text = "Dimmer", TextSize = 32, Gravity = GravityFlags.Center, Typeface = Typeface.DefaultBold };
        title.SetTextColor(Color.White);
        var subTitle = new TextView(ctx) { Text = "Sync. Chat. Listen.", TextSize = 16, Gravity = GravityFlags.Center, Alpha = 0.7f };
        subTitle.SetTextColor(Color.White);
        subTitle.SetPadding(0, 0, 0, 40);

        // Error Banner
        _errorText = new TextView(ctx) { Visibility = ViewStates.Gone, Gravity = GravityFlags.Center };
        _errorText.SetTextColor(Color.Red);
        _errorText.SetPadding(0, 0, 0, 20);

        // Inputs
        _userLayout = CreateInput(ctx, "Username");
        _userEdit = (TextInputEditText)_userLayout.EditText!;

        _passLayout = CreateInput(ctx, "Password", true);
        _passEdit = (TextInputEditText)_passLayout.EditText!;
        
        _emailLayout = CreateInput(ctx, "Email"); // Initially hidden
        _emailEdit = (TextInputEditText)_emailLayout.EditText!;
        _emailLayout.Visibility = ViewStates.Gone;

        // Button
        _actionBtn = new MaterialButton(ctx)
        {
            Text = "Log In",
            LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        };

        _actionBtn.SetBackgroundColor(Android.Graphics.Color.DarkSlateBlue);
        _actionBtn.SetTextColor(Color.White);
        ((LinearLayout.LayoutParams)_actionBtn.LayoutParameters).SetMargins(0, 30, 0, 0);

        // Toggle Text
        _toggleBtn = new MaterialButton(ctx) { Text = "Don't have an account? Sign Up" };
        _toggleBtn.SetBackgroundColor(Color.Transparent);
        _toggleBtn.SetTextColor(Color.White);
        _toggleBtn.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent) { Gravity = GravityFlags.Center };

        _progressBar = new ProgressBar(ctx) { Indeterminate = true, Visibility = ViewStates.Gone };

        centerStack.AddView(title);
        centerStack.AddView(subTitle);
        centerStack.AddView(_errorText);
        centerStack.AddView(_userLayout);
        centerStack.AddView(_emailLayout);
        centerStack.AddView(_passLayout);
        centerStack.AddView(_actionBtn);
        centerStack.AddView(_toggleBtn);
        centerStack.AddView(_progressBar);

        root.AddView(centerStack);
        return root;
    }

    public async override void OnResume()
    {
        base.OnResume();
        loginViewModel.IsBusy = true;
        await loginViewModel.InitAsync();

        SessionManagementViewModel sessionMgtVM= MainApplication.ServiceProvider.GetRequiredService<SessionManagementViewModel>();
         
        if(loginViewModel.NavigateToCloudPage(this, new CloudDataFragment(_transitionName, sessionMgtVM), "CloudDataFragment"))
        {
            loginViewModel.IsBusy = false;
            return;
        }
        // 1. Two-way binding for Inputs

        loginViewModel.IsBusy = false;
        _userEdit.TextChanged += UserEdit_TextChanged;
        _passEdit.TextChanged += PassEdit_TextChanged;
        _emailEdit.TextChanged += EmailEdit_TextChanged;
        // 2. Bind Commands

        _toggleBtn.Click += ToggleBtn_Click;
        _actionBtn.Click += ActionBtn_Click;
       

        // 3. Reactive UI Bindings (State -> UI)
        loginViewModel.PropertyChanged += OnViewModelPropertyChanged;

    }

    public override void OnPause()
    {
        base.OnPause();
        _disposables.Clear();
        // Unsubscribe to prevent memory leaks
        _userEdit.TextChanged -= UserEdit_TextChanged;
        _passEdit.TextChanged -= PassEdit_TextChanged;
        _emailEdit.TextChanged -= EmailEdit_TextChanged;
        _toggleBtn.Click -= ToggleBtn_Click;
        _actionBtn.Click -= ActionBtn_Click;
        loginViewModel.PropertyChanged -= OnViewModelPropertyChanged;
    }

    private void UserEdit_TextChanged(object? sender, Android.Text.TextChangedEventArgs e) => loginViewModel.Username = e.Text.ToString();
    private void PassEdit_TextChanged(object? sender, Android.Text.TextChangedEventArgs e) => loginViewModel.Password = e.Text.ToString();
    private void EmailEdit_TextChanged(object? sender, Android.Text.TextChangedEventArgs e) => loginViewModel.Email = e.Text.ToString();

    private void ToggleBtn_Click(object? sender, EventArgs e) => loginViewModel.ToggleModeCommand.Execute(null);

    private async void ActionBtn_Click(object? sender, EventArgs e)
    {
        if (loginViewModel.IsRegisterMode)
            await loginViewModel.RegisterCommand.ExecuteAsync(null);
        else
        {
            await loginViewModel.LoginCommand.ExecuteAsync(null);
            if(loginViewModel.CurrentUserOnline is not null && loginViewModel.CurrentUserOnline.IsAuthenticated)
            {
                loginViewModel.NavigateToProfilePage(this, new ProfileFragment(_transitionName, loginViewModel), "ProfileFragment");
            }
        }

    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // UI updates must happen on the Main Thread
        Activity?.RunOnUiThread(() =>
        {
            switch (e.PropertyName)
            {
                case nameof(LoginViewModel.IsRegisterMode):
                case nameof(LoginViewModel.IsBusy):
                case nameof(LoginViewModel.ErrorMessage):
                    UpdateUiState();
                    break;
            }
        });
    }

    private void UpdateUiState()
    {
        // 1. Toggle Mode
        bool isRegister = loginViewModel.IsRegisterMode;
        _emailLayout.Visibility = isRegister ? ViewStates.Visible : ViewStates.Gone;
        _actionBtn.Text = isRegister ? "Sign Up" : "Log In";
        _toggleBtn.Text = loginViewModel.ToggleText;

        // 2. Busy State
        bool isBusy = loginViewModel.IsBusy;
        _progressBar.Visibility = isBusy ? ViewStates.Visible : ViewStates.Gone;
        _actionBtn.Enabled = !isBusy;
        _userEdit.Enabled = !isBusy;
        _passEdit.Enabled = !isBusy;

        // 3. Error Message
        var err = loginViewModel.ErrorMessage;
        _errorText.Text = err;
        _errorText.Visibility = string.IsNullOrEmpty(err) ? ViewStates.Gone : ViewStates.Visible;
    }

 

    private TextInputLayout CreateInput(Context ctx, string hint, bool isPassword = false)
    {
        var layout = new TextInputLayout(ctx)
        {
            Hint = hint,
            LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        };
        var edit = new TextInputEditText(ctx);
        if (isPassword) edit.InputType = Android.Text.InputTypes.TextVariationPassword | Android.Text.InputTypes.ClassText;
        
        layout.AddView(edit);
        return layout;
    }
}