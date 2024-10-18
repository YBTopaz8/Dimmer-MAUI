using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer_MAUI.Utilities.IServices;

public interface IDiscordRPC
{
    void Initialize();
    void UpdatePresence(SongsModelView song, TimeSpan duration, TimeSpan position); //add a pause method
    void ClearPresence();
    void ShutDown();
}
