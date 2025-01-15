using Android.App;
using Android.Content;
using Android.Media;
using Android.Net.Wifi;
using Android.OS;
using Android.Media.Session;
using Android.Graphics;
using Activity = Android.App.Activity;
using Application = Android.App.Application;
using Android.Content.PM;
using Uri = Android.Net.Uri;
using Exception = System.Exception;


namespace Dimmer_MAUI.Platforms.Android.MAudioLib;
public class MediaPlayerServiceBinder : Binder
{
    private readonly MediaPlayerService service;

    public MediaPlayerServiceBinder(MediaPlayerService service)
    {
        this.service = service;
    }

    public MediaPlayerService GetMediaPlayerService()
    {
        return service;
    }
}
