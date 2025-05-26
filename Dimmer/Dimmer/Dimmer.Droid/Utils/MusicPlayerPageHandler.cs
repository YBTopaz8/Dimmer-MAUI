using Android.Views;
using Android.Widget;
using AndroidX.CoordinatorLayout.Widget;
using Google.Android.Material.BottomSheet;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageButton = Android.Widget.ImageButton;
using View = Android.Views.View;

namespace Dimmer.Utils;


public class MusicPlayerPageHandler : PageHandler
{
    /// <summary>
    /// The bottom sheet behavior.
    /// </summary>
    private BottomSheetBehavior _bottomSheetBehavior;
    /// <summary>
    /// The bottom sheet view.
    /// </summary>
    private LinearLayout _bottomSheetView;
    /// <summary>
    /// The track title mini text view.
    /// </summary>
    private TextView _trackTitleMiniTextView;
    /// <summary>
    /// The track title expanded text view.
    /// </summary>
    private TextView _trackTitleExpandedTextView;
    /// <summary>
    /// Play pause button mini.
    /// </summary>
    private Android.Widget.ImageButton _playPauseButtonMini;
    /// <summary>
    /// The mini player header.
    /// </summary>
    private RelativeLayout _miniPlayerHeader;
    /// <summary>
    /// The bottom sheet callback instance.
    /// </summary>
    private BottomSheetCallback _bottomSheetCallbackInstance;
    // ... other native view references

    /// <summary>
    /// The root coordinator layout.
    /// </summary>
    private CoordinatorLayout? _rootCoordinatorLayout;
    /// <summary>
    /// Creates platform view.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="NullReferenceException"></exception>
    /// <returns>A ContentViewGroup</returns>
    /// 
    private FrameLayout _mauiContentHost;
    protected override ContentViewGroup CreatePlatformView()
    {
        if (MauiContext == null || MauiContext.Context == null)
        {
            throw new InvalidOperationException("MauiContext and its Context cannot be null.");
        }

        var inflater = LayoutInflater.From(MauiContext.Context);
        // Inflate your custom Android layout. This CoordinatorLayout will act as the Page's root view.
        var nativeView = inflater?.Inflate(Resource.Layout.music_player_page_layout, null) as CoordinatorLayout;
        _rootCoordinatorLayout = nativeView;


        if (nativeView == null)
        {
            throw new InvalidOperationException($"Failed to inflate Resource.Layout.music_player_page_layout or cast it to CoordinatorLayout.");
        }

        _bottomSheetView = nativeView.FindViewById<LinearLayout>(Resource.Id.bottom_sheet_player);
        if (_bottomSheetView == null)
        {
            throw new InvalidOperationException("CRITICAL: bottom_sheet_player LinearLayout is NULL!");
        }


        // Get references to your mini player and expanded player UI elements
        _trackTitleMiniTextView = nativeView.FindViewById<TextView>(Resource.Id.track_title_mini);
        _trackTitleExpandedTextView = nativeView.FindViewById<TextView>(Resource.Id.track_title_expanded);
        _playPauseButtonMini = nativeView.FindViewById<ImageButton>(Resource.Id.play_pause_button_mini);
        var expandedContent = nativeView.FindViewById<LinearLayout>(Resource.Id.expanded_player_content)??throw new NullReferenceException("expanded_player_content not found");



        _bottomSheetView.Post(() =>
        {
            try
            {
                // Now try to get the behavior.
                // The app:layout_behavior in XML should have already created and attached it.
                var lp = _bottomSheetView.LayoutParameters as CoordinatorLayout.LayoutParams;
                if (lp != null && lp.Behavior is BottomSheetBehavior bsb)
                {
                    _bottomSheetBehavior = bsb;
                    System.Diagnostics.Debug.WriteLine("BottomSheetBehavior retrieved from LayoutParams.");
                }
                else
                {
                    // Fallback if not found on params (shouldn't happen with app:layout_behavior)
                    _bottomSheetBehavior = BottomSheetBehavior.From(_bottomSheetView);
                    System.Diagnostics.Debug.WriteLine("BottomSheetBehavior retrieved using From().");
                }

                if (_bottomSheetBehavior == null)
                {
                    System.Diagnostics.Debug.WriteLine("CRITICAL: _bottomSheetBehavior is STILL NULL after Post().");
                    return;
                }

                // Configure BottomSheetBehavior
                _bottomSheetBehavior.PeekHeight = (int)MauiContext.Context.ToPixels(80);
                _bottomSheetBehavior.Hideable = false;
                _bottomSheetBehavior.SkipCollapsed = false; // Explicitly set if needed
                _bottomSheetBehavior.State = BottomSheetBehavior.StateCollapsed; // Set initial state

                var expandedContent = _rootCoordinatorLayout.FindViewById<LinearLayout>(Resource.Id.expanded_player_content);
                if (expandedContent != null)
                {
                    _bottomSheetCallbackInstance = new BottomSheetCallback(expandedContent);
                    _bottomSheetBehavior.AddBottomSheetCallback(_bottomSheetCallbackInstance);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("CRITICAL: expanded_player_content is NULL in Post().");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception in _bottomSheetView.Post(): {ex}");
            }
        });

        // Setup MAUI content host (can still be done synchronously)
        _mauiContentHost = _rootCoordinatorLayout.FindViewById<FrameLayout>(Resource.Id.maui_content_host);
        if (VirtualView is ContentPage contentPage && contentPage.Content != null && _mauiContentHost != null && MauiContext != null)
        {
            var nativeMauiView = contentPage.Content.ToPlatform(MauiContext);
            nativeMauiView.LayoutParameters = new FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
            _mauiContentHost.AddView(nativeMauiView);
        }

        // Event listeners for mini player can be set up synchronously
        _playPauseButtonMini = _rootCoordinatorLayout.FindViewById<ImageButton>(Resource.Id.play_pause_button_mini);
        if (_playPauseButtonMini != null)
            _playPauseButtonMini.Click += OnPlayPauseMiniClick;
        _miniPlayerHeader = _rootCoordinatorLayout.FindViewById<RelativeLayout>(Resource.Id.mini_player_header);
        if (_miniPlayerHeader != null)
            _miniPlayerHeader.Click += OnMiniPlayerHeaderClick;


        var mauiContentViewGroup = new ContentViewGroup(Context);
        mauiContentViewGroup.AddView(_rootCoordinatorLayout, new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent));
        return mauiContentViewGroup;

    }
    /// <summary>
    /// On play pause mini click.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The E.</param>
    private void OnPlayPauseMiniClick(object? sender, EventArgs e)
    {
        (VirtualView as MusicPlayerPage)?.PlayPauseCommand?.Execute(null);
    }

