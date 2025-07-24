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