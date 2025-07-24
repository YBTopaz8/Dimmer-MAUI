using Dimmer.Interfaces.Services.Interfaces.FileProcessing;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Data.ModelView.LibSanityModels;


// DuplicateStatus.cs
public enum DuplicateStatus
{
    Original, // The one we think should be kept
    Duplicate // A candidate for deletion
}

// DuplicateAction.cs
public enum DuplicateAction
{
    Keep,
    Delete,
    Ignore // "Save aside"
}


public partial class DuplicateItemViewModel : ObservableObject
{
    // The actual song data
    public SongModelView Song { get; }

    // Is this the original or a duplicate? (For UI styling)
    public DuplicateStatus Status { get; }

    // What does the user want to do with this item?
    [ObservableProperty]
    public partial DuplicateAction Action { get; set; }

    public DuplicateItemViewModel(SongModelView song, DuplicateStatus status)
    {
        Song = song;
        Status = status;

        // Sensible defaults: Keep the original, mark duplicates for deletion.
        Action = status == DuplicateStatus.Original ? DuplicateAction.Keep : DuplicateAction.Delete;
    }
}