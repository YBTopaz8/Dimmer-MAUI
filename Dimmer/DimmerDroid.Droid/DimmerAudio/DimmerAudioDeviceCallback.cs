using Android.Media;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dimmer.DimmerAudio;


public partial class DimmerAudioDeviceCallback : AudioDeviceCallback
{
    private readonly IDimmerAudioService _audioService;

    public DimmerAudioDeviceCallback(IDimmerAudioService audioService)
    {
        _audioService = audioService;
    }


    public override void OnAudioDevicesAdded(AudioDeviceInfo[]? addedDevices)
    {
        base.OnAudioDevicesAdded(addedDevices);

        if (addedDevices == null || addedDevices.Length == 0) return;

        foreach (var device in addedDevices)
        {
            if (IsHeadsetOrBluetooth(device))
            {
                System.Diagnostics.Debug.WriteLine($"[AudioRouting] Detected new audio hardware: {device.ProductName} ({device.Type})");

                // Switch back to the newly plugged headset!
                RxSchedulers.Background.ScheduleTo(async () =>
                {
                    // Refresh devices
                    var devices = await _audioService.GetAllAudioDevicesAsync();
                    var target = devices?.FirstOrDefault(d => d.IsDefaultDevice) ?? devices?.FirstOrDefault();

                    if (target != null)
                    {
                        _audioService.SetPreferredOutputDevice(target);
                    }
                });
                break;
            }
        }
    }


    public override void OnAudioDevicesRemoved(AudioDeviceInfo[]? removedDevices)
    {
        base.OnAudioDevicesRemoved(removedDevices);
        System.Diagnostics.Debug.WriteLine("[AudioRouting] Audio device removed.");
    }

    private static bool IsHeadsetOrBluetooth(AudioDeviceInfo device)
    {
        return device.Type is AudioDeviceType.WiredHeadset
                           or AudioDeviceType.WiredHeadphones
                           or AudioDeviceType.BluetoothA2dp
                           or AudioDeviceType.BluetoothSco
                           or AudioDeviceType.UsbHeadset
                           or AudioDeviceType.UsbDevice;
    }
}
