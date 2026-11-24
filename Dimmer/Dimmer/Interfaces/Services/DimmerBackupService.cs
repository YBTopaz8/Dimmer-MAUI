using Realms;

namespace Dimmer.Interfaces.Services;

public class RealmRestoreResult
{
    public bool Success => string.IsNullOrEmpty(ErrorMessage);
    public string? ErrorMessage { get; set; }
    public int SongsFound { get; set; }
    public int PlayEventsFound { get; set; }
    public bool RestartRequired { get; set; }

    public override string ToString() => Success
        ? (RestartRequired
            ? $"Backup validated ({SongsFound} songs, {PlayEventsFound} play events). Restart the app to finish restoring."
            : $"Restored {SongsFound} songs / {PlayEventsFound} play events.")
        : $"Restore failed: {ErrorMessage}";
}

public class DimmerBackupService
{
    private const string PendingSuffix = ".restore_pending";
    private const string RollbackSuffix = ".restore_rollback";

    private readonly IRealmFactory _realmFactory;

    public DimmerBackupService(IRealmFactory factory)
    {
        _realmFactory = factory;
    }

    public async Task<string?> ExportRawRealmFileAsync(IRealmFactory realmF, string? exportFolderUriOrPath = null)
    {
        string fileName = $"DimmerDB_{DateTime.Now:yyyy-MM-dd_HHmmss}.realm";
        string tempPath = Path.Combine(FileSystem.CacheDirectory, fileName);

        if (File.Exists(tempPath))
            File.Delete(tempPath);

        await Task.Run(() =>
        {
            using var realm = realmF.GetRealmInstance();
            var exportConfig = new RealmConfiguration(tempPath)
            {
                EncryptionKey = realm.Config is RealmConfiguration rc ? rc.EncryptionKey : null,
                SchemaVersion = realm.Config.SchemaVersion
            };
            realm.WriteCopy(exportConfig);
        }).ConfigureAwait(false);

        if (!string.IsNullOrEmpty(exportFolderUriOrPath) &&
            !exportFolderUriOrPath.StartsWith("content://", StringComparison.OrdinalIgnoreCase))
        {
            Directory.CreateDirectory(exportFolderUriOrPath);
            string destPath = Path.Combine(exportFolderUriOrPath, fileName);
            File.Copy(tempPath, destPath, overwrite: true);
            return destPath;
        }

        if (!string.IsNullOrEmpty(exportFolderUriOrPath) &&
            exportFolderUriOrPath.StartsWith("content://", StringComparison.OrdinalIgnoreCase))
        {
            TaggingUtils.PlatformSpecificStreamCopier?.Invoke(exportFolderUriOrPath, fileName, tempPath);
            return fileName;
        }

        return tempPath;
    }

    // --- RESTORE OPTION A: Staged Full-DB Swap (Exact 1:1 restore, applies on restart) ---
    public async Task<RealmRestoreResult> StageRealmRestoreAsync(Stream backupStream)
    {
        var result = new RealmRestoreResult { RestartRequired = true };

        string targetDbPath;
        byte[]? encryptionKey = null;
        ulong schemaVersion = 0;
        RealmConfiguration.MigrationCallbackDelegate? migrationCallback = null;

        using (var liveRealm = _realmFactory.GetRealmInstance())
        {
            targetDbPath = liveRealm.Config.DatabasePath;
            schemaVersion = liveRealm.Config.SchemaVersion;
            if (liveRealm.Config is RealmConfiguration rc)
            {
                encryptionKey = rc.EncryptionKey;
                migrationCallback = rc.MigrationCallback;
            }
        }

        string stagedPath = targetDbPath + PendingSuffix;

        try
        {
            // Clean any prior incomplete staged file + sidecars
            DeleteRealmFileAndSidecars(stagedPath);

            await using (var output = File.Create(stagedPath))
            {
                await backupStream.CopyToAsync(output).ConfigureAwait(false);
            }

            if (new FileInfo(stagedPath).Length == 0)
            {
                DeleteRealmFileAndSidecars(stagedPath);
                result.ErrorMessage = "Selected file is empty.";
                return result;
            }

            // Validate & migrate (if from an older schema) on a background thread
            await Task.Run(() =>
            {
                var testConfig = new RealmConfiguration(stagedPath)
                {
                    EncryptionKey = encryptionKey,
                    SchemaVersion = schemaVersion,
                    MigrationCallback = migrationCallback
                };

                using var testRealm = Realm.GetInstance(testConfig);
                result.SongsFound = testRealm.All<SongModel>().Count();
                result.PlayEventsFound = testRealm.All<DimmerPlayEvent>().Count();
            }).ConfigureAwait(false);

            // Remove .lock/.note/.management created during validation, keeping stagedPath itself
            DeleteSidecarsOnly(stagedPath);
            return result;
        }
        catch (Exception ex)
        {
            DeleteRealmFileAndSidecars(stagedPath);
            result.ErrorMessage = ex.Message;
            return result;
        }
    }

    /// <summary>
    /// Call ONCE at very early app startup BEFORE RealmFactory.GetRealmInstance() is ever called.
    /// Returns true if a pending restore was applied (so you can recalculate stats if needed).
    /// </summary>
    public static bool ApplyPendingRealmRestoreIfAny(string defaultRealmPath)
    {
        string stagedPath = defaultRealmPath + PendingSuffix;
        if (!File.Exists(stagedPath))
            return false;

        string rollbackPath = defaultRealmPath + RollbackSuffix;

        try
        {
            DeleteSidecarsOnly(stagedPath);
            DeleteSidecarsOnly(defaultRealmPath);

            // Keep a rollback copy of current DB while moving
            if (File.Exists(defaultRealmPath))
            {
                if (File.Exists(rollbackPath)) File.Delete(rollbackPath);
                File.Move(defaultRealmPath, rollbackPath, overwrite: true);
            }

            File.Move(stagedPath, defaultRealmPath, overwrite: true);

            if (File.Exists(rollbackPath))
                File.Delete(rollbackPath);

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ApplyPendingRealmRestoreIfAny failed: {ex}");

            // Attempt rollback so the user isn't left without a DB
            if (!File.Exists(defaultRealmPath) && File.Exists(rollbackPath))
            {
                try { File.Move(rollbackPath, defaultRealmPath, overwrite: true); } catch { }
            }
            return false;
        }
    }

