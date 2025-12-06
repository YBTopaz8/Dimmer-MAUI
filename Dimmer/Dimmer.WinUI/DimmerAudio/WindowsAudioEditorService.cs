using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

using Parse.Abstractions.Infrastructure;

using Xabe.FFmpeg.Downloader;

using ProgressBar = Microsoft.UI.Xaml.Controls.ProgressBar;


namespace Dimmer.WinUI.DimmerAudio;

public class WindowsAudioEditorService : IDimmerAudioEditorService
{
    private string _ffmpegPath;
    public WindowsAudioEditorService()
    {
        // Point this to where you ship ffmpeg.exe, or download it dynamically
        // FFmpeg.SetExecutablesPath(@"C:\Path\To\Dimmer\Binaries");
        _ffmpegPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Dimmer", "ffmpeg");
        if (!Directory.Exists(_ffmpegPath)) Directory.CreateDirectory(_ffmpegPath);
        FFmpeg.SetExecutablesPath(_ffmpegPath);
    }
    IProgress<int> progress;
    private async Task GuardEnsureFFmpeg()
    {
        bool ready = await EnsureFFmpegLoadedAsync();
        if (!ready)
        {
            throw new FileNotFoundException($"FFmpeg.exe was not found in {_ffmpegPath} and could not be downloaded. Please install manually.");
        }
    }
    public async Task<string> TrimAudioAsync(string inputFile, TimeSpan start, TimeSpan end, IProgress<double> progress)
    {
        await GuardEnsureFFmpeg();

        string outputFile = GenerateOutputPath(inputFile, "_trimmed");

        // Calculate duration
        TimeSpan duration = end - start;

        // RAW COMMAND CONSTRUCTION
        // -y: Overwrite output files without asking. (Fixes the crash if file exists)
        // -i: Input file
        // -ss: Start Position
        // -t: Duration
        // -c copy: Copy the stream directly. NO Re-encoding. (Instant speed, 1:1 quality)
        // -avoid_negative_ts 1: Helps with timestamp issues when cutting

        string args = $"-y -i \"{inputFile}\" -ss {start:c} -t {duration:c} -c copy -avoid_negative_ts 1 \"{outputFile}\"";

        try
        {
            var conversion = FFmpeg.Conversions.New();

            // We can't get exact progress percentage easily with raw commands on short files 
            // because FFmpeg outputs "size=" lines differently for Stream Copy.
            // We report 50% to show activity.
            progress?.Report(10);

            // Run the raw command
            await conversion.Start(args);

            progress?.Report(100);
            return outputFile;
        }
        catch (Xabe.FFmpeg.Exceptions.ConversionException cex)
        {
            // This will print the ACTUAL error from FFmpeg (not just the version header)
            System.Diagnostics.Debug.WriteLine($"[FFmpeg Crash Log]: {cex.InputParameters}");
            throw new Exception($"Trim failed. The output file might be locked or invalid.\nDetails: {cex.Message}", cex);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Trim Error]: {ex}");
            throw;
        }
    }

    public async Task<bool> EnsureFFmpegLoadedAsync()
    {
        try
        {
            // Check if ffmpeg.exe exists in the folder
            string exePath = Path.Combine(_ffmpegPath, "ffmpeg.exe");
            if (File.Exists(exePath)) return true;

            // Download if missing (this might take time)
            await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official, _ffmpegPath);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FFmpeg Setup Failed: {ex.Message}");
            return false;
        }
    }
    private string GenerateOutputPath(string input, string suffix)
    {
        string dir = Path.GetDirectoryName(input);
        string name = Path.GetFileNameWithoutExtension(input);
        string ext = Path.GetExtension(input);
        // Save to MyMusic or Cache, don't overwrite input
        string cache = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyMusic);
        
        return Path.Combine(cache, $"{name}{suffix}{ext}");
    }
    public async Task<string> CreateInfiniteLoopAsync(string inputFile, TimeSpan duration, IProgress<double> progress)
    {
        string outputFile = GenerateOutputPath(inputFile, "_1hour_loop");

        try
        {
           
            string args = $"-stream_loop -1 -i \"{inputFile}\" -t {duration.TotalSeconds} -c:a copy \"{outputFile}\"";

            // We use a raw conversion for specific flags
            var conversion = FFmpeg.Conversions.New();

            // Fake progress for raw command (hard to track exact stream_loop progress without parsing stderr)
            progress?.Report(10);

            await conversion.Start(args);

            progress?.Report(100);
            return outputFile;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Loop Error: {ex}");
            return null;
        }
    }

    public async Task<bool> CreateOneHourLoopAsync(string inputFile, string outputFile)
    {
        try
        {
            // To make a 1 hour version, we use the stream_loop input option
            // -stream_loop -1 (infinite) but mapped to a time duration with -t 3600

            // Xabe might not support complex filter chains easily, so raw command string is often better for specific logic
            string args = $"-stream_loop -1 -i \"{inputFile}\" -t 3600 -c:a copy \"{outputFile}\"";

            // Note: -c:a copy is fast (no re-encoding) but might have clicks at loop points if the source isn't perfect.
            // If clicks occur, remove "-c:a copy" to re-encode (slower).

            var conversion = FFmpeg.Conversions.New();
            await conversion.Start(args);

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"FFmpeg Loop Error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> MergeAudioFilesAsync(string[] inputFiles, string outputFile)
    {
        // 1. Create a text file list for ffmpeg concat demuxer
        string listFile = Path.Combine(Path.GetTempPath(), "dimmer_concat_list.txt");
        var lines = inputFiles.Select(f => $"file '{f}'");
        await File.WriteAllLinesAsync(listFile, lines);

        try
        {
            // ffmpeg -f concat -safe 0 -i list.txt -c copy output.mp3
            string args = $"-f concat -safe 0 -i \"{listFile}\" -c copy \"{outputFile}\"";

            var conversion = FFmpeg.Conversions.New();
            await conversion.Start(args);

            return true;
        }
        finally
        {
            if (File.Exists(listFile)) File.Delete(listFile);
        }
    }
}