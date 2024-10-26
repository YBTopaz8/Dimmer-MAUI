using InputKit.Shared.Helpers;
using UraniumUI.Extensions;

namespace Dimmer_MAUI.Views.Mobile.CustomViewsM;
[ContentProperty(nameof(Body))]
public partial class NowPlayingBtmSheetContainer : Border, IPageAttachment
{
    public UraniumContentPage AttachedPage { get; protected set; }
    public AttachmentPosition AttachmentPosition => AttachmentPosition.Front;

    public View Body { get; set; }

    public View? HeaderWhenClosed { get; set; }


    private TapGestureRecognizer closeGestureRecognizer = new();
    public void OnAttached(UraniumContentPage page)
    {
        Init();

        AttachedPage = page;
        if (Body != null)
        {
            Body.SizeChanged += (s, e) => AlignBottomSheet(true);
        }
    }
    protected virtual void Init()
    {
        HeaderWhenClosed ??= GenerateAnchor(); // Header when sheet is closed
        HeaderWhenClosed.IsVisible = true;  // Show HeaderWhenClosed

        Padding = 0;
        this.StyleClass = new[] { "BottomSheet" };
        this.VerticalOptions = LayoutOptions.End;
        this.HorizontalOptions = LayoutOptions.Fill;
        this.Body.VerticalOptions = LayoutOptions.End;

        // Initially, show only HeaderWhenClosed and Body
        this.Content = new VerticalStackLayout()
        {
            Children =
            {
                HeaderWhenClosed, // Show HeaderWhenClosed when closed
                Body     // The body content
            }
        };

        if (DeviceInfo.Idiom != DeviceIdiom.Desktop)
        {
            var panGestureRecognizer = new PanGestureRecognizer();
            panGestureRecognizer.PanUpdated += PanGestureRecognizer_PanUpdated;
            HeaderWhenClosed.GestureRecognizers.Add(panGestureRecognizer);
            Body.GestureRecognizers.Add(panGestureRecognizer); // Only attach swipe-to-dismiss to Body
        }
        var tapGestureRecognizer = new TapGestureRecognizer();
        tapGestureRecognizer.Tapped += HeaderWhenClosedTapGestureRecognizer_Tapped; //(s, e) => IsPresented = !IsPresented;
        HeaderWhenClosed.GestureRecognizers.Add(tapGestureRecognizer);

        closeGestureRecognizer.Tapped += (s, e) => IsPresented = false; // Removed tap-to-close on the border

    }


    // Generate the visual for HeaderWhenClosed (when closed)
    protected virtual View GenerateAnchor()
    {
        var anchor = new ContentView
        {
            HorizontalOptions = LayoutOptions.Fill,
            Padding = 10,
            Content = new BoxView
            {
                HeightRequest = 2,
                CornerRadius = 2,
                WidthRequest = 50,
                Color = this.BackgroundColor?.ToSurfaceColor() ?? Microsoft.Maui.Graphics.Colors.Gray,
                HorizontalOptions = LayoutOptions.Center,
            }
        };

        return anchor;
    }

    protected virtual void OnOpened()
    {
        if (CloseOnTapOutside)
        {
            AttachedPage?.ContentFrame?.GestureRecognizers.Add(closeGestureRecognizer);
        }
        IsPresented = true;
        IsPresentedChanged?.Invoke(this, true);
        DeviceDisplay.Current.KeepScreenOn = true;
    }

    protected virtual void OnClosed()
    {
        if (CloseOnTapOutside)
        {
            AttachedPage?.ContentFrame?.GestureRecognizers.Remove(closeGestureRecognizer);
        }
        IsPresented = false;
        IsPresentedChanged?.Invoke(this, false);
        DeviceDisplay.Current.KeepScreenOn = false;
    }

    private bool isVerticalPan = false;

