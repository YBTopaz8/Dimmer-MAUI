using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Data.ModelView;

public class AudioOutputDevice
{

    public string? Id { get; set; }
    public string? Type { get; set; }
    public string? ProductName { get; set; }
    public bool? IsSource { get; set; }


    public string? Name { get; set; }
}