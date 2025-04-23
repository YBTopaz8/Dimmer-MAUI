#region Using Directives
// Android Core
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Uri = Android.Net.Uri;

// AndroidX Core & Media
using AndroidX.Core.App;
using AndroidX.Core.Content;

// Media3 Core Components - !! ENSURE NUGETS ARE CORRECT !!
using AndroidX.Media3.Common; // Provides IPlayer, MediaItem, MediaMetadata, Rating, etc.
using AndroidX.Media3.ExoPlayer; // Provides IExoPlayer, ExoPlayer
using AndroidX.Media3.Session; // Provides MediaSessionService, MediaSession, SessionCommand, SessionResult, etc.

// Java Interop
using Java.Lang;
using Object = Java.Lang.Object;

// AndroidX Concurrent Futures - For CallbackToFutureAdapter
using AndroidX.Concurrent.Futures; // <<-- USE THIS
using Google.Common.Util.Concurrent; // <<-- PROVIDES IListenableFuture INTERFACE needed by CallbackToFutureAdapter

// System & IO
using System;
using System.IO;
using System.Collections.Generic;
#endregion

// Your App specific using
using Dimmer.Droid; // Make sure this namespace is correct for MainActivity and HeartRating
using AndroidX.Media3.Common.Text;
// using Exception = Java.Lang.Exception; // Can use System.Exception generally
using DeviceInfo = AndroidX.Media3.Common.DeviceInfo;
using Exception = Java.Lang.Exception;


namespace Dimmer.DimmerAudio // Make sure this namespace is correct
{
    [Service(Name = "com.yvanbrunel.dimmer.MediaPlaybackService", // Ensure this matches AndroidManifest.xml if needed
             Enabled = true, Exported = true,
             ForegroundServiceType = ForegroundService.TypeMediaPlayback)]
    public class ExoPlayerService : MediaSessionService
    {
        // --- NuGet Check Reminder ---
        // Required:
        // - Xamarin.AndroidX.Media3.Common
        // - Xamarin.AndroidX.Media3.ExoPlayer
        // - Xamarin.AndroidX.Media3.Session
        // - Xamarin.AndroidX.Concurrent.Futures
        // - Xamarin.Google.Guava.ListenableFuture (or full Guava, needed for IListenableFuture interface itself)
        // --- End NuGet Check Reminder ---

        // --- Components ---
        private MediaSession? mediaSession;
        private IExoPlayer? player;
        private MediaPlaybackSessionCallback? sessionCallback; // Use concrete type

        // --- Constants ---
        public const string CommandPreparePlay = "com.yvanbrunel.dimmer.action.PREPARE_PLAY";
        public const string CommandSetFavorite = "com.yvanbrunel.dimmer.action.SET_FAVORITE";
        public const string KeyMediaPlayDataUrl = "KEY_URL";
        public const string KeyMediaPlayDataTitle = "KEY_TITLE";
        public const string KeyMediaPlayDataArtist = "KEY_ARTIST";
        public const string KeyMediaPlayDataAlbum = "KEY_ALBUM";
        public const string KeyMediaPlayDataImagePath = "KEY_IMAGE_PATH";
        public const string KeyMediaPlayDataDuration = "KEY_DURATION";
        public const string KeyMediaPlayDataPosition = "KEY_POSITION";
        public const string KeyMediaPlayDataIsFavorite = "KEY_IS_FAVORITE";
        private const string MetadataKeyDurationString = MetadataCompat.MetadataKeyDuration; // Use constant from support lib if available

        // --- Internal State ---
        private MediaItem? currentMediaItem;
        private bool currentFavoriteStatus = false;
        private const int NotificationId = 65432; // Choose a unique ID

