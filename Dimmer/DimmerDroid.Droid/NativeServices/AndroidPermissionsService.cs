using Android;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using Application = Android.App.Application;

public static class AndroidPermissionsService
{
    public static string[] GetAudioStoragePermissions()
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu) // API 33+
            return new[] { Manifest.Permission.ReadMediaAudio };

        if (Build.VERSION.SdkInt >= BuildVersionCodes.R) // API 30-32
            return new[] { Manifest.Permission.ReadExternalStorage };

        return new[] { Manifest.Permission.ReadExternalStorage, Manifest.Permission.WriteExternalStorage };
    }

    public static string[] GetAppRequiredPermissions()
    {
        var permissions = new List<string>(GetAudioStoragePermissions());

        if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu) // API 33+
            permissions.Add(Manifest.Permission.PostNotifications);

        if (Build.VERSION.SdkInt >= BuildVersionCodes.S) // API 31+ (Android 12+)
            permissions.Add(Manifest.Permission.BluetoothConnect);

        return permissions.ToArray();
    }

    public static bool HasAudioStoragePermission()
    {
        var context = Application.Context;
        return GetAudioStoragePermissions()
            .All(p => ContextCompat.CheckSelfPermission(context, p) == Permission.Granted);
    }

    public static bool HasAllPermissions()
    {
        var context = Application.Context;
        return GetAppRequiredPermissions()
            .All(p => ContextCompat.CheckSelfPermission(context, p) == Permission.Granted);
    }

    public static void RequestAllPermissions(Activity activity, int requestCode = 99)
    {
        var missing = GetAppRequiredPermissions()
            .Where(p => ContextCompat.CheckSelfPermission(activity, p) != Permission.Granted)
            .ToArray();

        if (missing.Length > 0)
            ActivityCompat.RequestPermissions(activity, missing, requestCode);
    }

    public static List<string> QueryAndroidMediaStoreAudio(IReadOnlySet<string> supportedExtensions)
    {
        var results = new List<string>();
        if (!AndroidPermissionsService.HasAudioStoragePermission())
            return results;

        var normalizedExts = new HashSet<string>(
            supportedExtensions.Select(e => e.StartsWith('.') ? e : "." + e),
            StringComparer.OrdinalIgnoreCase);

        var uri = Android.Provider.MediaStore.Audio.Media.ExternalContentUri;
        if (uri == null) return results;

        string[] projection =
        {
        Android.Provider.MediaStore.Audio.Media.InterfaceConsts.Data
    };

        using var cursor = Android.App.Application.Context.ContentResolver?.Query(
            uri,
            projection,
            $"{Android.Provider.MediaStore.Audio.Media.InterfaceConsts.IsMusic} != 0",
            null,
            null);

        if (cursor == null) return results;

        int dataCol = cursor.GetColumnIndex(Android.Provider.MediaStore.Audio.Media.InterfaceConsts.Data);
        if (dataCol < 0) return results;

        while (cursor.MoveToNext())
        {
            var filePath = cursor.GetString(dataCol);
            if (!string.IsNullOrWhiteSpace(filePath) &&
                normalizedExts.Contains(Path.GetExtension(filePath)) &&
                File.Exists(filePath))
            {
                results.Add(filePath);
            }
        }

        return results;
    }

    internal static List<string> ScanAndroidFolderWithMediaStore(
    string folderOrUri,
    IReadOnlySet<string> normalizedExts)
    {
        var results = new List<string>();
        var context = Android.App.Application.Context;

        // 1. Handle direct file
        if (File.Exists(folderOrUri))
        {
            if (normalizedExts.Contains(Path.GetExtension(folderOrUri)))
                results.Add(folderOrUri);
            return results;
        }

        // 2. Normalize SAF/FolderPicker paths (e.g. "/tree/primary:Music" -> "/storage/emulated/0/Music")
        string? normalizedFolder = NormalizeAndroidFolderPath(folderOrUri);

        // 3. Query MediaStore (works on API 30-36 with READ_MEDIA_AUDIO, no MANAGE_EXTERNAL_STORAGE needed)
        var uri = Android.Provider.MediaStore.Audio.Media.ExternalContentUri;
        if (uri != null && AndroidPermissionsService.HasAudioStoragePermission())
        {
            string[] projection = { Android.Provider.MediaStore.Audio.Media.InterfaceConsts.Data };

            using var cursor = context.ContentResolver?.Query(uri, projection, null, null, null);
            if (cursor != null)
            {
                int dataCol = cursor.GetColumnIndex(Android.Provider.MediaStore.Audio.Media.InterfaceConsts.Data);
                if (dataCol >= 0)
                {
                    string? prefix = string.IsNullOrEmpty(normalizedFolder)
                        ? null
                        : normalizedFolder.TrimEnd('/') + "/";

                    // If user picked root "/storage/emulated/0", match everything under it
                    while (cursor.MoveToNext())
                    {
                        var filePath = cursor.GetString(dataCol);
                        if (string.IsNullOrWhiteSpace(filePath))
                            continue;

                        if (prefix != null &&
                            !filePath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                            continue;

                        if (normalizedExts.Contains(Path.GetExtension(filePath)) && File.Exists(filePath))
                            results.Add(filePath);
                    }
                }
            }
        }

        // 4. Fallback for app-private sandbox folders (e.g. FileSystem.AppDataDirectory)
        if (results.Count == 0 && !string.IsNullOrEmpty(normalizedFolder) && Directory.Exists(normalizedFolder))
        {
            try
            {
                foreach (var file in Directory.EnumerateFiles(normalizedFolder, "*", new EnumerationOptions
                {
                    IgnoreInaccessible = true,
                    RecurseSubdirectories = true
                }))
                {
                    if (normalizedExts.Contains(Path.GetExtension(file)))
                        results.Add(file);
                }
            }
            catch { /* Ignored */ }
        }

        return results;
    }

    internal static string? NormalizeAndroidFolderPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;

        // Handle SAF content URIs or "/tree/primary:Music/Rock" style strings
        if (path.StartsWith("content://", StringComparison.OrdinalIgnoreCase))
        {
            var decoded = Android.Net.Uri.Decode(path);
            var idx = decoded?.LastIndexOf("primary:", StringComparison.OrdinalIgnoreCase) ?? -1;
            if (idx >= 0)
            {
                var rel = decoded![(idx + "primary:".Length)..].TrimStart('/');
                return $"/storage/emulated/0/{rel}";
            }
            return null;
        }

        if (path.Contains("primary:", StringComparison.OrdinalIgnoreCase))
        {
            var idx = path.IndexOf("primary:", StringComparison.OrdinalIgnoreCase);
            var rel = path[(idx + "primary:".Length)..].TrimStart('/');
            return $"/storage/emulated/0/{rel}";
        }

        return path;
    }
}

