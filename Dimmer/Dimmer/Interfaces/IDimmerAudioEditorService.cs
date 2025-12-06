using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Interfaces;

public interface IDimmerAudioEditorService
{
    Task<string> CreateInfiniteLoopAsync(string inputFile, TimeSpan duration, IProgress<double> progress);
    Task<bool> CreateOneHourLoopAsync(string inputFile, string outputFile);
    Task<bool> EnsureFFmpegLoadedAsync();
    Task<bool> MergeAudioFilesAsync(string[] inputFiles, string outputFile);
    //Task<string?> TrimAudioAsync(string inputFile, string outputFile, TimeSpan start, TimeSpan end);
    Task<string> TrimAudioAsync(string inputFile, TimeSpan start, TimeSpan end, IProgress<double> progress);
    Task<string> ApplyAudioEffectsAsync(string inputFile, AudioEffectOptions options, IProgress<double> progress);
    Task<string> RemoveSectionAsync(string inputFile, TimeSpan cutStart, TimeSpan cutEnd, IProgress<double> progress);
    Task<string> Apply8DAudioAsync(string inputFile, IProgress<double> progress);
}

// Simple options class to pass parameters
public class AudioEffectOptions
{
    public double Speed { get; set; } = 1.0; // 0.5 to 2.0
    public double Pitch { get; set; } = 1.0; // 0.5 to 2.0 (Linked to speed usually in FFmpeg unless using rubberband)
    public bool EnableReverb { get; set; }
    public double VolumeGain { get; set; } = 1.0; // 1.0 = 100%
}