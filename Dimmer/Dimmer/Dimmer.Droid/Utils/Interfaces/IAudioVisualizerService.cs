using Android.Media.Audiofx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Object = Java.Lang.Object;
using System.Threading.Tasks;

namespace Dimmer.Utils.Interfaces;

public interface IAudioVisualizerService
{
    event EventHandler<byte[]?>? WaveformDataAvailable;
    event EventHandler<byte[]?>? FftDataAvailable;
    void Start(int audioSessionId);
    void Stop();
    bool IsEnabled { get; }
}

public partial class AudioVisualizerService : Object, IAudioVisualizerService, Visualizer.IOnDataCaptureListener
{
    private Visualizer? _visualizer;
    public event EventHandler<byte[]?>? WaveformDataAvailable;
    public event EventHandler<byte[]?>? FftDataAvailable;

    public bool IsEnabled => _visualizer?.Enabled ?? false;

    public void Start(int audioSessionId)
    {
        Stop(); // Ensure any previous instance is stopped

        try
        {
            _visualizer = new Visualizer(audioSessionId);
            if (_visualizer.Enabled) // It might be enabled by default by some players
            {
                _visualizer.SetEnabled(false); // Disable to configure
            }

            _visualizer.SetCaptureSize(Visualizer.GetCaptureSizeRange()[1]); // Max capture size
            _visualizer.SetScalingMode(VisualizerScalingMode.Normalized); // Values 0-255

            // The rate is in mHz (milliHertz), so 10000 is 10Hz (10 times per second)
            int captureRate = Visualizer.MaxCaptureRate / 2; // Or a fixed rate like 20Hz (20000)
            _visualizer.SetDataCaptureListener(this, captureRate, true, true); // Waveform and FFT
            _visualizer.SetEnabled(true);
            System.Diagnostics.Debug.WriteLine("Visualizer started.");
        }
        catch (Java.Lang.Exception ex) // Visualizer can throw various exceptions
        {
            System.Diagnostics.Debug.WriteLine($"Visualizer Start Error: {ex.Message}");
            _visualizer?.Release();
            _visualizer = null;
        }
    }

    public void Stop()
    {
        if (_visualizer != null)
        {
            try
            {
                _visualizer.SetEnabled(false);
                _visualizer.Release(); // Crucial
            }
            catch (Java.Lang.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Visualizer Stop Error: {ex.Message}");
            }
            finally
            {
                _visualizer = null;
                System.Diagnostics.Debug.WriteLine("Visualizer stopped and released.");
            }
        }
    }

    // IOnDataCaptureListener Implementation
    public void OnFftDataCapture(Visualizer? visualizer, byte[]? fft, int samplingRate)
    {
        FftDataAvailable?.Invoke(this, fft);
    }

    public void OnWaveFormDataCapture(Visualizer? visualizer, byte[]? waveform, int samplingRate)
    {
        WaveformDataAvailable?.Invoke(this, waveform);
    }
}