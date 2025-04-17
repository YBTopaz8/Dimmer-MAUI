using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Interfaces;
public interface ISettingsService
{
    RepeatMode RepeatMode { get; set; }
    bool ShuffleOn { get; set; }
    double VolumeLevel { get; set; }
    string LastPlayedSong { get; set; }
    bool IsStickToTop { get; set; }
    IList<string> UserMusicFoldersPreference { get; }

    void AddMusicFolder(string path);
    bool RemoveMusicFolder(string path);
    void SetMusicFolders(IEnumerable<string> paths);
}