        // --- Service Lifecycle ---
        public override void OnCreate()
        {
            base.OnCreate();
            Console.WriteLine("[ExoPlayerService] OnCreate");
            try
            {
                player = new ExoPlayerBuilder(this)
                    .SetAudioAttributes(AudioAttributes.Default, true) // handleAudioFocus=true
                    .SetHandleAudioBecomingNoisy(true)
                    .SetWakeMode(C.WakeModeNetwork)
                    .Build();

                player.AddListener(new PlayerEventListener(this));

                sessionCallback = new MediaPlaybackSessionCallback(this); // Use concrete type

                mediaSession = new MediaSession.Builder(this, player)
                    .SetSessionActivity(GetMainActivityPendingIntent())
                    .SetCallback(sessionCallback)
                    .SetId("Dimmer_MediaSession_Main") // Choose a unique ID
                    .Build();

                Console.WriteLine("[ExoPlayerService] Initialization successful.");
            }
            catch (Java.Lang.Throwable ex) { HandleInitError("JAVA INITIALIZATION", ex); StopSelf(); }
            catch (System.Exception ex) { HandleInitError("SYSTEM INITIALIZATION", ex); StopSelf(); }
        }

        public override MediaSession? OnGetSession(MediaSession.ControllerInfo? controllerInfo)
        {
            // Called by controllers connecting to the session. Return the session instance.
            if (mediaSession == null)
            {
                LogInitWarning($"OnGetSession from {controllerInfo.PackageName}");
            }
            else
            {
                Console.WriteLine($"[ExoPlayerService] OnGetSession: Returning session instance to {controllerInfo.PackageName}.");
            }
            return mediaSession;
        }

        public override void OnDestroy()
        {
            Console.WriteLine("[ExoPlayerService] OnDestroy");
            // Release resources in reverse order of creation
            mediaSession?.Release();
            player?.Release();
            mediaSession = null;
            player = null;
            sessionCallback = null; // Not strictly needed but good practice
            base.OnDestroy();
        }

        // --- Public Accessors ---
        public IPlayer? GetPlayerInstance() => player;
        public MediaSession? GetMediaSessionInstance() => mediaSession;

        // --- Private Helpers ---
        private PendingIntent GetMainActivityPendingIntent()
        {
            // Intent to launch your main UI when the notification/session is tapped
            var launchIntent = new Intent(this, typeof(MainActivity)); // Ensure MainActivity is correct
            launchIntent.SetAction(Intent.ActionMain);
            launchIntent.AddCategory(Intent.CategoryLauncher);
            // Flags ensure existing instance is brought forward or new one started cleanly
            launchIntent.AddFlags(ActivityFlags.NewTask | ActivityFlags.ClearTop);

            // Use Immutable flag for security on Android S+
            PendingIntentFlags flags = PendingIntentFlags.UpdateCurrent;
            if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
            {
                flags |= PendingIntentFlags.Immutable;
            }

            return PendingIntent.GetActivity(this, 0, launchIntent, flags);
        }

        private void LogInitWarning(string stage)
        {
            Console.WriteLine($"[ExoPlayerService] Warning: {stage} called but initialization may have failed or is incomplete.");
            Console.WriteLine("---> Check previous logs for errors and verify NuGet packages are correct! <---");
        }
        private void HandleInitError(string type, System.Exception ex)
        {
            // Log critical errors - consider using a more robust logging framework
            Console.WriteLine($"[ExoPlayerService] !!! CRITICAL {type} ERROR: {ex.GetType().Name} - {ex.Message} !!!");
            Console.WriteLine($"[ExoPlayerService] Stack Trace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"[ExoPlayerService] Inner Exception: {ex.InnerException}");
            }
            Console.WriteLine("---> CHECK NuGet Packages: Media3.*, AndroidX.Concurrent.Futures, Guava/ListenableFuture <---");
            Console.WriteLine("---> Ensure target framework and dependencies are compatible. <---");
        }
        private void HandleInitError(string type, Java.Lang.Throwable ex)
        {
            Console.WriteLine($"[ExoPlayerService] !!! CRITICAL JAVA {type} ERROR: {ex.Class.Name} - {ex.LocalizedMessage} !!!");
            Console.WriteLine($"[ExoPlayerService] Java Stack Trace: {Android.Util.Log.GetStackTraceString(ex)}");
            if (ex.Cause != null)
            {
                Console.WriteLine($"[ExoPlayerService] Java Cause: {ex.Cause}");
            }
            Console.WriteLine("---> CHECK NuGet Packages: Media3.*, AndroidX.Concurrent.Futures, Guava/ListenableFuture <---");
            Console.WriteLine("---> Check for Java class/method not found errors - often due to mismatched bindings. <---");
        }


