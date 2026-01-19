using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.ViewsAndPages.NativeViews.DimmerLive.LastFMViews;

using System.Reactive.Disposables;

using Dimmer.ViewModel;
using Dimmer.WinUI.UiUtils;
using ProgressBar = Android.Widget.ProgressBar;


public class LastFMLoginFragment : Fragment
{
    private readonly string _transitionName;
    private readonly BaseViewModelAnd MyViewModel;

    public SettingsViewModel? settingsVM { get; }
 
    private readonly CompositeDisposable _disposables = new();

    // UI Elements
    private TextInputLayout _userLayout;
    TextInputEditText _userEdit;
    private MaterialButton _actionBtn;
    private TextView _errorText;
    private ProgressBar _progressBar;

    public LastFMLoginFragment(string transitionName, BaseViewModelAnd baseViewModel)
    {
        _transitionName = transitionName;
        MyViewModel = baseViewModel;

        settingsVM = MainApplication.ServiceProvider.GetService<SettingsViewModel>();
    }

    public override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
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
        _userLayout = CreateInput(ctx, "Last FM Username");
        _userLayout.HintTextColor = UiBuilder.IsDark(ctx) ? AppUtil.ToColorStateList(Color.Red ): AppUtil.ToColorStateList(Color.White);
        _userEdit = (TextInputEditText)_userLayout.EditText!;

       

        // Button
        _actionBtn = new MaterialButton(ctx)
        {
            Text = "Authorize Login",
            LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        };

        _actionBtn.SetIconResource(Resource.Drawable.lastfmsmoll);
        _actionBtn.SetBackgroundColor(Color.ParseColor("#D51007"));
_actionBtn.SetTextColor(Color.White);
        ((LinearLayout.LayoutParams)_actionBtn.LayoutParameters).SetMargins(0, 30, 0, 0);

      
        _progressBar = new ProgressBar(ctx) { Indeterminate = true, Visibility = ViewStates.Gone };

        centerStack.AddView(title);
        centerStack.AddView(subTitle);
        centerStack.AddView(_errorText);
        centerStack.AddView(_userLayout);
        centerStack.AddView(_actionBtn);
        centerStack.AddView(_progressBar);

        root.AddView(centerStack);
        return root;
    }

    public async override void OnResume()
    {
        base.OnResume();
        settingsVM.IsBusy = true;

       
        
        // 1. Two-way binding for Inputs

        settingsVM.IsBusy = false;
        _userEdit.TextChanged += UserEdit_TextChanged;
     
        _actionBtn.Click += ActionBtn_Click;


        // 3. Reactive UI Bindings (State -> UI)
        settingsVM.PropertyChanged += OnViewModelPropertyChanged;

    }

    private void UserEdit_TextChanged(object? sender, Android.Text.TextChangedEventArgs e)
    {
        if(e is not null && e.Text is not null)
            BaseViewModel.LastFMName = e.Text.ToString()!;
    }

    public override void OnPause()
    {
        base.OnPause();
        _disposables.Clear();
        // Unsubscribe to prevent memory leaks
        _userEdit.TextChanged -= UserEdit_TextChanged;
        _actionBtn.Click -= ActionBtn_Click;
        settingsVM.PropertyChanged -= OnViewModelPropertyChanged;
    }


    private async void ActionBtn_Click(object? sender, EventArgs e)
    {
        BaseViewModel.LastFMName = string.IsNullOrEmpty(_userEdit.Text) ? string.Empty : _userEdit.Text ;

        await MyViewModel.LoginToLastfm();
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
        
        _actionBtn.Text = "Authorize";

        // 2. Busy State
        bool isBusy = settingsVM.IsBusy;
        _progressBar.Visibility = isBusy ? ViewStates.Visible : ViewStates.Gone;
        _actionBtn.Enabled = !isBusy;
        _userEdit.Enabled = !isBusy;
        string err = settingsVM.ErrorMessage;
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