    /// <summary>
    /// On mini player header click.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The E.</param>
    private void OnMiniPlayerHeaderClick(object? sender, EventArgs e)
    {
        if (_bottomSheetBehavior == null)
            return;
        if (_bottomSheetBehavior.State == BottomSheetBehavior.StateCollapsed)
        {
            _bottomSheetBehavior.State = BottomSheetBehavior.StateExpanded;
        }
        else if (_bottomSheetBehavior.State == BottomSheetBehavior.StateExpanded)
        {
            _bottomSheetBehavior.State = BottomSheetBehavior.StateCollapsed;
        }
    }



    // This is where you map MAUI properties to native view updates
    /// <summary>
    /// The property mapper.
    /// </summary>
    public static readonly IPropertyMapper<MusicPlayerPage, MusicPlayerPageHandler> PropertyMapper =
        new PropertyMapper<MusicPlayerPage, MusicPlayerPageHandler>(ViewHandler.ViewMapper)
        {
            [nameof(MusicPlayerPage.TrackTitle)] = MapTrackTitle,
            [nameof(MusicPlayerPage.ArtistName)] = MapArtistName,
            [nameof(MusicPlayerPage.AlbumArtSource)] = MapAlbumArtSource,
            [nameof(MusicPlayerPage.IsPlaying)] = MapIsPlaying,
            [nameof(MusicPlayerPage.CurrentProgress)] = MapCurrentProgress,
            [nameof(MusicPlayerPage.CurrentTimeDisplay)] = MapCurrentTimeDisplay,
            [nameof(MusicPlayerPage.DurationDisplay)] = MapDurationDisplay,
        };

    /// <summary>
    /// Initializes a new instance of the <see cref="MusicPlayerPageHandler"/> class.
    /// </summary>
    public MusicPlayerPageHandler() : base(PropertyMapper)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MusicPlayerPageHandler"/> class.
    /// </summary>
    /// <param name="mapper">The mapper.</param>
    public MusicPlayerPageHandler(IPropertyMapper mapper = null) : base(mapper ?? PropertyMapper)
    {
    }