        // --- Internal Command Handlers (Called by Session Callback) ---
        internal void HandlePreparePlay(Bundle extras)
        {
            if (player == null)
            { Console.WriteLine("[ExoPlayerService] HandlePreparePlay failed: Player is null."); return; }

            string? url = extras.GetString(KeyMediaPlayDataUrl);
            if (string.IsNullOrEmpty(url))
            { Console.WriteLine("[ExoPlayerService] HandlePreparePlay failed: URL missing."); return; }

            string title = extras.GetString(KeyMediaPlayDataTitle, "Unknown Title");
            string artist = extras.GetString(KeyMediaPlayDataArtist, "Unknown Artist");
            string album = extras.GetString(KeyMediaPlayDataAlbum, "Unknown Album");
            string? imagePath = extras.GetString(KeyMediaPlayDataImagePath);
            long startPosition = extras.GetLong(KeyMediaPlayDataPosition, 0); // Use GetLong for position
            long duration = extras.GetLong(KeyMediaPlayDataDuration, C.TimeUnset);
            bool isFavorite = extras.GetBoolean(KeyMediaPlayDataIsFavorite, false);
            currentFavoriteStatus = isFavorite; // Update internal state

            Console.WriteLine($"[ExoPlayerService] Handling PREPARE_PLAY: Title='{title}', StartPos={startPosition}ms, Favorite={isFavorite}");

            var metadataBuilder = new MediaMetadata.Builder()
                .SetTitle(title)
                .SetArtist(artist)
                .SetAlbumTitle(album)
                .SetMediaType(new Java.Lang.Integer(MediaMetadata.MediaTypeMusic)) // Use Java Integer wrapper
                .SetIsPlayable(Java.Lang.Boolean.True); // Use Java Boolean wrapper

            // Set user rating (favorite status)
            metadataBuilder.SetUserRating(new HeartRating(isFavorite)); // Ensure HeartRating class exists

            // Set artwork URI if available
            if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
            {
                try
                {
                    metadataBuilder.SetArtworkUri(Uri.FromFile(new Java.IO.File(imagePath)));
                    Console.WriteLine($"[ExoPlayerService] Set ArtworkUri: {imagePath}");
                }
                catch (System.Exception ex)
                {
                    Console.WriteLine($"[ExoPlayerService] Warning: Failed to set ArtworkUri from path '{imagePath}': {ex.Message}");
                }
            }
            else if (!string.IsNullOrEmpty(imagePath))
            {
                Console.WriteLine($"[ExoPlayerService] Warning: Artwork file not found: {imagePath}");
            }


            // Add duration to extras bundle if available (Media3 might pick it up)
            Bundle mdExtras = new Bundle();
            if (duration != C.TimeUnset)
            {
                mdExtras.PutLong(MetadataKeyDurationString, duration); // Use standard key
            }
            metadataBuilder.SetExtras(mdExtras);


            try
            {
                currentMediaItem = new MediaItem.Builder()
                   .SetMediaId(url) // Use URL as Media ID for simplicity
                   .SetUri(Uri.Parse(url))
                   .SetMediaMetadata(metadataBuilder.Build())
                   .Build();

                Console.WriteLine($"[ExoPlayerService] Setting MediaItem: ID={currentMediaItem.MediaId}, Pos={startPosition}");
                player.SetMediaItem(currentMediaItem, startPosition); // Set item and start position
                player.Prepare();
                player.Play(); // Start playback immediately
                Console.WriteLine("[ExoPlayerService] Player Prepare() and Play() called.");
            }
            catch (Java.Lang.Throwable jex) { HandleInitError("PreparePlay SetMediaItem/Prepare", jex); }
            catch (System.Exception ex) { HandleInitError("PreparePlay SetMediaItem/Prepare", ex); }
        }

