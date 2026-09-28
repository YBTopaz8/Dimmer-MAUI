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
                System.Diagnostics.Debug.WriteLine($"[AudioRouting] Detected new audio hardware: {device.ProductName}");

                RxSchedulers.Background.ScheduleTo(async () =>
                {
                    try
                    {
                     
                        await Task.Delay(1000);

                        var devices = await _audioService.GetAllAudioDevicesAsync();
                        var target = devices?.FirstOrDefault(d => d.IsDefaultDevice) ?? devices?.FirstOrDefault();

                        if (target != null)
                        {
                            // Pause -> Switch -> Resume prevents native crashes
                            bool wasPlaying = _audioService.IsPlaying;
                            if (wasPlaying) await _audioService.PauseAsync();

                            _audioService.SetPreferredOutputDevice(target);

                            if (wasPlaying) await _audioService.PlayAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AudioRouting] Error adding device: {ex}");
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
        RxSchedulers.Background.ScheduleTo(async () =>
        {
            try
            {
                
                await _audioService.PauseAsync();

                var devices = await _audioService.GetAllAudioDevicesAsync();
                var fallbackTarget = devices?.FirstOrDefault(d => d.IsDefaultDevice) ?? devices?.FirstOrDefault();

                if (fallbackTarget != null)
                {
                    _audioService.SetPreferredOutputDevice(fallbackTarget);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AudioRouting] Error switching after removal: {ex.Message}");
            }
        });
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
