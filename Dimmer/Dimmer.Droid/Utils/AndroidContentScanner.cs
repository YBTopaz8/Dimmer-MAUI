using Android.Database;
using Android.Provider;

using AndroidX.DocumentFile.Provider;

using Microsoft.Win32.SafeHandles;

using Application = Android.App.Application;
using Environment = Android.OS.Environment;
using Path = System.IO.Path;
using Uri = Android.Net.Uri;

namespace Dimmer.Utils;


public static class AndroidContentScanner
{

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
    public static void Initialize()
    {
        TaggingUtils.PlatformSpecificScanner = ScanAndroidUri;

        TaggingUtils.PlatformSpecificFileValidator = IsValidContentUri;
        TaggingUtils.PlatformFileExistsHook = ContentUriExists;

        TaggingUtils.PlatformGetFileSizeHook = GetContentFileSize;
        TaggingUtils.PlatformGetStreamHook = GetSeekableStream;
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
            var boolVal = cursor != null && cursor.MoveToFirst();
            if (boolVal)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        catch
        {
            return false;
        }
    }
    public static Stream GetSeekableStream(string contentUriString)
    {
        var uri = Android.Net.Uri.Parse(contentUriString);
        var context = Android.App.Application.Context;

        // Open as read-only ("r")
        var pfd = context.ContentResolver.OpenFileDescriptor(uri, "r");

        if (pfd != null)
        {
            int fd = pfd.DetachFd();
            pfd.Close();
            var safeHandle = new SafeFileHandle((nint)fd, ownsHandle: true);
            return new FileStream(safeHandle, FileAccess.Read);
        }
        return null;
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
    private static long GetContentFileSize(string uriString)
    {
        ICursor? cursor = null;
        try
        {
            var uri = Android.Net.Uri.Parse(uriString);
            var context = Android.App.Application.Context;

            // We only need the Size column
            string[] projection = { OpenableColumns.Size };

            cursor = context.ContentResolver?.Query(uri, projection, null, null, null);

            if (cursor != null && cursor.MoveToFirst())
            {
                int sizeIndex = cursor.GetColumnIndex(OpenableColumns.Size);
                // Return the size, or 0 if column invalid
                return (sizeIndex != -1) ? cursor.GetLong(sizeIndex) : 0;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting content size: {ex.Message}");
        }
        finally
        {
            cursor?.Close();
        }

        return 0;
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