        internal void HandleSetFavorite(Bundle extras)
        {
            if (player == null)
            { Console.WriteLine("[ExoPlayerService] HandleSetFavorite failed: Player is null."); return; }
            if (currentMediaItem == null)
            { Console.WriteLine("[ExoPlayerService] HandleSetFavorite failed: currentMediaItem is null."); return; }

            bool isFavorite = extras.GetBoolean(KeyMediaPlayDataIsFavorite, false);

            // Avoid unnecessary updates if status hasn't changed
            if (currentFavoriteStatus == isFavorite)
            {
                Console.WriteLine($"[ExoPlayerService] HandleSetFavorite: Status already {isFavorite}, skipping update.");
                return;
            }

            currentFavoriteStatus = isFavorite; // Update internal state FIRST
            Console.WriteLine($"[ExoPlayerService] Handling SET_FAVORITE: IsFavorite={isFavorite}");

            // Get existing metadata, prioritize player's current metadata
            MediaMetadata? currentMetadata = player.MediaMetadata;
            if (currentMetadata == null && currentMediaItem.MediaMetadata != null)
            {
                currentMetadata = currentMediaItem.MediaMetadata;
                Console.WriteLine("[ExoPlayerService] HandleSetFavorite: Using metadata from initial MediaItem.");
            }
            else if (currentMetadata == null)
            {
                Console.WriteLine("[ExoPlayerService] HandleSetFavorite failed: Cannot find current metadata to update.");
                return;
            }


            try
            {
                // Create a builder based on the current metadata
                var newMetadataBuilder = currentMetadata.BuildUpon();

                // Update the user rating
                newMetadataBuilder.SetUserRating(new HeartRating(isFavorite)); // Ensure HeartRating exists
                var updatedMetadata = newMetadataBuilder.Build();

                // Create a new MediaItem with the updated metadata
                var newItemBuilder = currentMediaItem.BuildUpon().SetMediaMetadata(updatedMetadata);
                var updatedMediaItem = newItemBuilder.Build();

                // Store the updated item
                currentMediaItem = updatedMediaItem;

                // Update the player's current item *without* resetting position or state
                // Note: This might be tricky. Depending on the player state and exact behavior desired,
                // you might need to just update the session's metadata if the player doesn't reflect
                // MediaItem metadata changes smoothly mid-playback without a setMediaItem call.
                // Let's try updating the player's metadata directly first if possible.

                // Check if player allows direct metadata update (might not be public API)
                // If not, we might need to call setMediaItem again, preserving position.

                // Option 1: Try just updating the player's mediaMetadata property if it takes effect (less disruptive)
                // player.MediaMetadata = updatedMetadata; // This property is likely read-only

                // Option 2: Replace the item, preserving position (might cause slight interruption)
                long currentPosition = player.CurrentPosition;
                bool playWhenReady = player.PlayWhenReady;
                Console.WriteLine($"[ExoPlayerService] HandleSetFavorite: Replacing media item to update metadata at pos {currentPosition}");
                player.SetMediaItem(currentMediaItem, currentPosition);
                // We might need to re-prepare if SetMediaItem clears the prepared state
                // player.Prepare(); // Check if needed after setMediaItem
                player.PlayWhenReady = playWhenReady; // Restore PlayWhenReady state

                // Option 3: Update MediaSession? metadata directly (affects controllers, maybe not player)
                mediaSession?.SetPlayer(player, player.AvailableCommands); // Might refresh metadata

                Console.WriteLine($"[ExoPlayerService] SetFavorite update processed for item {currentMediaItem.MediaId}.");

            }
            catch (Java.Lang.Throwable jex) { HandleInitError("SetFavorite Update", jex); }
            catch (System.Exception ex) { HandleInitError("SetFavorite Update", ex); }
        }


        // --- Player Event Listener ---
        private class PlayerEventListener : Object, IPlayerListener // Use specific IPlayer.Listener
        {
            private readonly ExoPlayerService service;
            public PlayerEventListener(ExoPlayerService service) { this.service = service; }

            // --- Key Event Implementations ---
            public void OnPlaybackStateChanged(int playbackState)
            {
                string stateString = playbackState switch
                {
                    1 => "Idle",
                    2 => "Buffering",
                    3 => "Ready",
                    4 => "Ended",
                    _ => "Unknown"
                };
                Console.WriteLine($"[PlayerEventListener] PlaybackStateChanged: {stateString} ({playbackState})");
                // You might trigger notification updates or UI changes based on state here
            }

