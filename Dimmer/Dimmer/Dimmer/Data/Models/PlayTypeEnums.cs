using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Data.Models;
public enum PlayTypeEnums
{
    Play = 0,
    Pause = 1,
    Resume = 2,
    Completed = 3,
    Seeked = 4,
    Skipped = 5,
    Restarted = 6,
    SeekRestarted = 7,
    CustomRepeat = 8,
    Previous = 9
}