    private async void HeaderWhenClosedTapGestureRecognizer_Tapped(object? sender, TappedEventArgs e)
    {
        // Set the sheet to open and hide the closed header
        IsPresented = true;
        AlignBottomSheet(shouldVibrate: true); // Align the sheet after the gesture is completed or canceled
    }
    private void PanGestureRecognizer_PanUpdated(object sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                isVerticalPan = false;
                break;

            case GestureStatus.Running:
                // If the bottom sheet is fully closed, prevent further downward movement
                if (!IsPresented && e.TotalY > 0) // Prevent downward movement when already closed
                {
                    return;
                }

                if (IsPresented && e.TotalY < 0) // Prevent upward movement when already open
                {
                    return;
                }
                if (Math.Abs(e.TotalY) > Math.Abs(e.TotalX) && !isVerticalPan)
                {
                    isVerticalPan = true; // Mark this as a vertical pan
                }

                if (isVerticalPan)
                {
                    Body.IsVisible = true;
                    var isApple = DeviceInfo.Current.Platform == DevicePlatform.iOS || DeviceInfo.Current.Platform == DevicePlatform.MacCatalyst;

                    // Only update TranslationY if panning is allowed
                    var y = TranslationY + (isApple ? e.TotalY * .05 : e.TotalY);

                    this.TranslationY = y;
                }
                break;

            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                var openThresholdHeight = this.Height * 0.10;
                var closeThresholdHeight = this.Height * 0.10;

                if (isVerticalPan)
                {
                    if (IsPresented)
                    {
                        if (this.TranslationY > closeThresholdHeight)
                        {
                            IsPresented = false;
                        }                        
                    }
                    else
                    {
                        IsPresented = true;
                    }
                }

                AlignBottomSheet(shouldVibrate:true); // Align the sheet after the gesture is completed or canceled
                break;
        }
    }


    // Align and switch headers (HeaderWhenClosed)
    private void AlignBottomSheet(bool animate = true, bool shouldVibrate=false)
    {
        double y;
        double heightRequest;

        if (IsPresented)
        {
            y = 0;  // Opened, bring it to full view
            HeaderWhenClosed.HeightRequest = 0;
            HeaderWhenClosed.Opacity = 0;
            OnOpened();
            this.TranslateToSafely(this.X, y, 250, Easing.CubicInOut);
            Body.HeightRequest = AttachedPage.Height;
            this.HeightRequest = AttachedPage.Height;

            Shell.SetNavBarIsVisible(this.AttachedPage, false);
            Shell.SetTabBarIsVisible(this.AttachedPage, false);
        }
        else
        {
            HeaderWhenClosed.HeightRequest = 65;
            HeaderWhenClosed.Opacity = 1;
            y = this.Height - 65; // Align the bottom sheet to be at the bottom
            OnClosed();
            this.TranslateToSafely(this.X, y, 250, Easing.CubicInOut);

            Shell.SetNavBarIsVisible(this.AttachedPage, false);
            Shell.SetTabBarIsVisible(this.AttachedPage, true);
        }
        if (shouldVibrate)
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);
        }
        UpdateDisabledStateOfPage();
    }

    // Disabling the rest of the page if needed
    protected void UpdateDisabledStateOfPage()
    {
        if (AttachedPage?.Body != null && DisablePageWhenOpened)
        {
            AttachedPage.Body.InputTransparent = IsPresented;

            AttachedPage.Body.FadeToSafely(IsPresented ? .5 : 1);
        }
    }
}


public partial class NowPlayingBtmSheetContainer
{
    // Event to notify when IsPresented has changed
    public event EventHandler<bool> IsPresentedChanged;
    public bool IsPresented
    {
        get => (bool)GetValue(IsPresentedProperty);
        set
        {
            SetValue(IsPresentedProperty, value);
            OnIsPresentedChanged(value);
        }
    }

    // Method to raise the event when IsPresented changes
    protected virtual void OnIsPresentedChanged(bool newValue)
    {
        IsPresentedChanged?.Invoke(this, newValue); // Fire the event with the new value
    }


    public static readonly BindableProperty IsPresentedProperty =
        BindableProperty.Create(
            nameof(IsPresented),
            typeof(bool),
            typeof(NowPlayingBtmSheetContainer),
            defaultValue: false,
            defaultBindingMode: BindingMode.TwoWay,
            propertyChanged: (bo, ov, nv) =>
            {
                (bo as NowPlayingBtmSheetContainer).
                AlignBottomSheet();
            });

    public bool DisablePageWhenOpened { get => (bool)GetValue(DisablePageWhenOpenedProperty); set => SetValue(DisablePageWhenOpenedProperty, value); }

    public static readonly BindableProperty DisablePageWhenOpenedProperty =
        BindableProperty.Create(
            nameof(DisablePageWhenOpened),
            typeof(bool), typeof(NowPlayingBtmSheetContainer), defaultValue: true);

    public bool CloseOnTapOutside { get => (bool)GetValue(CloseOnTapOutsideProperty); set => SetValue(CloseOnTapOutsideProperty, value); }

    public static readonly BindableProperty CloseOnTapOutsideProperty =
        BindableProperty.Create(
            nameof(CloseOnTapOutside),
            typeof(bool), typeof(UraniumUI.Material.Attachments.BackdropView), defaultValue: true);
}