    /// <summary>
    /// Maps track title.
    /// </summary>
    /// <param name="handler">The handler.</param>
    /// <param name="page">The page.</param>
    private static void MapTrackTitle(MusicPlayerPageHandler handler, MusicPlayerPage page)
    {
        handler._trackTitleMiniTextView?.SetText(page.TrackTitle, TextView.BufferType.Normal);
        handler._trackTitleExpandedTextView?.SetText(page.TrackTitle, TextView.BufferType.Normal); // Or a more detailed title
    }
    /// <summary>
    /// Maps artist name.
    /// </summary>
    /// <param name="handler">The handler.</param>
    /// <param name="page">The page.</param>
    private static void MapArtistName(MusicPlayerPageHandler handler, MusicPlayerPage page)
    {
        // Assuming you add TextViews for artist name in your XML
        // handler._artistNameMiniTextView?.SetText(page.ArtistName, TextView.BufferType.Normal);
        // handler._artistNameExpandedTextView?.SetText(page.ArtistName, TextView.BufferType.Normal);
    }

    /// <summary>
    /// Maps album art source.
    /// </summary>
    /// <param name="handler">The handler.</param>
    /// <param name="page">The page.</param>
    private static void MapAlbumArtSource(MusicPlayerPageHandler handler, MusicPlayerPage page)
    {
        // Assuming ImageView in your XML:
        // ImageView miniAlbumArt = handler.PlatformView.FindViewById<ImageView>(Resource.Id.album_art_mini);
        // ImageView expandedAlbumArt = handler.PlatformView.FindViewById<ImageView>(Resource.Id.album_art_expanded);
        //
        // if (page.AlbumArtSource != null && handler.MauiContext != null)
        // {
        //     var service = handler.MauiContext.Services.GetRequiredService<IImageSourceService>();
        //     var result = await service.GetDrawableAsync(page.AlbumArtSource, handler.MauiContext.Context);
        //     miniAlbumArt?.SetImageDrawable(result?.Value);
        //     expandedAlbumArt?.SetImageDrawable(result?.Value);
        // }
        // else
        // {
        //     miniAlbumArt?.SetImageDrawable(null);
        //     expandedAlbumArt?.SetImageDrawable(null);
        // }
    }

    /// <summary>
    /// Map is playing.
    /// </summary>
    /// <param name="handler">The handler.</param>
    /// <param name="page">The page.</param>
    private static void MapIsPlaying(MusicPlayerPageHandler handler, MusicPlayerPage page)
    {
        // Assuming your XML has these IDs
        var playPauseMini = handler.PlatformView?.FindViewById<Android.Widget.ImageButton>(Resource.Id.play_pause_button_mini);
        // var playPauseExpanded = handler.PlatformView.FindViewById<ImageButton>(Resource.Id.play_pause_button_expanded);

        if (page.IsPlaying)
        {
            playPauseMini?.SetImageResource(Android.Resource.Drawable.IcMediaPause);
            //playPauseExpanded?.SetImageResource(Android.Resource.Drawable.IcMediaPause);
        }
        else
        {
            playPauseMini?.SetImageResource(Android.Resource.Drawable.IcMediaPlay);
            // playPauseExpanded?.SetImageResource(Android.Resource.Drawable.IcMediaPlay);
        }
    }
    /// <summary>
    /// Maps current progress.
    /// </summary>
    /// <param name="handler">The handler.</param>
    /// <param name="page">The page.</param>
    private static void MapCurrentProgress(MusicPlayerPageHandler handler, MusicPlayerPage page)
    {
        var seekBar = handler.PlatformView?.FindViewById<SeekBar>(Resource.Id.song_progress_bar);
        if (seekBar != null)
        {
            // SeekBar typically works with integers 0-Max.
            // Convert double progress (0.0-1.0) to integer.
            int progress = (int)(page.CurrentProgress * seekBar.Max);
            seekBar.Progress = progress;
        }
    }
    /// <summary>
    /// Maps current time display.
    /// </summary>
    /// <param name="handler">The handler.</param>
    /// <param name="page">The page.</param>
    private static void MapCurrentTimeDisplay(MusicPlayerPageHandler handler, MusicPlayerPage page)
    {
        // TextView currentTimeTextView = handler.PlatformView.FindViewById<TextView>(Resource.Id.current_time_text);
        // currentTimeTextView?.SetText(page.CurrentTimeDisplay, TextView.BufferType.Normal);
    }

    /// <summary>
    /// Maps duration display.
    /// </summary>
    /// <param name="handler">The handler.</param>
    /// <param name="page">The page.</param>
    private static void MapDurationDisplay(MusicPlayerPageHandler handler, MusicPlayerPage page)
    {
        // TextView durationTextView = handler.PlatformView.FindViewById<TextView>(Resource.Id.duration_text);
        // durationTextView?.SetText(page.DurationDisplay, TextView.BufferType.Normal);
    }
    // Example mapper for play/pause state
    // private static void MapIsPlayingState(MusicPlayerPageHandler handler, MusicPlayerPage page)
    // {
    //     if (page.IsPlaying)
    //     {
    //         handler._playPauseButtonMini?.SetImageResource(Android.Resource.Drawable.IcMediaPause);
    //     }
    //     else
    //     {
    //         handler._playPauseButtonMini?.SetImageResource(Android.Resource.Drawable.IcMediaPlay);
    //     }
    // }

