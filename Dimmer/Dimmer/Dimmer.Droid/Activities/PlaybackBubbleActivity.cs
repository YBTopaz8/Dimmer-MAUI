using Android.App;
using Android.Content; // <-- Add if missing for Intent/ComponentName etc.
using Android.OS;
using Android.Widget;
using AndroidX.AppCompat.App;
using System;
using System.Diagnostics;

// *** Correct using statement for the listener interface ***
using Android.Views;
using View = Android.Views.View;
using ImageButton = Android.Widget.ImageButton; // <--- Ensure this is present
namespace Dimmer.Activities;
// Match attributes with AndroidManifest.xml
[Activity(Label = "Playback Bubble", Theme = "@style/Theme.AppCompat.DayNight.NoActionBar", AllowEmbedded = true, ResizeableActivity = true, DocumentLaunchMode = Android.Content.PM.DocumentLaunchMode.Always, Exported = false)]
public class PlaybackBubbleActivity: AppCompatActivity, View.IOnClickListener, IPlaybackBubbleUpdateListener
{
    // --- UI Elements ---
    private ImageView? _coverImageView;
    private TextView? _titleTextView;
    private TextView? _artistTextView;
    private TextView? _albumTextView;
    private SeekBar? _seekBar;
    private ImageButton? _playPauseButton;
    private ImageButton? _closeButton;

    // --- Service Communication ---
    private ExoPlayerServiceBinder? _serviceBinder; // Use YOUR Binder class name
    private bool _isBound = false;
    private PlaybackServiceConnection? _serviceConnection;


    // --- Activity Lifecycle Methods ---

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContentView(Resource.Layout.PlaybackBubbleActivity); // Ensure this layout exists!

        // Find UI Elements
        _coverImageView = FindViewById<ImageView>(Resource.Id.bubble_cover_image);
        _titleTextView = FindViewById<TextView>(Resource.Id.bubble_song_title);
        _artistTextView = FindViewById<TextView>(Resource.Id.bubble_song_artist);
        _albumTextView = FindViewById<TextView>(Resource.Id.bubble_song_album);
        _seekBar = FindViewById<SeekBar>(Resource.Id.bubble_seek_bar);
        _playPauseButton = FindViewById<ImageButton>(Resource.Id.bubble_play_pause_button);
        _closeButton = FindViewById<ImageButton>(Resource.Id.bubble_close_button);

        // Set Click Listeners
        _playPauseButton?.SetOnClickListener(this);
        _closeButton?.SetOnClickListener(this);

        // --- SeekBar Setup (Placeholder - Seeking needs more logic) ---
        _seekBar?.SetOnSeekBarChangeListener(null); // Disable user seeking for now
        _seekBar.Max=100; // Default max
        _seekBar?.SetProgress(0, false);

        // Initialize service connection object
        _serviceConnection = new PlaybackServiceConnection(this);

