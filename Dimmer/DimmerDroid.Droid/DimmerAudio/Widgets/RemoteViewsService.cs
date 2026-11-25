using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Widget;

namespace Dimmer.DimmerAudio.Widgets;

[BroadcastReceiver(Label = "Dimmer Compact Player", Exported = true)]
[IntentFilter(new[] { AppWidgetManager.ActionAppwidgetUpdate })]
[MetaData("android.appwidget.provider", Resource = "@xml/dimmer_compact_widget_info")]
public class DimmerCompactWidgetProvider : AppWidgetProvider
{
    public override void OnUpdate(Context? context, AppWidgetManager? appWidgetManager, int[]? appWidgetIds)
    {
        if (context == null) return;
        var audio = IPlatformApplication.Current?.Services.GetService<IDimmerAudioService>();
        DimmerWidgetUpdater.UpdateCompactWidgets(context, appWidgetManager, appWidgetIds, audio, null);
    }
}

[BroadcastReceiver(Label = "Dimmer Player + Queue", Exported = true)]
[IntentFilter(new[] { AppWidgetManager.ActionAppwidgetUpdate })]
[MetaData("android.appwidget.provider", Resource = "@xml/dimmer_queue_widget_info")]
public class DimmerQueueWidgetProvider : AppWidgetProvider
{
    public override void OnUpdate(Context? context, AppWidgetManager? appWidgetManager, int[]? appWidgetIds)
    {
        if (context == null) return;
        var audio = IPlatformApplication.Current?.Services.GetService<IDimmerAudioService>();
        DimmerWidgetUpdater.UpdateQueueWidgets(context, appWidgetManager, appWidgetIds, audio, null);
    }
}

[Service(Permission = "android.permission.BIND_REMOTEVIEWS", Exported = false)]
public partial class DimmerQueueRemoteViewsService : RemoteViewsService
{
    public override IRemoteViewsFactory OnGetViewFactory(Intent? intent) =>
        new DimmerQueueRemoteViewsFactory(ApplicationContext!);
}

internal partial class DimmerQueueRemoteViewsFactory : Java.Lang.Object, RemoteViewsService.IRemoteViewsFactory
{
    private readonly Context _context;
    private List<SongModelView> _items = new();

    public DimmerQueueRemoteViewsFactory(Context context) => _context = context;

    public void OnCreate() => LoadQueue();
    public void OnDataSetChanged() => LoadQueue();
    public void OnDestroy() => _items.Clear();

    private void LoadQueue()
    {
        var snapshot = DimmerWidgetUpdater.GetQueueSnapshot();

        // Fallback if widget was added/updated before DimmerCompatMediaService started
        if (snapshot.Count == 0)
        {
            var vm = IPlatformApplication.Current?.Services.GetService<BaseViewModel>();
            if (vm != null)
                snapshot = vm.PlaybackQueueSource.Items.ToList();
        }

        _items = snapshot.ToList();

    }

    public int Count => _items.Count;
    public bool HasStableIds => true;
    public int ViewTypeCount => 1;
    public long GetItemId(int position) => position;
    public RemoteViews? LoadingView => null;

    public RemoteViews GetViewAt(int position)
    {
        var rv = new RemoteViews(_context.PackageName, Resource.Layout.widget_queue_item);
        if (position < 0 || position >= _items.Count) return rv;

        var song = _items[position];
        rv.SetTextViewText(Resource.Id.widget_row_title, song.Title);
        rv.SetTextViewText(Resource.Id.widget_row_artist, song.OtherArtistsName ?? song.ArtistName);

        // Fill-in intent sends the tapped row index back to DimmerCompatMediaService
        var fillIn = new Intent();
        fillIn.PutExtra(DimmerCompatMediaService.ExtraQueueIndex, position);
        rv.SetOnClickFillInIntent(Resource.Id.widget_queue_row, fillIn);

        return rv;
    }
}

public static class DimmerWidgetUpdater
{
    private static volatile IReadOnlyList<SongModelView> _queueSnapshot = Array.Empty<SongModelView>();

    public static void SetQueueSnapshot(IReadOnlyList<SongModelView> items) =>
        _queueSnapshot = items;

