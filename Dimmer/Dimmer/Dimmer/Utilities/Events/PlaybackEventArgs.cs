using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Utilities.Events;
public class PlaybackEventArgs : EventArgs
{
    public SongModelView? MediaSong { get; set; }

}
