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
}
