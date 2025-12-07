using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Android.Content;
using Android.Database;
using Android.Net;
using Android.Provider;

using AndroidX.DocumentFile.Provider;

using Application = Android.App.Application;
using Environment = Android.OS.Environment;
using Path = System.IO.Path;
using Uri = Android.Net.Uri;

namespace Dimmer.Utils;


public static class AndroidContentScanner
{
    public static void Initialize()
    {
        TaggingUtils.PlatformSpecificScanner = ScanAndroidUri;

        TaggingUtils.PlatformSpecificFileValidator = IsValidContentUri;
        TaggingUtils.PlatformFileExistsHook = ContentUriExists;

        TaggingUtils.PlatformGetStreamHook = GetContentStream;
    }
    private static bool ContentUriExists(string uriString)
    {
        try
        {
            var uri = Android.Net.Uri.Parse(uriString);
            var context = Android.App.Application.Context;

            // We just try to query for the ID column. If we get a row, it exists.
            string[] projection = { Android.Provider.IBaseColumns.Id };

            using var cursor = context.ContentResolver?.Query(uri, projection, null, null, null);
            return cursor != null && cursor.MoveToFirst();
        }
        catch
        {
            return false;
        }
    }
    private static Stream GetContentStream(string uriString)
    {
        try
        {
            var uri = Android.Net.Uri.Parse(uriString);
            var context = Android.App.Application.Context;

            // "r" = read-only. Use "w" if you plan to save tags back to the file.
            return context.ContentResolver.OpenInputStream(uri);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to open stream for URI: {ex.Message}");
            return null;
        }
    }
    private static bool IsValidContentUri(string uriString, IReadOnlySet<string> supportedExtensions)
    {
        ICursor? cursor = null;
        try
        {
            var uri = Android.Net.Uri.Parse(uriString);
            var context = Android.App.Application.Context;

            // Query only the columns we need: Name and Size
            string[] projection = {
                OpenableColumns.DisplayName,
                OpenableColumns.Size
            };

            cursor = context.ContentResolver.Query(uri, projection, null, null, null);

            if (cursor != null && cursor.MoveToFirst())
            {
                // 1. Check Size
                int sizeIndex = cursor.GetColumnIndex(OpenableColumns.Size);
                long size = (sizeIndex != -1) ? cursor.GetLong(sizeIndex) : 0;

                if (size <= 1024) return false;

                // 2. Check Extension via Display Name
                int nameIndex = cursor.GetColumnIndex(OpenableColumns.DisplayName);
                string fileName = (nameIndex != -1) ? cursor.GetString(nameIndex) : "";

                // Use System.IO.Path just to split the string safely
                string extension = Path.GetExtension(fileName);

                return !string.IsNullOrEmpty(extension) &&
                       supportedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
            }
        }
        catch
        {
            // Log error
            return false;
        }
        finally
        {
            cursor?.Close();
        }

        return false;
    }

    private static List<string> ScanAndroidUri(string uriString, IReadOnlySet<string> supportedExtensions)
    {
        var results = new List<string>();
        try
        {
            var context = Android.App.Application.Context;
            var treeUri = Uri.Parse(uriString);

            // This is safe here because this file is inside Platforms/Android
            DocumentFile? rootDir = DocumentFile.FromTreeUri(context, treeUri);

            if (rootDir != null && rootDir.CanRead())
            {
                Traverse(rootDir, results, supportedExtensions);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Android Scan Error: {ex.Message}");
        }
        return results;
    }

    private static void Traverse(DocumentFile dir, List<string> results, IReadOnlySet<string> supportedExtensions)
    {
        var files = dir.ListFiles();
        if(files == null) return;
        foreach (var file in files)
        {
            if (file.IsDirectory)
            {
                Traverse(file, results, supportedExtensions);
            }
            else
            {
                var name = file.Name;
                if (!string.IsNullOrEmpty(name))
                {
                    var ext = "." + name.Split('.').LastOrDefault();
                    if (supportedExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
                    {
                        // Return the playable URI
                        results.Add(file.Uri!.ToString()!);
                    }
                }
            }
        }
    }
}


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
