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

public class MediaSessionCallback : MediaSession.Callback
{
    //private Lazy<HomePageVM> ViewModel { get; set; }
    //private readonly MediaPlayerServiceBinder mediaPlayerService;
    //public MediaSessionCallback(MediaPlayerServiceBinder service)
    //{
    //    mediaPlayerService = service;
    //}

    //bool isPlaying = true;
    //public override void OnPause()
    //{
        
    //    mediaPlayerService.GetMediaPlayerService().SetPlayingChanged(false);
    //    base.OnPause();
    //    isPlaying = false;
    //}

    //public override void OnPlay()
    //{
    //    Console.WriteLine("Step 2 On Play Callback Method");
    //    mediaPlayerService.GetMediaPlayerService().SetPlayingChanged(true);
    //    base.OnPlay();
    //    isPlaying = true;
    //}

    //public override async void OnSkipToNext()
    //{
    //    Console.WriteLine("Step 1 Skip to next Callback Method");
    //    await mediaPlayerService.GetMediaPlayerService().PlayNext();
    //    base.OnSkipToNext();
    //}

    //public override void OnSkipToPrevious()
    //{
    //    mediaPlayerService.GetMediaPlayerService().PlayPrevious();
    //    base.OnSkipToPrevious();
    //}

    //public override async void OnStop()
    //{
    //    await mediaPlayerService.GetMediaPlayerService().Stop();
    //    base.OnStop();
    //}

    //public override async void OnSeekTo(long pos)
    //{
    //    await mediaPlayerService.GetMediaPlayerService().Seek((int)pos);
    //    Console.WriteLine("From OnSeek Seeking to " + pos);
    //    //base.OnSeekTo(pos);
    //}

    //public override void OnSetRating(Rating rating)
    //{
    //    base.OnSetRating(rating);
    //}

}