            public void OnIsPlayingChanged(bool isPlaying)
            {
                Console.WriteLine($"[PlayerEventListener] IsPlayingChanged: {isPlaying}");
                // Update foreground service state, notification actions, etc.
            }

            public void OnPlayerError(PlaybackException error) // Use specific PlaybackException
            {
                Console.WriteLine($"[PlayerEventListener] PlayerError: Code={error.ErrorCodeName}, Message={error.Message}");
                // Log the full error
                service.HandleInitError("PLAYER_ERROR", error);
                // Optionally stop the player or service on error
                service.player?.Stop();
                // service.StopSelf(); // Consider if service should stop on error
            }

            public void OnPlayerErrorChanged(PlaybackException? error) // Nullable error
            {
                if (error != null)
                {
                    Console.WriteLine($"[PlayerEventListener] PlayerErrorChanged: Code={error.ErrorCodeName}, Message={error.Message}");
                    service.HandleInitError("PLAYER_ERROR_CHANGED", error);
                }
                else
                {
                    Console.WriteLine("[PlayerEventListener] PlayerErrorChanged: Error cleared.");
                }
            }

            public void OnMediaMetadataChanged(MediaMetadata? mediaMetadata)
            {
                Console.WriteLine($"[PlayerEventListener] MediaMetadataChanged: Title='{mediaMetadata?.Title ?? "N/A"}', Favorite={mediaMetadata?.UserRating is HeartRating hr && hr.IsHeart}");
                // Update notification, UI if needed
                // This confirms if HandleSetFavorite successfully updated player metadata
                if (mediaMetadata?.UserRating is HeartRating hrr)
                {
                    service.currentFavoriteStatus = hrr.IsHeart; // Sync internal state if needed
                }
            }

            public void OnMediaItemTransition(MediaItem? item, int reason)
            {
                string reasonString = reason switch
                {
                    1 => "Repeat",
                    2 => "Auto",
                    3 => "Seek",
                    4 => "PlaylistChanged",
                    _ => "Unknown"
                };
                Console.WriteLine($"[PlayerEventListener] MediaItemTransition: Item='{item?.MediaId ?? "None"}', Reason={reasonString} ({reason})");
                service.currentMediaItem = item; // Update the service's current item reference
                                                 // Reset favorite status based on the new item's metadata if available
                if (item?.MediaMetadata?.UserRating is HeartRating hr)
                {
                    service.currentFavoriteStatus = hr.IsHeart;
                }
                else
                {
                    service.currentFavoriteStatus = false; // Default if no rating
                }
            }

