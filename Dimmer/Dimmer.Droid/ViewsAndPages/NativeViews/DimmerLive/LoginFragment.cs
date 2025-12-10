using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;

using ProgressBar = Android.Widget.ProgressBar;

namespace Dimmer.ViewsAndPages.NativeViews.DimmerLive;

public class LoginFragment : Fragment, IOnBackInvokedCallback
{
    private readonly string _transitionName;
    private readonly BaseViewModelAnd _viewModel;
    private readonly CompositeDisposable _disposables = new();

    // UI
    private TextInputLayout _userLayout, _passLayout, _emailLayout;
    private TextInputEditText _userEdit, _passEdit, _emailEdit;
    private MaterialButton _actionBtn, _toggleBtn;
    private TextView _errorText;
    private ProgressBar _progressBar;
    private LinearLayout _registerContainer;

    public LoginFragment(string transitionName, BaseViewModelAnd viewModel)
    {
        _transitionName = transitionName;
        _viewModel = viewModel;
    }

    public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
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
        _userEdit = (TextInputEditText)_userLayout.EditText;

        _passLayout = CreateInput(ctx, "Password", true);
        _passEdit = (TextInputEditText)_passLayout.EditText;

        _emailLayout = CreateInput(ctx, "Email"); // Initially hidden
        _emailEdit = (TextInputEditText)_emailLayout.EditText;
        _emailLayout.Visibility = ViewStates.Gone;

        // Button
        _actionBtn = new MaterialButton(ctx) { Text = "Log In" };
        _actionBtn.SetBackgroundColor(Color.ParseColor("#6200EE"));
        ((LinearLayout.LayoutParams)_actionBtn.LayoutParameters).SetMargins(0, 30, 0, 0);

        // Toggle Text
        _toggleBtn = new MaterialButton(ctx) { Text = "Don't have an account? Sign Up" };
        //_toggleBtn.Style = Resource.Style.Widget_MaterialComponents_Button_TextButton; // Requires Theme, doing manual fallback
        _toggleBtn.SetBackgroundColor(Color.Transparent);
        _toggleBtn.SetTextColor(Color.White);

        _progressBar = new ProgressBar(ctx) { Indeterminate = true, Visibility = ViewStates.Gone };

        centerStack.AddView(title);
        centerStack.AddView(subTitle);
        centerStack.AddView(_errorText);
        centerStack.AddView(_userLayout);
        centerStack.AddView(_emailLayout); // Hidden by default
        centerStack.AddView(_passLayout);
        centerStack.AddView(_actionBtn);
        centerStack.AddView(_toggleBtn);
        centerStack.AddView(_progressBar);

        root.AddView(centerStack);
        return root;
    }

    public override void OnResume()
    {
        base.OnResume();

        // Bind Inputs
        //_userEdit.TextChanged += (s, e) => _viewModel.LoginVM.Username = e.Text.ToString();
        //_passEdit.TextChanged += (s, e) => _viewModel.LoginVM.Password = e.Text.ToString();
        //_emailEdit.TextChanged += (s, e) => _viewModel.LoginVM.Email = e.Text.ToString();

        //// Bind Toggle Logic
        //_toggleBtn.Click += (s, e) => _viewModel.LoginVM.ToggleModeCommand.Execute(null);

        //_viewModel.LoginVM.WhenAnyValue(vm => vm.IsRegisterMode)
        //    .ObserveOn(RxSchedulers.UI)
        //    .Subscribe(reg =>
        //    {
        //        _emailLayout.Visibility = reg ? ViewStates.Visible : ViewStates.Gone;
        //        _actionBtn.Text = reg ? "Sign Up" : "Log In";
        //        _toggleBtn.Text = reg ? "Already have an account? Log In" : "Don't have an account? Sign Up";
        //    })
        //    .DisposeWith(_disposables);

        //// Bind Action Button
        //_actionBtn.Click += async (s, e) =>
        //{
        //    if (_viewModel.LoginVM.IsRegisterMode)
        //        await _viewModel.LoginVM.RegisterCommand.ExecuteAsync(null);
        //    else
        //        await _viewModel.LoginVM.LoginCommand.ExecuteAsync(null);
        //};

        //// Bind Error & Busy
        //_viewModel.LoginVM.WhenAnyValue(vm => vm.ErrorMessage)
        //    .ObserveOn(RxSchedulers.UI)
        //    .Subscribe(err =>
        //    {
        //        _errorText.Text = err;
        //        _errorText.Visibility = string.IsNullOrEmpty(err) ? ViewStates.Gone : ViewStates.Visible;
        //    })
        //    .DisposeWith(_disposables);

        //_viewModel.LoginVM.WhenAnyValue(vm => vm.IsBusy)
        //    .ObserveOn(RxSchedulers.UI)
        //    .Subscribe(busy =>
        //    {
        //        _progressBar.Visibility = busy ? ViewStates.Visible : ViewStates.Gone;
        //        _actionBtn.Enabled = !busy;
        //    })
        //    .DisposeWith(_disposables);
    }

    public override void OnPause()
    {
        base.OnPause();
        _disposables.Clear();
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

    public void OnBackInvoked() { }
}