using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.DimmerLive.Models;

public class CloudBackupModel
{
    public string ObjectId { get; set; }
    public string CreatedAtDisplay { get; set; }
    public string EventCountDisplay { get; set; }

    public CloudBackupModel(Parse.ParseObject parseObj)
    {
        ObjectId = parseObj.ObjectId;
        // Format date nicely
        CreatedAtDisplay = parseObj.CreatedAt?.ToLocalTime().ToString("g") ?? "Unknown Date";

        // Safely get the count, default to 0
        int count = parseObj.ContainsKey("eventCount") ? parseObj.Get<int>("eventCount") : 0;
        EventCountDisplay = $"{count} Events";
    }
}