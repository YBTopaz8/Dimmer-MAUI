﻿namespace Dimmer.Utilities;
public class DeviceStaticUtils
{
    // Helper to get a unique ID for the current physical device
    public static string GetCurrentDeviceId()
    {
        return DeviceInfo.Current.Idiom == DeviceIdiom.Desktop ?
               (DeviceInfo.Current.Name + "_" + DeviceInfo.Current.Model + "_" + System.Net.Dns.GetHostName()).Replace(" ", "_") :
               (DeviceInfo.Current.Platform.ToString() + "_" + DeviceInfo.Current.VersionString); // DeviceInfo.Id can be useful if available & stable
    }

    public static ArtistModelView? SelectedArtistOne { get; set; }
    public static SongModelView SelectedSongOne { get; set; }
    public static ArtistModelView? SelectedArtistTwo { get; set; }

}