    // --- RESTORE OPTION B (Optional): Live Merge without Restart / Safe Across OSes ---
    public async Task<RealmRestoreResult> MergeFromRealmBackupAsync(Stream backupStream)
    {
        var result = new RealmRestoreResult { RestartRequired = false };
        string tempImportPath = Path.Combine(FileSystem.CacheDirectory, $"DimmerImport_{Guid.NewGuid():N}.realm");

        try
        {
            await using (var output = File.Create(tempImportPath))
            {
                await backupStream.CopyToAsync(output).ConfigureAwait(false);
            }

            await Task.Run(async () =>
            {
                using var liveRealm = _realmFactory.GetRealmInstance();

                var importConfig = new RealmConfiguration(tempImportPath)
                {
                    SchemaVersion = liveRealm.Config.SchemaVersion
                };
                if (liveRealm.Config is RealmConfiguration rc)
                {
                    importConfig.EncryptionKey = rc.EncryptionKey;
                    importConfig.MigrationCallback = rc.MigrationCallback;
                }

                using var backupRealm = Realm.GetInstance(importConfig);

                // Snapshot values from backupRealm before writing to liveRealm
                var favKeys = backupRealm.All<SongModel>()
                    .Where(s => s.IsFavorite && s.TitleDurationKey != null)
                    .AsEnumerable()
                    .Select(s => s.TitleDurationKey!)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var backupEvents = backupRealm.All<DimmerPlayEvent>()
                    .AsEnumerable()
                    .Select(e => new
                    {
                        e.Id,
                        e.SongName,
                        e.ArtistName,
                        e.AlbumName,
                        e.CoverImagePath,
                        e.IsFav,
                        e.PlayType,
                        e.PlayTypeStr,
                        e.DatePlayed,
                        e.WasPlayCompleted,
                        e.PositionInSeconds,
                        e.EventDate,
                        e.DeviceName,
                        e.DeviceFormFactor,
                        e.DeviceModel,
                        e.DeviceManufacturer,
                        e.DeviceVersion,
                        TitleDurationKey = e.SongsLinkingToThisEvent.FirstOrDefault()?.TitleDurationKey
                    })
                    .ToList();

                await liveRealm.WriteAsync(() =>
                {
                    var songsByKey = liveRealm.All<SongModel>()
                        .AsEnumerable()
                        .Where(s => !string.IsNullOrEmpty(s.TitleDurationKey))
                        .GroupBy(s => s.TitleDurationKey!, StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

                    foreach (var key in favKeys)
                    {
                        if (songsByKey.TryGetValue(key, out var song))
                        {
                            song.IsFavorite = true;
                            result.SongsFound++;
                        }
                    }

                    foreach (var evt in backupEvents)
                    {
                        if (liveRealm.Find<DimmerPlayEvent>(evt.Id) != null)
                            continue;

                        var newEvt = new DimmerPlayEvent
                        {
                            Id = evt.Id,
                            SongName = evt.SongName,
                            ArtistName = evt.ArtistName,
                            AlbumName = evt.AlbumName,
                            CoverImagePath = evt.CoverImagePath,
                            IsFav = evt.IsFav,
                            PlayType = evt.PlayType,
                            PlayTypeStr = evt.PlayTypeStr,
                            DatePlayed = evt.DatePlayed,
                            WasPlayCompleted = evt.WasPlayCompleted,
                            PositionInSeconds = evt.PositionInSeconds,
                            EventDate = evt.EventDate,
                            DeviceName = evt.DeviceName,
                            DeviceFormFactor = evt.DeviceFormFactor,
                            DeviceModel = evt.DeviceModel,
                            DeviceManufacturer = evt.DeviceManufacturer,
                            DeviceVersion = evt.DeviceVersion
                        };

                        if (!string.IsNullOrEmpty(evt.TitleDurationKey) &&
                            songsByKey.TryGetValue(evt.TitleDurationKey, out var song))
                        {
                            newEvt.SongId = song.Id;
                            newEvt.SongName = song.Title;
                            newEvt.CoverImagePath = song.CoverImagePath;
                            song.PlayHistory.Add(newEvt);
                        }

                        liveRealm.Add(newEvt);
                        result.PlayEventsFound++;
                    }
                });
            }).ConfigureAwait(false);

            return result;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.Message;
            return result;
        }
        finally
        {
            DeleteRealmFileAndSidecars(tempImportPath);
        }
    }

    private static void DeleteRealmFileAndSidecars(string realmPath)
    {
        DeleteSidecarsOnly(realmPath);
        try { if (File.Exists(realmPath)) File.Delete(realmPath); } catch { }
    }

    private static void DeleteSidecarsOnly(string realmPath)
    {
        foreach (var ext in new[] { ".lock", ".note" })
        {
            try
            {
                var sidecar = realmPath + ext;
                if (File.Exists(sidecar)) File.Delete(sidecar);
            }
            catch { }
        }

        try
        {
            var mgmtDir = realmPath + ".management";
            if (Directory.Exists(mgmtDir)) Directory.Delete(mgmtDir, recursive: true);
        }
        catch { }
    }
}