            // --- Other IPlayer.Listener Methods (Implement if needed, stubs are often sufficient) ---
            public void OnAudioAttributesChanged(AudioAttributes? p0) { /* Log if needed */ }
            public void OnAudioSessionIdChanged(int p0) { /* Log if needed */ }
            public void OnAvailableCommandsChanged(PlayerCommands? p0) { Console.WriteLine($"[PlayerEventListener] AvailableCommandsChanged"); }
            public void OnCues(CueGroup? p0) { /* Handle subtitles/cues */ }
            public void OnDeviceInfoChanged(DeviceInfo? p0) { /* Log if needed */ }
            public void OnDeviceVolumeChanged(int p0, bool p1) { /* Log if needed */ }
            public void OnEvents(IPlayer? player, PlayerEvents? events) { /* Generic event hook, log specific events */ }
            public void OnIsLoadingChanged(bool isLoading) { Console.WriteLine($"[PlayerEventListener] IsLoadingChanged: {isLoading}"); }
            public void OnMaxSeekToPreviousPositionChanged(long p0) { /* Log if needed */ }
            // public void OnMetadata(MediaMetadata? p0) {} // Superseded by OnMediaMetadataChanged? Check docs.
            public void OnPlayWhenReadyChanged(bool playWhenReady, int reason) { Console.WriteLine($"[PlayerEventListener] PlayWhenReadyChanged: {playWhenReady}, Reason={reason}"); }
            public void OnPlaybackParametersChanged(PlaybackParameters? p0) { /* Log if needed */ }
            public void OnPlaybackSuppressionReasonChanged(int reason) { Console.WriteLine($"[PlayerEventListener] PlaybackSuppressionReasonChanged: {reason}"); }
            // public void OnPositionDiscontinuity(int reason){} // Deprecated version
            public void OnPositionDiscontinuity(global::AndroidX.Media3.Common.PlayerPositionInfo? oldPosition, global::AndroidX.Media3.Common.PlayerPositionInfo? newPosition, int reason) { Console.WriteLine($"[PlayerEventListener] PositionDiscontinuity: Reason={reason}"); }
            public void OnRenderedFirstFrame() { Console.WriteLine($"[PlayerEventListener] RenderedFirstFrame"); }
            public void OnRepeatModeChanged(int p0) { /* Log if needed */ }
            public void OnSeekBackIncrementChanged(long p0) { /* Log if needed */ }
            public void OnSeekForwardIncrementChanged(long p0) { /* Log if needed */ }
            public void OnShuffleModeEnabledChanged(bool p0) { /* Log if needed */ }
            public void OnSkipSilenceEnabledChanged(bool p0) { /* Log if needed */ }
            public void OnSurfaceSizeChanged(int p0, int p1) { /* Video related */ }
            public void OnTimelineChanged(Timeline? p0, int reason) { Console.WriteLine($"[PlayerEventListener] TimelineChanged: Reason={reason}"); }
            public void OnTrackSelectionParametersChanged(TrackSelectionParameters? p0) { /* Log if needed */ }
            public void OnTracksChanged(Tracks? p0) { /* Log if needed */ }
            public void OnVideoSizeChanged(VideoSize? p0) { /* Video related */ }
            public void OnVolumeChanged(float p0) { /* Log if needed */ }

        } // End PlayerEventListener


        // --- Media Session Callback (Handles Controller Commands) ---
        sealed class MediaPlaybackSessionCallback : Object, MediaSession.ICallback
        {
            private readonly ExoPlayerService service;
            private const string CallbackDebugTag = "MediaSessionCallback"; // For CallbackToFutureAdapter

            public MediaPlaybackSessionCallback(ExoPlayerService service)
            {
                this.service = service;
            }

            const string TAG = "MediaSessionCallback";
            // --- Connection Handling ---
            public override MediaSession.ConnectionResult OnConnect(
      MediaSession session,
      MediaSession.ControllerInfo controller)
            {
                Console.WriteLine($"[{TAG}] OnConnect from {controller.PackageName}");

                var sessionCommands = new SessionCommands.Builder()
                    .Add(SessionCommand.COMMAND_CODE_SESSION_SET_RATING)
                    .Add(new SessionCommand(CommandPreparePlay, Bundle.Empty))
                    .Add(new SessionCommand(CommandSetFavorite, Bundle.Empty))
                    .Build();

                var playerCommands = new Player.Commands.Builder()
                    .AddAllCommands()
                    .Build();

                Console.WriteLine($"[{TAG}] Accepting {controller.PackageName}");
                return MediaSession.ConnectionResult.Accept(sessionCommands, playerCommands);
            }
            public void OnPostConnect(MediaSession? session, MediaSession.ControllerInfo? controller)
            {
                // Called after connection is established. Good place for post-connection logic if needed.
                Console.WriteLine($"[SessionCallback] OnPostConnect: Controller {controller.PackageName} connected.");
                // base.OnPostConnect(session, controller); // Call base if overridden
            }

            public void OnDisconnected(MediaSession? session, MediaSession.ControllerInfo? controller)
            {
                // Called when a controller disconnects.
                Console.WriteLine($"[SessionCallback] OnDisconnected: Controller {controller.PackageName} disconnected.");
                // base.OnDisconnected(session, controller); // Call base if overridden
            }

            // --- Command Handling ---