    // Change parameter type to PageViewGroup
    /// <summary>
    /// Disconnects the handler.
    /// </summary>
    /// <param name="platformView">The platform view.</param>
    protected override void DisconnectHandler(ContentViewGroup platformView) // platformView is the PageViewGroup returned by CreatePlatformView
    {
        if (_playPauseButtonMini != null)
        {
            _playPauseButtonMini.Click -= OnPlayPauseMiniClick;
        }
        if (_miniPlayerHeader != null)
        {
            _miniPlayerHeader.Click -= OnMiniPlayerHeaderClick;
        }

        if (_bottomSheetBehavior != null && _bottomSheetCallbackInstance != null)
        {
            _bottomSheetBehavior.RemoveBottomSheetCallback(_bottomSheetCallbackInstance);
            _bottomSheetCallbackInstance = null; // Allow GC
        }

        _bottomSheetBehavior?.Dispose();
        _bottomSheetBehavior = null;

        // _bottomSheetView is part of the inflated layout, managed by platformView's lifecycle.
        // Explicit dispose might not be needed if platformView handles its children.
        // _bottomSheetView?.Dispose();
        // _bottomSheetView = null;

        _trackTitleMiniTextView = null;
        _trackTitleExpandedTextView = null;
        _playPauseButtonMini = null;
        _miniPlayerHeader = null;
        _mauiContentHost?.RemoveAllViews();
        _mauiContentHost = null;
        base.DisconnectHandler(platformView);
    }
}


// Helper class for BottomSheetBehavior callbacks/// <summary>
/// The bottom sheet callback.
/// </summary>

public class BottomSheetCallback : BottomSheetBehavior.BottomSheetCallback
{
    /// <summary>
    /// Expanded content.
    /// </summary>
    private readonly View _expandedContent; // The view to show/hide

    /// <summary>
    /// Initializes a new instance of the <see cref="BottomSheetCallback"/> class.
    /// </summary>
    /// <param name="expandedContent">The expanded content.</param>
    public BottomSheetCallback(View expandedContent)
    {
        _expandedContent = expandedContent;
    }

    /// <summary>
    /// On state changed.
    /// </summary>
    /// <param name="p0">The p0.</param>
    /// <param name="p1">The p1.</param>
    public override void OnStateChanged(View p0, int p1)
    {
        // Handle state changes:
        // p1 can be StateDragging, StateSettling, StateExpanded, StateCollapsed, StateHidden
        if (p1 == (int)BottomSheetBehavior.StateExpanded)
        {
            _expandedContent.Visibility = ViewStates.Visible;
        }
        else if (p1 == (int)BottomSheetBehavior.StateCollapsed)
        {
            _expandedContent.Visibility = ViewStates.Gone; // Or Invisible
        }
        // You can add more logic here, e.g., for animations or other UI changes
    }

    /// <summary>
    /// On slide.
    /// </summary>
    /// <param name="bottomSheet">The bottom sheet.</param>
    /// <param name="newState">The new state.</param>
    public override void OnSlide(View bottomSheet, float newState)
    {
        // p1 ranges from -1.0 (hidden) to 1.0 (expanded).
        // 0.0 is collapsed.
        // You can use this for animations, e.g., cross-fading elements.
        // For example, fade in expanded content as it slides up:
        if (newState > 0) // Moving towards expanded
        {
            _expandedContent.Visibility = ViewStates.Visible;
            _expandedContent.Alpha = newState;
        }
        else // Moving towards collapsed or hidden
        {
            // Only make it gone if fully collapsed, otherwise it might look weird during drag
            if (newState <= 0 && _expandedContent.Visibility == ViewStates.Visible && bottomSheet.Height * (1+newState) < _expandedContent.Top)
            {
                // _expandedContent.Alpha = 1 + p1; // Fade out (if p1 goes negative)
            }
        }
    }

    //public override void OnSlide(Android.Views.View bottomSheet, float p1)
    //{
    //    throw new NotImplementedException();
    //}

    //public override void OnStateChanged(Android.Views.View p0, int p1)
    //{
    //    throw new NotImplementedException();
    //}
}