    public static IReadOnlyList<SongModelView> GetQueueSnapshot() =>
        _queueSnapshot;
    public static void UpdateProgressOnly(Context context, IDimmerAudioService? audio)
    {
        var mgr = AppWidgetManager.GetInstance(context);
        if (mgr == null || audio == null) return;

        var song = audio.CurrentTrackMetadata;
        double duration = song?.DurationInSeconds > 0 ? song.DurationInSeconds : audio.Duration;
        int progress = duration > 0
            ? (int)Math.Clamp((audio.CurrentPosition / duration) * 1000.0, 0, 1000)
            : 0;

        var compactIds = mgr.GetAppWidgetIds(new ComponentName(context, Java.Lang.Class.FromType(typeof(DimmerCompactWidgetProvider))));
        if (compactIds is { Length: > 0 })
        {
            var rv = new RemoteViews(context.PackageName, Resource.Layout.widget_player_compact);
            rv.SetProgressBar(Resource.Id.widget_progress, 1000, progress, false);
            mgr.PartiallyUpdateAppWidget(compactIds, rv);
        }

        var queueIds = mgr.GetAppWidgetIds(new ComponentName(context, Java.Lang.Class.FromType(typeof(DimmerQueueWidgetProvider))));
        if (queueIds is { Length: > 0 })
        {
            var rv = new RemoteViews(context.PackageName, Resource.Layout.widget_player_queue);
            rv.SetProgressBar(Resource.Id.widget_progress, 1000, progress, false);
            mgr.PartiallyUpdateAppWidget(queueIds, rv);
        }
    }
    public static void UpdateAllWidgets(Context context, IDimmerAudioService? audio, Bitmap? coverArt, bool refreshQueueList = true)
    {
        var mgr = AppWidgetManager.GetInstance(context);
        if (mgr == null) return;

        var compactIds = mgr.GetAppWidgetIds(new ComponentName(context, Java.Lang.Class.FromType(typeof(DimmerCompactWidgetProvider))));
        if (compactIds is { Length: > 0 })
            UpdateCompactWidgets(context, mgr, compactIds, audio, coverArt);

        var queueIds = mgr.GetAppWidgetIds(new ComponentName(context, Java.Lang.Class.FromType(typeof(DimmerQueueWidgetProvider))));
        if (queueIds is { Length: > 0 })
            UpdateQueueWidgets(context, mgr, queueIds, audio, coverArt, refreshQueueList);
    }

    public static void NotifyQueueDataChanged(Context context)
    {
        var mgr = AppWidgetManager.GetInstance(context);
        if (mgr == null) return;

        var queueIds = mgr.GetAppWidgetIds(new ComponentName(context, Java.Lang.Class.FromType(typeof(DimmerQueueWidgetProvider))));
        if (queueIds is { Length: > 0 })
            mgr.NotifyAppWidgetViewDataChanged(queueIds, Resource.Id.widget_queue_list);
    }
    public static void UpdateCompactWidgets(Context context, AppWidgetManager? mgr, int[]? ids, IDimmerAudioService? audio, Bitmap? coverArt)
    {
        if (mgr == null || ids == null || ids.Length == 0) return;
        var views = BuildHeaderViews(context, Resource.Layout.widget_player_compact, audio, coverArt);
        mgr.UpdateAppWidget(ids, views);
    }

    public static void UpdateQueueWidgets(Context context, AppWidgetManager? mgr, int[]? ids, IDimmerAudioService? audio, Bitmap? coverArt, bool refreshQueueList=true)
    {

        if (mgr == null || ids == null || ids.Length == 0) return;



        var views = BuildHeaderViews(context, Resource.Layout.widget_player_queue, audio, coverArt);

        var adapterIntent = new Intent(context, typeof(DimmerQueueRemoteViewsService));
        adapterIntent.PutExtra(AppWidgetManager.ExtraAppwidgetId, ids[0]);
        adapterIntent.SetData(Android.Net.Uri.Parse(adapterIntent.ToUri(IntentUriType.Scheme)));
        views.SetRemoteAdapter(Resource.Id.widget_queue_list, adapterIntent);


        views.SetEmptyView(Resource.Id.widget_queue_list, Resource.Id.widget_queue_empty);

        // Row click template MUST use PendingIntentFlags.Mutable on Android 12+ so FillInIntent extras survive!
        var clickTemplateIntent = new Intent(context, typeof(DimmerCompatMediaService))
            .SetAction(DimmerCompatMediaService.ActionJumpQueue);
        var clickTemplate = BuildForegroundServicePendingIntent(context, 400, clickTemplateIntent, mutable: true);
        views.SetPendingIntentTemplate(Resource.Id.widget_queue_list, clickTemplate);

        mgr.UpdateAppWidget(ids, views);
        if (refreshQueueList)
        {

            mgr.NotifyAppWidgetViewDataChanged(ids, Resource.Id.widget_queue_list);
        }
    }

