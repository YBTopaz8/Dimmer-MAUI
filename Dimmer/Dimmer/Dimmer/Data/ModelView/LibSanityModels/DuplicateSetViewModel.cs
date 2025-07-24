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