        Console.WriteLine("PlaybackBubbleActivity: OnCreate completed.");
    }

    protected override void OnStart()
    {
        base.OnStart();
        Console.WriteLine("PlaybackBubbleActivity: OnStart - Binding to service...");
        // Bind to the MediaSessionService
        if (_serviceConnection != null)
        {
            Intent serviceIntent = new Intent(this, typeof(ExoPlayerServiceBinder)); // <<< Use YOUR actual service class name here
                                                                                  // Start the service explicitly if it might not be running (optional, depends on your service lifecycle)
                                                                                  // StartService(serviceIntent);
            BindService(serviceIntent, _serviceConnection, Bind.AutoCreate);
        }
        else
        {
            Console.WriteLine("PlaybackBubbleActivity: ServiceConnection is null in OnStart.");
        }
    }

    protected override void OnStop()
    {
        base.OnStop();
        Console.WriteLine("PlaybackBubbleActivity: OnStop - Unbinding from service...");
        // Unbind from the service
        if (_isBound && _serviceConnection != null)
        {
            // IMPORTANT: Tell the service this bubble activity is no longer the primary listener
            _serviceBinder?.Service.SetBubbleUpdateListener(null); // <<< Implement SetBubbleUpdateListener in your Service

            UnbindService(_serviceConnection);
        }
        _isBound = false;
        _serviceBinder = null; // Clear binder reference
    }

    // --- IPlaybackBubbleUpdateListener Implementation ---
    // These methods are called BY the Service (via the reference it holds)

    public void UpdateMetadataUI(string? title, string? artist, string? album /*, Bitmap coverArt = null */)
    {
        // Ensure updates happen on the UI thread
        RunOnUiThread(() => {
            Console.WriteLine($"PlaybackBubbleActivity: Updating Metadata - Title: {title}");
            _titleTextView?.SetText(title ?? "Unknown Title", TextView.BufferType.Normal);
            _artistTextView?.SetText(artist ?? "Unknown Artist", TextView.BufferType.Normal);
            _albumTextView?.SetText(album ?? "Unknown Album", TextView.BufferType.Normal);

            // --- TODO: Implement Cover Art Loading ---
            // This often requires a library like Glide/Coil or manual bitmap handling
            if (false /* coverArt != null */)
            {
                //     _coverImageView?.SetImageBitmap(coverArt);
            }
            else
            {
                // Use a placeholder if no art is available
                _coverImageView?.SetImageResource(Android.Resource.Drawable.SymDefAppIcon); // Generic Android icon
            }
        });
    }

    public void UpdatePlaybackStateUI(bool isPlaying, int currentPositionMs, int durationMs)
    {
        // Ensure updates happen on the UI thread
        RunOnUiThread(() => {
            Console.WriteLine($"PlaybackBubbleActivity: Updating State - IsPlaying: {isPlaying}, Pos: {currentPositionMs}, Dur: {durationMs}");
            // Update Play/Pause Button Icon
            _playPauseButton?.SetImageResource(isPlaying
                ? Android.Resource.Drawable.IcMediaPause // Built-in pause system icon
                : Android.Resource.Drawable.IcMediaPlay); // Built-in play system icon

            // Update SeekBar
            if (_seekBar != null)
            {
                if (durationMs > 0)
                {
                    _seekBar.Max = durationMs;
                    _seekBar.Progress = currentPositionMs;
                }
                else
                {
                    // Handle unknown duration (e.g., live stream)
                    _seekBar.Max = 100;
                    _seekBar.Progress = 0;
                }
            }
        });
    }


    // --- View.IOnClickListener Implementation ---

    public void OnClick(View? v)
    {
        if (!_isBound || _serviceBinder == null)
        {
            Console.WriteLine("PlaybackBubbleActivity: Click ignored - Service not bound.");
            return; // Don't do anything if not bound
        }

        // Get service instance (requires GetSessionService() method in your Binder)
        var service = _serviceBinder.Service; // <<< Use YOUR actual service class name via Binder
        if (service == null)
        {
            Console.WriteLine("PlaybackBubbleActivity: Click ignored - Could not get service instance from binder.");
            return;
        }


        int id = v?.Id ?? -1;
        if (id == Resource.Id.bubble_play_pause_button)
        {
            Console.WriteLine("PlaybackBubbleActivity: Play/Pause clicked - Sending command to service.");
            
            if (service.GetPlayerInstance()!.IsPlaying) // <<< Implement TogglePlayPause (or similar) in your Service
            {
                service.GetPlayerInstance()!.Pause();

            }
            else
            {
                service.GetPlayerInstance()!.Play();
            }
           
        }
        else if (id == Resource.Id.bubble_close_button)
        {
            Console.WriteLine("PlaybackBubbleActivity: Close clicked - Finishing activity.");
            // --- Optional: Tell the service to stop playback ---
            // service.StopPlayback(); // <<< Implement if needed

            // Close the bubble activity window
            FinishAndRemoveTask(); // Use this to ensure it's removed properly
        }
        // --- Optional: Handle SeekBar clicks/changes ---
        // else if (id == Resource.Id.bubble_seek_bar) { ... } // Requires IOnSeekBarChangeListener
    }


    // --- Service Connection Inner Class ---
    // Handles the connection lifecycle between the Activity and the Service

    private class PlaybackServiceConnection : Java.Lang.Object, IServiceConnection
    {
        private readonly PlaybackBubbleActivity _activity;

        public PlaybackServiceConnection(PlaybackBubbleActivity activity)
        {
            _activity = activity;
        }

        public void OnServiceConnected(ComponentName? name, IBinder? serviceBinder)
        {
            if (serviceBinder is ExoPlayerServiceBinder binder) // <<< Use YOUR actual Binder class name
            {
                _activity._serviceBinder = binder;
                _activity._isBound = true;
                Console.WriteLine("PlaybackBubbleActivity: Service Connected via Binder.");

                // --- Get Initial State & Register Listener ---
                var sessionService = binder.Service; // <<< Use YOUR actual service class name
                if (sessionService != null)
                {
                    // 1. Register this activity as the listener in the service
                    sessionService.SetBubbleUpdateListener(_activity); // <<< Implement this method in your service

                    // 2. Request current state from service to update UI immediately
                    sessionService.RequestCurrentStateForBubble(); // <<< Implement this method in your service
                }
                else
                {
                    Console.WriteLine("PlaybackBubbleActivity: Failed to get service instance from binder in OnServiceConnected.");
                }
            }
            else
            {
                Console.WriteLine($"PlaybackBubbleActivity: Incorrect binder type received: {serviceBinder?.GetType().FullName}");
            }
        }

        public void OnServiceDisconnected(ComponentName? name)
        {
            Console.WriteLine("PlaybackBubbleActivity: Service Disconnected.");
            // Clean up references
            if (_activity != null)
            { // Check if activity still exists
                _activity._serviceBinder?.Service.SetBubbleUpdateListener(null); // Unregister listener
                _activity._isBound = false;
                _activity._serviceBinder = null;
            }
        }
    }
}

public interface IPlaybackBubbleUpdateListener
{
    void UpdateMetadataUI(string? title, string? artist, string? album /*, Bitmap coverArt = null */);
    void UpdatePlaybackStateUI(bool isPlaying, int currentPositionMs, int durationMs);
}