    private static RemoteViews BuildHeaderViews(Context context, int layoutId, IDimmerAudioService? audio, Bitmap? coverArt)
    {
        var rv = new RemoteViews(context.PackageName, layoutId);
        var song = audio?.CurrentTrackMetadata;
        bool isPlaying = audio?.IsPlaying ?? false;

        rv.SetTextViewText(Resource.Id.widget_title, song?.Title ?? "Not Playing");
        rv.SetTextViewText(Resource.Id.widget_artist, song?.OtherArtistsName ?? song?.ArtistName ?? "Dimmer");
        rv.SetImageViewResource(Resource.Id.widget_btn_play_pause, isPlaying ? Resource.Drawable.media3_icon_pause : Resource.Drawable.media3_icon_play);


        rv.SetImageViewResource(Resource.Id.widget_btn_prev, Resource.Drawable.media3_icon_previous);
        

        rv.SetImageViewResource(Resource.Id.widget_btn_next, Resource.Drawable.media3_icon_next);

        int white = Android.Graphics.Color.White.ToArgb();
        rv.SetInt(Resource.Id.widget_btn_prev, "setColorFilter", white);
        rv.SetInt(Resource.Id.widget_btn_play_pause, "setColorFilter", white);
        rv.SetInt(Resource.Id.widget_btn_next, "setColorFilter", white);

        double duration = song?.DurationInSeconds > 0 ? song.DurationInSeconds : (audio?.Duration ?? 0);
        int progress = duration > 0
            ? (int)Math.Clamp(((audio?.CurrentPosition ?? 0) / duration) * 1000.0, 0, 1000)
            : 0;
        rv.SetProgressBar(Resource.Id.widget_progress, 1000, progress, false);

        // Keep widget bitmap small (<= 160px) to avoid Android's 1MB RemoteViews Binder crash
        if (coverArt != null)
        {
            using var scaled = Bitmap.CreateScaledBitmap(coverArt, 160, 160, true);
            rv.SetImageViewBitmap(Resource.Id.widget_cover, scaled);
        }
        else
        {
            rv.SetImageViewResource(Resource.Id.widget_cover, Resource.Drawable.musicalbum);
        }

       
        // Wire buttons directly to DimmerCompatMediaService
        rv.SetOnClickPendingIntent(Resource.Id.widget_btn_prev,
            BuildForegroundServicePendingIntent(context, 101, new Intent(context, typeof(DimmerCompatMediaService)).SetAction(DimmerCompatMediaService.ActionPrev)));
        rv.SetOnClickPendingIntent(Resource.Id.widget_btn_play_pause,
            BuildForegroundServicePendingIntent(context, 102, new Intent(context, typeof(DimmerCompatMediaService)).SetAction(DimmerCompatMediaService.ActionPlayPause)));
        rv.SetOnClickPendingIntent(Resource.Id.widget_btn_next,
            BuildForegroundServicePendingIntent(context, 103, new Intent(context, typeof(DimmerCompatMediaService)).SetAction(DimmerCompatMediaService.ActionNext)));

        // Tapping cover/title opens the app
        var launchIntent = context.PackageManager?.GetLaunchIntentForPackage(context.PackageName!);
        if (launchIntent != null)
        {
            var flags = PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable;
            rv.SetOnClickPendingIntent(Resource.Id.widget_cover, PendingIntent.GetActivity(context, 104, launchIntent, flags));
        }

        return rv;
    }

    private static PendingIntent BuildForegroundServicePendingIntent(Context context, int requestCode, Intent intent, bool mutable = false)
    {
        var flags = PendingIntentFlags.UpdateCurrent |
                    (mutable ? PendingIntentFlags.Mutable : PendingIntentFlags.Immutable);

        return Build.VERSION.SdkInt >= BuildVersionCodes.O
            ? PendingIntent.GetForegroundService(context, requestCode, intent, flags)!
            : PendingIntent.GetService(context, requestCode, intent, flags)!;
    }
}