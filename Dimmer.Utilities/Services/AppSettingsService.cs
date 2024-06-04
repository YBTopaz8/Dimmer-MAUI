using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Utilities.IServices;
public class AppSettingsService : IAppSettingsService
{
    public static class VolumeSettings
    {
        const double defaultVolume = 1;
        const double maxVolume = 1.0;
        const double minVolume = 0.0;

        public static double Volume
        {
            get => Preferences.Default.Get(nameof(Volume), defaultVolume);
            set => Preferences.Default.Set(nameof(Volume), Math.Clamp(value, minVolume, maxVolume));
        }

        public static void SetVolumeLevel(double newValue)
        {
            if (newValue != defaultVolume)
            {
                Volume = newValue;
            }
        }
        public static double GetVolumeLevel()
        {
            return Volume;
        }

    }
}
