using System.Threading.Tasks;

using AndroidX.Fragment.App;

using Bumptech.Glide;

using Google.Android.Material.Button;
using Google.Android.Material.Card;
using Google.Android.Material.Carousel;
using Google.Android.Material.Chip;
using Google.Android.Material.Dialog;
using Google.Android.Material.TextView;

using DialogFragment = AndroidX.Fragment.App.DialogFragment;
using ScrollView = Android.Widget.ScrollView;
using Slider = Google.Android.Material.Slider.Slider;
using TimePicker = Android.Widget.TimePicker;

namespace Dimmer.ViewsAndPages.NativeViews;

public partial class NowPlayingFragment : Fragment, IOnBackInvokedCallback
{
    FloatingActionButton fabMD3;
    private FrameLayout root;
    public TextView LyricsPlaceholder;

    public BaseViewModelAnd MyViewModel { get; private set; } = null!;
    public MaterialTextView SongTitle { get; private set; }
    public ImageView CoverImageView { get; private set; }
    public MaterialTextView ArtistName { get; private set; }
    public ImageView FavIcon { get; private set; }
    public MaterialTextView AlbumName { get; private set; }
    public MaterialTextView PlayCount { get; private set; }
    public MaterialTextView FileFormatText { get; private set; }
    public Slider SeekSlider { get; private set; }
    public Slider VolumeSlider { get; private set; }
    public bool IsDragging { get; private set; } = false;
    public string ArtTransitionName { get; set; }
    public string TitleTransitionName { get; set; }
    public string ArtistTransitionName { get; set; }
    public string AlbumTransitionName { get; set; }
    public NowPlayingFragment()
    {
        
    }

    public NowPlayingFragment(BaseViewModelAnd viewModel)
    {
        MyViewModel = viewModel;
    }
    public override View OnCreateView(LayoutInflater? inflater, ViewGroup? container, Bundle? savedInstanceState)
    {


        var ctx = Context!;
        // 1. MAIN SCROLL VIEW (Vertical)
        var mainScrollView = new ScrollView(ctx)
        {
            LayoutParameters = new ViewGroup.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent),
            FillViewport = true
        };

