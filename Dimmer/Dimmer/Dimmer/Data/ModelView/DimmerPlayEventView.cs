using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Data.ModelView;
public class DimmerPlayEventView
{
    public ObjectId Id { get; set; } // Or public string Id if you prefer for display
    public bool IsNewOrModified { get; set; } // If needed by UI
    public string? SongName { get; set; }


    public ObjectId? SongId { get; set; }
    public int PlayType { get; set; }
    public string? PlayTypeStr { get; set; }
    public DateTimeOffset DatePlayed { get; set; }
    public DateTimeOffset DateFinished { get; set; }
    public bool WasPlayCompleted { get; set; }
    public double PositionInSeconds { get; set; }
    public DateTimeOffset? EventDate { get; set; }
    public string? DeviceName { get; set; }
    public string? DeviceFormFactor { get; set; }
    public string? DeviceModel { get; set; }
    public string? DeviceManufacturer { get; set; }
    public string? DeviceVersion { get; set; }

}
public partial class PlayEventGroup : ObservableCollection<DimmerPlayEventView>
{
    public string Name { get; private set; }

    public PlayEventGroup(string name, IEnumerable<DimmerPlayEventView> events) : base(events)
    {
        Name = name;
    }

    public override string ToString() => Name;
}