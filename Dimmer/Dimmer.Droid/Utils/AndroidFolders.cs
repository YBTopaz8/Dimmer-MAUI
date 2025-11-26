using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Application = Android.App.Application;
using Environment = Android.OS.Environment;

namespace Dimmer.Utils;


public static class AndroidFolders
{
    // Replacement for FileSystem.AppDataDirectory
    // Path: /data/user/0/com.yvanbrunel.dimmer/files
    public static string AppDataDirectory =>
        Application.Context.FilesDir?.AbsolutePath ?? "";

    // Replacement for FileSystem.CacheDirectory
    // Path: /data/user/0/com.yvanbrunel.dimmer/cache
    public static string CacheDirectory =>
        Application.Context.CacheDir?.AbsolutePath ?? "";

    // Public Music Folder (Standard Android Music folder)
    // Path: /storage/emulated/0/Music
    public static string PublicMusicDirectory =>
        Environment.GetExternalStoragePublicDirectory(Environment.DirectoryMusic)?.AbsolutePath ?? "";

    // Get path for a specific filename in AppData
    public static string GetPathInAppData(string filename) =>
        Path.Combine(AppDataDirectory, filename);
}