        // 2. MAIN CONTAINER (Vertical Stack)
        var mainStack = new LinearLayout(ctx)
        {
            Orientation = Orientation.Vertical,
            LayoutParameters = new FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent)
        };
        mainStack.SetPadding(DpToPx(20), DpToPx(20), DpToPx(20), DpToPx(20));

        // --- ELEMENT 1: Song Title ---
        SongTitle = new MaterialTextView(ctx)
        {
            Text = MyViewModel.CurrentPlayingSongView.Title,
            TextSize = 24f,
            Gravity = GravityFlags.Center,
            LayoutParameters = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent)
        };
        ((LinearLayout.LayoutParams)SongTitle.LayoutParameters).BottomMargin = DpToPx(16);
        if (!string.IsNullOrEmpty(TitleTransitionName))
            SongTitle.TransitionName = TitleTransitionName;

        // --- ELEMENT 2: CardView with Image + Overlay ---
        var albumCard = new MaterialCardView(ctx)
        {
            Radius = DpToPx(24),
            CardElevation = DpToPx(8),
            LayoutParameters = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                DpToPx(350)) // Fixed Height 350dp
        };
        albumCard.SetBackgroundColor(Color.Transparent);
        ((LinearLayout.LayoutParams)albumCard.LayoutParameters).BottomMargin = DpToPx(24);

        // Internal Frame to stack Image and Lyrics
        var cardInternalFrame = new FrameLayout(ctx)
        {
            LayoutParameters = new ViewGroup.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent)
        };

        // Layer A: Album Art
        CoverImageView = new ImageView(ctx)
        {
            LayoutParameters = new FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent)
        };
        CoverImageView.SetScaleType(ImageView.ScaleType.CenterCrop);

        var lyParams =
            new FrameLayout.LayoutParams(
                AppUtil.DpToPx(200),
                ViewGroup.LayoutParams.MatchParent);
        lyParams.SetMargins(4, 4, 4, 4);
        var carouselMaskedFramelayout = new MaskableFrameLayout(ctx)
        {
            LayoutParameters = lyParams

        };
        carouselMaskedFramelayout.ShapeAppearanceModel = carouselMaskedFramelayout.ShapeAppearanceModel.ToBuilder()
            .SetAllCornerSizes(AppUtil.DpToPx(10))
            .Build();
        carouselMaskedFramelayout.AddView(CoverImageView);

        if (!string.IsNullOrEmpty(MyViewModel.CurrentPlayingSongView.CoverImagePath) && System.IO.File.Exists(MyViewModel.CurrentPlayingSongView.CoverImagePath))
        {
            Glide.With(this)
                .Load(MyViewModel.CurrentPlayingSongView.CoverImagePath)
                .Placeholder(Resource.Drawable.musicnotess)
                .Into(CoverImageView);
            // Load from disk
           
        }

        if (!string.IsNullOrEmpty(ArtTransitionName))
            CoverImageView.TransitionName = ArtTransitionName;

        

        // Add a sample text to the overlay to show it works
         LyricsPlaceholder = new TextView(ctx)
        {
            Text = "Lyrics will appear here...",
            Gravity = GravityFlags.Center,
            LayoutParameters = new FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.WrapContent,
                ViewGroup.LayoutParams.WrapContent,
                GravityFlags.Center)
        };
        LyricsPlaceholder.SetTextColor(Color.White);
        //LyricsOverlay.AddView(LyricsPlaceholder);

        cardInternalFrame.AddView(carouselMaskedFramelayout);
        albumCard.AddView(cardInternalFrame);


        // --- ELEMENT 3: Artist + Fav Icon (Row) ---
        var artistRow = new LinearLayout(ctx)
        {
            Orientation = Orientation.Horizontal,
            LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        };

        ArtistName = new MaterialTextView(ctx)
        {
            Text = MyViewModel.CurrentPlayingSongView.ArtistName,
            TextSize = 18f,
            LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1f) // Weight 1
        };
        ArtistName.Click += ArtistName_Click;


        if (!string.IsNullOrEmpty(ArtistTransitionName))
            ArtistName.TransitionName = ArtistTransitionName;


        FavIcon = new ImageView(ctx)
        {
            LayoutParameters = new LinearLayout.LayoutParams(DpToPx(24), DpToPx(24))
        };
        FavIcon.SetImageResource(Resource.Drawable.heart); // Replace with your Heart icon
        FavIcon.ImageTintList = ColorStateList.ValueOf(Color.DarkSlateBlue);

        artistRow.AddView(ArtistName);
        artistRow.AddView(FavIcon);


        // --- ELEMENT 4: Album + Play Count (Row) ---
        var albumRow = new LinearLayout(ctx)
        {
            Orientation = Orientation.Horizontal,
            LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        };
        ((LinearLayout.LayoutParams)albumRow.LayoutParameters).TopMargin = DpToPx(8);

        AlbumName = new MaterialTextView(ctx)
        {
            Text = MyViewModel.CurrentPlayingSongView.AlbumName,
            TextSize = 14f,
            LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1f)
        };
        AlbumName.Click += AlbumName_Click;

        AlbumName.SetTextColor(Color.Gray);

        PlayCount = new MaterialTextView(ctx)
        {
            Text = $"Plays: {MyViewModel.CurrentPlayingSongView.PlayCompletedCount}",
            TextSize = 12f,
            LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent)
        };

        albumRow.AddView(AlbumName);
        albumRow.AddView(PlayCount);


        // --- ELEMENT 5: 25% Btn | 75% Btn ---
        var rowBtnA = new LinearLayout(ctx)
        {
            Orientation = Orientation.Horizontal,
            LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        };
        ((LinearLayout.LayoutParams)rowBtnA.LayoutParameters).TopMargin = DpToPx(20);

        var btn1_25 = new MaterialButton(ctx)
        {
            Text = "Prev",
            LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 0.25f) // 25%
        };
        btn1_25.Click += async (s, e) =>
        {
            await MyViewModel.PreviousTrackASync();
        };

        var playBtn_75 = new MaterialButton(ctx)
        {
            Text = MyViewModel.IsDimmerPlaying ? "Pause" : "Play",
            LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 0.75f) // 75%
        };
        playBtn_75.Click += async (s, e) =>
        {
            await MyViewModel.PlayPauseToggleAsync();

            await Task.Delay(100);
            if (!MyViewModel.IsDimmerPlaying)
            {
         
                playBtn_75.Text = "Play";
            }
            else
            {
                playBtn_75.Text = "Pause";
            }
        };

        // Add margin between
        ((LinearLayout.LayoutParams)playBtn_75.LayoutParameters).LeftMargin = DpToPx(8);

        rowBtnA.AddView(btn1_25);
        rowBtnA.AddView(playBtn_75);


        // --- ELEMENT 6: 75% Btn | 25% Btn ---
        var rowBtnB = new LinearLayout(ctx)
        {
            Orientation = Orientation.Horizontal,
            LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        };
        ((LinearLayout.LayoutParams)rowBtnB.LayoutParameters).TopMargin = DpToPx(8);

        SeekSlider = new Slider(ctx)
        {
            ValueFrom = 0f,
            ValueTo = 100f,
            Value = 30f,
            LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 0.75f)
        };



        var btn2_25 = new MaterialButton(ctx)
        {
            Text = "Next",
            LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 0.25f) // 25%
        };
        ((LinearLayout.LayoutParams)btn2_25.LayoutParameters).LeftMargin = DpToPx(8);
        btn2_25.Click += async (s, e) =>
        {
            await MyViewModel.NextTrackAsync();
        };
        rowBtnB.AddView(SeekSlider);
        rowBtnB.AddView(btn2_25);


        // --- ELEMENT 7: File Format Text ---
        FileFormatText = new MaterialTextView(ctx)
        {
            Text = $"{MyViewModel.CurrentPlayingSongView.FileFormat} - {MyViewModel.CurrentPlayingSongView.SampleRate} Hz",
            TextSize = 12f,
            Gravity = GravityFlags.Center,
            LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        };
        FileFormatText.SetTextColor(Color.DarkGray);
        ((LinearLayout.LayoutParams)FileFormatText.LayoutParameters).TopMargin = DpToPx(16);

        
        // --- ELEMENT 8: Seek Slider ---
        SeekSlider.Touch += (sender, e) =>
        {
            // 1. IMPORTANT: Set Handled to false so the Slider still slides!
            e.Handled = false;

            if (e.Event?.Action == MotionEventActions.Down)
            {
                IsDragging = true; // User grabbed the handle
            }
            else if (e.Event?.Action == MotionEventActions.Up || e.Event?.Action == MotionEventActions.Cancel)
            {
                IsDragging = false; // User let go
                if (sender is Slider s)
                {
                    OnSeekCompleted(s.Value);
                }
            }
        };
        // --- ELEMENT 9: Volume Row (10% | 80% | 10%) ---
        var volRow = new LinearLayout(ctx)
        {
            Orientation = Orientation.Horizontal,
            LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        };
        ((LinearLayout.LayoutParams)volRow.LayoutParameters).TopMargin = DpToPx(8);
        volRow.SetGravity(GravityFlags.CenterVertical);

        var volDownImg = new ImageView(ctx) { LayoutParameters = new LinearLayout.LayoutParams(0, DpToPx(24), 0.1f) };
        volDownImg.SetImageResource(Android.Resource.Drawable.IcLockSilentModeOff); // Replace with vol- icon

        VolumeSlider = new  Slider(ctx)
        {
            ValueFrom = 0f,
            ValueTo = 1f,
            StepSize =0.1f,
            Value = (float)MyViewModel.DeviceVolumeLevel,
            LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 0.8f)
        };

        VolumeSlider.Touch += VolumeSlider_Touch;

        var volUpImg = new ImageView(ctx) { LayoutParameters = new LinearLayout.LayoutParams(0, DpToPx(24), 0.1f) };
        volUpImg.SetImageResource(Android.Resource.Drawable.IcLockSilentModeOff); // Replace with vol+ icon

        volRow.AddView(volDownImg);
        volRow.AddView(VolumeSlider);
        volRow.AddView(volUpImg);


        // --- ELEMENT 10: Right Aligned Chips (View Queue, Sleep) ---
        var chipsLayout = new LinearLayout(ctx)
        {
            Orientation = Orientation.Horizontal,
            LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        };
        chipsLayout.SetGravity(GravityFlags.End); // Push to Right
        ((LinearLayout.LayoutParams)chipsLayout.LayoutParameters).TopMargin = DpToPx(16);
        ((LinearLayout.LayoutParams)chipsLayout.LayoutParameters).BottomMargin = DpToPx(40); // Bottom padding for scroll

        var queueChip = new Chip(ctx) { Text = "View Queue" };
        queueChip.SetEnsureMinTouchTargetSize(false); // Compact style

        queueChip.Click += QueueChip_Click;


        var sleepChip = new Chip(ctx) { Text = "Sleep Timer" };
        sleepChip.Touch += SleepChip_Touch;
        sleepChip.SetEnsureMinTouchTargetSize(false);
        var sleepChipLyParams = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
        sleepChipLyParams.LeftMargin = DpToPx(8);
        sleepChip.LayoutParameters = sleepChipLyParams;

        chipsLayout.AddView(queueChip);
        chipsLayout.AddView(sleepChip);


        // --- BUILD TREE ---
        mainStack.AddView(SongTitle);
        mainStack.AddView(albumCard);
        mainStack.AddView(artistRow);
        mainStack.AddView(albumRow);
        mainStack.AddView(rowBtnA);
        mainStack.AddView(rowBtnB);
        mainStack.AddView(FileFormatText);
        mainStack.AddView(volRow);
        mainStack.AddView(chipsLayout);

        mainScrollView.AddView(mainStack);

        return mainScrollView;
    }

    private void AlbumName_Click(object? sender, EventArgs e)
    {
        throw new NotImplementedException();
    }

    private void ArtistName_Click(object? sender, EventArgs e)
    {
        throw new NotImplementedException();
    }

    private async void QueueChip_Click(object? sender, EventArgs e)
    {
        // programatically press back key
        var parentActivity = Activity as TransitionActivity;
        if (parentActivity != null)
        {
            parentActivity.OnBackPressedDispatcher.OnBackPressed();
var isHomePageType = MyViewModel.CurrentPage is HomePageFragment;
            if (isHomePageType)
            {
                var appHomePage = (HomePageFragment)MyViewModel.CurrentPage!;
                await Task.Delay(400);
                appHomePage.PageFAB_Click(this, EventArgs.Empty);
            }
        }


    }
    
    private void SleepChip_Touch(object? sender, View.TouchEventArgs e)
    {
        //var popUpRequestingUserToChooseTimeToSetTimerForSleep = new dialog
        DialogFragment sleepDiagFrag = new SleepTimerDialogFragment(MyViewModel);
        
        

        sleepDiagFrag.Show(ParentFragmentManager, "SleepTimerDialog");
        
        //MyViewModel.SetSleepTimer()

    }

    public class SleepTimerDialogFragment : DialogFragment
    {
        BaseViewModelAnd MyViewModel { get; set; } = null!;
        public SleepTimerDialogFragment(BaseViewModelAnd ViewModel)
        {
            MyViewModel= ViewModel;
            showCounter = 0;
        }
        int showCounter = 0;
        public override Dialog OnCreateDialog(Bundle? savedInstanceState)
        {

            
            var customViewBeingClockPicker = new TimePicker(Activity);
            customViewBeingClockPicker.SetIs24HourView(Java.Lang.Boolean.False);
            var materialCardView = new MaterialCardView(Activity!)
            {
                Radius = AppUtil.DpToPx(12),
                CardElevation = AppUtil.DpToPx(8),
                LayoutParameters = new ViewGroup.LayoutParams(
                    ViewGroup.LayoutParams.MatchParent,
                    ViewGroup.LayoutParams.WrapContent)
            };
            materialCardView.AddView(customViewBeingClockPicker);
            var builder = new MaterialAlertDialogBuilder(Activity)
                .SetView(materialCardView);
            builder.SetTitle("Set Sleep Timer");
            // Here you can add options for sleep timer durations
            builder.SetPositiveButton("Set", (s, e) =>
            {
                int hour = customViewBeingClockPicker.Hour;
                int minute = customViewBeingClockPicker.Minute;

                var toTimeSpan = new TimeSpan(hour, minute, 0);
                MyViewModel.SetSleepTimer(toTimeSpan);
                // Handle setting the sleep timer using hour and minute
                
            });
            builder.SetNegativeButton("Cancel", (s, e) =>
            {
                var toastMsg = Toast.MakeText(Context, "Sleep Timer Cancelled", ToastLength.Short);
                toastMsg?.Show();
            });
            showCounter++;
            return builder.Create();
        }
    }


    private void VolumeSlider_Touch(object? sender, View.TouchEventArgs e)
    {
        var slider = sender as Slider;
        if (e.Event?.Action == MotionEventActions.Up || e.Event?.Action == MotionEventActions.Cancel)
        {
            if (slider != null)
            {
                var volumeLevel = slider.Value;
                MyViewModel.SetVolumeLevel(volumeLevel);
            }
        }
    }

    // --- HELPER: Handle Slider Drop (Debounced) ---
    private void OnSeekCompleted(double value)
    {
        // This method fires ONLY when the user lets go of the handle
        // Implement your ViewModel command here
        Toast.MakeText(Context, $"Seek to: {value}", ToastLength.Short)?.Show();
        var position = MyViewModel.CurrentPlayingSongView.DurationInSeconds * (value / 100.0);
        MyViewModel.SeekTrackPositionCommand.Execute(value);
    }

    // --- HELPER: DP to PX ---
    private int DpToPx(int dp)
    {
        return (int)(dp * Resources!.DisplayMetrics!.Density);
    }

    public void OnBackInvoked()
    {
        Toast.MakeText(Context!, "Back invoked in HomePageFragment", ToastLength.Short)?.Show();

    }
}