            // Handle CUSTOM commands sent via controller.sendCustomCommand(...)
            public IListenableFuture OnCustomCommand(
    MediaSession? session,
    MediaSession.ControllerInfo? controller,
    SessionCommand? customCommand,
    Bundle? args)
            {
                string action = customCommand.CustomAction ?? "unknown";
                Console.WriteLine($"[SessionCallback] OnCustomCommand: Action='{action}' from {controller.PackageName}");

                return CallbackToFutureAdapter.GetFuture(
                    new Resolver(completer =>
                    {
                        Java.Lang.Object result = SessionResult.ResultSuccess;
                        try
                        {
                            switch (action)
                            {
                                case CommandPreparePlay:
                                    service.HandlePreparePlay(args);
                                    break;

                                case CommandSetFavorite:
                                    service.HandleSetFavorite(args);
                                    break;

                                default:
                                    Console.WriteLine($"[SessionCallback] Unsupported custom command: {action}");
                                    result = new SessionResult(SessionResult.ResultErrorNotSupported);
                                    break;
                            }
                            completer.Set(result);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[SessionCallback] Error in OnCustomCommand '{action}': {ex}");
                            service.HandleInitError($"CustomCommand {action}", ex);
                            completer.SetException(ex);
                        }
                    },
                    // debug tag just for tracing
                    $"{CallbackDebugTag}-OnCustomCommand-{action}")
                );
            }

            // Decide whether to allow a specific PLAYER command requested by a controller
            public int OnPlayerCommandRequest(MediaSession? session, MediaSession.ControllerInfo? controllerInfo, int playerCommand)
            {
                // Default: Allow any player command that was declared available in OnConnect.
                // You could add logic here to disallow specific commands based on state or controller.
                // Example: if (playerCommand == PlayerCommands.CommandSeekTo && !isSeekAllowed) return SessionResult.ResultErrorNotSupported;
                Console.WriteLine($"[SessionCallback] OnPlayerCommandRequest: Command={playerCommand} from {controllerInfo.PackageName}. Allowing.");
                return base.OnPlayerCommandRequest(session, controllerInfo, playerCommand); // Default allows if available
            }


            // --- Standard Command Implementations (using CallbackToFutureAdapter) ---

            public IListenableFuture OnPlay(MediaSession? session, MediaSession.ControllerInfo? controller)
    => CallbackToFutureAdapter.GetFuture(
         new Resolver(c => {
             service.player?.Play();
             c.Set(SessionResult.ResultSuccess);
         }, $"{CallbackDebugTag}-OnPlay"));

            public IListenableFuture OnPause(MediaSession? session, MediaSession.ControllerInfo? controller)
                => CallbackToFutureAdapter.GetFuture(
                     new Resolver(c => {
                         service.player?.Pause();
                         c.Set(SessionResult.ResultSuccess);
                     }, $"{CallbackDebugTag}-OnPause"));

            public IListenableFuture OnStop(MediaSession? session, MediaSession.ControllerInfo? controller)
                => CallbackToFutureAdapter.GetFuture(
                     new Resolver(c => {
                         service.player?.Stop();
                         c.Set(SessionResult.ResultSuccess);
                     }, $"{CallbackDebugTag}-OnStop"));

            public IListenableFuture OnSeekTo(MediaSession? session, MediaSession.ControllerInfo? controller, long posMs)
                => CallbackToFutureAdapter.GetFuture(
                     new Resolver(c => {
                         service.player?.SeekTo(posMs);
                         c.Set(SessionResult.ResultSuccess);
                     }, $"{CallbackDebugTag}-OnSeekTo"));
            public IListenableFuture OnSetRating(
    MediaSession? session,
    MediaSession.ControllerInfo? controller,
    Rating rating)
    => CallbackToFutureAdapter.GetFuture(
        new Resolver(c => {
            try
            {
                Java.Lang.Object result = SessionResult.ResultSuccess;
                if (rating is HeartRating hr)
                {
                    var args = new Bundle();
                    args.PutBoolean(KeyMediaPlayDataIsFavorite, hr.IsHeart);
                    service.HandleSetFavorite(args);
                }
                else
                {
                    result = new SessionResult(SessionResult.ResultErrorNotSupported);
                }
                c.Set(result);
            }
            catch (Exception ex)
            {
                c.SetException(ex);
                service.HandleInitError("OnSetRating", ex);
            }
        }, $"{CallbackDebugTag}-OnSetRating")
    );

