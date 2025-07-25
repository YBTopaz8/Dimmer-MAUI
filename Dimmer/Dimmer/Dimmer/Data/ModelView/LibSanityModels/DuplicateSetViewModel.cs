using Dimmer.Interfaces.Services.Interfaces.FileProcessing;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Data.ModelView.LibSanityModels;

public partial class DuplicateSetViewModel : ObservableObject
{
    // The properties that make this a duplicate set
    public string Title { get; }
    public double DurationInSeconds { get; }

    // The list of individual song items in this set
    public ObservableCollection<DuplicateItemViewModel> Items { get; } = new();

    [ObservableProperty]
    public partial bool IsResolved { get; set; } = false; // To hide it from the UI after processing

    public DuplicateSetViewModel(string title, double duration)
    {
        Title = title;
        DurationInSeconds = duration;
    }
}

// In a Models folder, e.g., Dimmer.Data.ModelView.LibSanityModels
public class LibraryValidationResult
{
    /// <summary>
    /// A list of songs that were found in the database but whose
    /// corresponding file no longer exists on disk.
    /// </summary>
    public List<SongModelView> MissingSongs { get; init; } = new();

    /// <summary>
    /// The total number of songs that were scanned.
    /// </summary>
    public int ScannedCount { get; init; }

    /// <summary>
    /// A convenience property for the count of missing songs.
    /// </summary>
    public int MissingCount => MissingSongs.Count;
}

/// <summary>
/// Represents the outcome of reconciling the library database with the file system.
/// </summary>
public class LibraryReconciliationResult
{
    /// <summary>
    /// A record of songs whose data was successfully migrated from an old,
    /// missing file path to a new, existing one.
    /// </summary>
    public List<MigrationDetail> MigratedSongs { get; init; } = new();

    /// <summary>
    /// A list of songs that were missing and for which NO suitable replacement
    /// file could be found. These are true "ghost" entries.
    /// </summary>
    public List<SongModelView> UnresolvedMissingSongs { get; init; } = new();

    public int ScannedCount { get; init; }
    public int MigratedCount => MigratedSongs.Count;
    public int UnresolvedCount => UnresolvedMissingSongs.Count;
}

/// <summary>
/// A small record to hold the details of a single data migration.
/// </summary>
/// <param name="From">The old song entry whose file is missing.</param>
/// <param name="To">The new song entry that received the old data.</param>
public record MigrationDetail(SongModelView From, SongModelView To);