            public IListenableFuture OnSkipToNext(
                MediaSession? session,
                MediaSession.ControllerInfo? controller)
                => CallbackToFutureAdapter.GetFuture(
                    new Resolver(c => {
                        try
                        {
                            if (service.player?.IsCommandAvailable(PlayerCommands.CommandSeekToNextMediaItem) == true)
                                service.player.SeekToNextMediaItem();
                            c.Set(SessionResult.ResultSuccess);
                        }
                        catch (Exception ex)
                        {
                            c.SetException(ex);
                            service.HandleInitError("OnSkipToNext", ex);
                        }
                    }, $"{CallbackDebugTag}-OnSkipToNext")
                );

            public IListenableFuture OnSkipToPrevious(
                MediaSession? session,
                MediaSession.ControllerInfo? controller)
                => CallbackToFutureAdapter.GetFuture(
                    new Resolver(c => {
                        try
                        {
                            long pos = service.player?.CurrentPosition ?? 0;
                            long threshold = service.player?.SeekBackIncrement ?? 3000;
                            if (service.player?.IsCommandAvailable(PlayerCommands.CommandSeekToPreviousMediaItem) == true
                                && pos <= threshold)
                            {
                                service.player.SeekToPreviousMediaItem();
                            }
                            else if (service.player?.IsCommandAvailable(PlayerCommands.CommandSeekTo) == true)
                            {
                                service.player.SeekTo(0);
                            }
                            if (service.player?.PlayWhenReady ?? false)
                                service.player.Play();
                            c.Set(SessionResult.ResultSuccess);
                        }
                        catch (Exception ex)
                        {
                            c.SetException(ex);
                            service.HandleInitError("OnSkipToPrevious", ex);
                        }
                    }, $"{CallbackDebugTag}-OnSkipToPrevious")
                );

            // TODO: Implement other standard callbacks if needed, e.g.:
            // OnSetRepeatMode, OnSetShuffleModeEnabled, OnSetPlaybackSpeed, etc.
            // Use the CallbackToFutureAdapter pattern for each. Example:
            /*
            public IListenableFuture OnSetRepeatMode(MediaSession? session, MediaSession.ControllerInfo? controller, int repeatMode)
            {
                Console.WriteLine($"[SessionCallback] OnSetRepeatMode: {repeatMode}");
                return CallbackToFutureAdapter.GetFuture(completer => {
                    try { service.player?.SetRepeatMode(repeatMode); completer.Set(SessionResult.ResultSuccess); }
                    catch (System.Exception ex) { completer.SetException(ex); service.HandleInitError("OnSetRepeatMode", ex); }
                    return $"{CallbackDebugTag}-OnSetRepeatMode";
                });
            }
            */

        } // End MediaPlaybackSessionCallback

    } // End ExoPlayerService class

    // --- Helper Classes ---


    public class HeartRating : Rating
    {
        public bool IsHeart { get; }

        // Mandatory JNI ctor  
        public HeartRating(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
        }

        // Your own convenience ctor  
        public HeartRating(bool isHeart)
            : base(IntPtr.Zero, JniHandleOwnership.DoNotTransfer) // Provide default values for base class constructor  
        {
            IsHeart = isHeart;
        }

        public override bool IsRated => IsHeart;

        // Implement the abstract Bundle-serializer  
        public override Bundle ToBundle()
        {
            var bundle = new Bundle();
            bundle.PutBoolean("user_rating", IsHeart);
            return bundle;
        }
    }
    sealed class Resolver : Java.Lang.Object, CallbackToFutureAdapter.IResolver
    {
        readonly Action<CallbackToFutureAdapter.Completer> _action;
        readonly string _tag;

        public Resolver(Action<CallbackToFutureAdapter.Completer> action, string tag)
        {
            _action = action;
            _tag = tag;
        }

        public Java.Lang.Object AttachCompleter(CallbackToFutureAdapter.Completer completer)
        {
            try
            { _action(completer); }
            catch (Exception ex) { completer.SetException(ex); }
            return _tag;
        }
    }
} // End namespace