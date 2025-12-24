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

        string outputFile = GenerateOutputPath(inputFile, "_trimmed", ".m4a");

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
    private string GenerateOutputPath(string input, string suffix, string extension)
    {
        string name = Path.GetFileNameWithoutExtension(input);
        string cache = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
        return Path.Combine(cache, $"{name}{suffix}{extension}");
    }
    public async Task<string> CreateInfiniteLoopAsync(string inputFile, TimeSpan duration, AudioFormat format, IProgress<double> progress)
    {
        await GuardEnsureFFmpeg();

        var settings = GetEncoderSettings(format);
        string outputFile = GenerateOutputPath(inputFile, "_1h_loop", settings.Extension);

        try
        {
            // stream_loop -1 (infinite)
            // -t duration
            // settings.EncoderArgs handles the re-encoding (e.g., to Opus)
            string args = $"-y -stream_loop -1 -i \"{inputFile}\" -t {duration.TotalSeconds} {settings.EncoderArgs} \"{outputFile}\"";

            var conversion = FFmpeg.Conversions.New();
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

    public async Task<string> CreateStoryVideoAsync(string imagePath, string audioPath, IProgress<double> progress)
    {
        await GuardEnsureFFmpeg();

        string outputFile = GenerateOutputPath(audioPath, "_story", ".mp4");

        // FFmpeg Command Logic:
        // -loop 1: Loop the input image infinitely
        // -i image: Input image
        // -i audio: Input audio
        // -c:v libx264: Use H.264 video codec (Standard for Social Media)
        // -tune stillimage: Optimization for static images (small file size)
        // -c:a aac: Audio codec
        // -b:a 192k: Audio bitrate
        // -pix_fmt yuv420p: Required pixel format for compatibility with players/Instagram
        // -shortest: Stop encoding when the shortest input (the audio) ends.
        // -vf scale...: Ensure even dimensions (required by some encoders) and reasonable size (1080p width)

        string args = $"-y -loop 1 -i \"{imagePath}\" -i \"{audioPath}\" " +
                      $"-c:v libx264 -tune stillimage -c:a aac -b:a 192k " +
                      $"-pix_fmt yuv420p -vf \"scale=1080:-2\" " +
                      $"-shortest \"{outputFile}\"";

        try
        {
            var conversion = FFmpeg.Conversions.New();
            progress?.Report(10);
            await conversion.Start(args);
            progress?.Report(100);
            return outputFile;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Video Gen Error: {ex}");
            throw;
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

    public async Task<string> ApplyAudioEffectsAsync(string inputFile, AudioEffectOptions options, AudioFormat format, IProgress<double> progress)
    {
        await GuardEnsureFFmpeg();

        var settings = GetEncoderSettings(format);

        // Create suffix based on effects + format
        string suffix = "_edited";
        if (options.Speed != 1.0) suffix += "_speed";
        if (options.EnableReverb) suffix += "_reverb";

        string outputFile = GenerateOutputPath(inputFile, suffix, settings.Extension);

        // --- CONSTRUCT FILTER CHAIN ---
        var filters = new List<string>();

        // 1. Speed & Pitch (Resampling method links them naturally for that "tape stop" effect)
        // 44100 is standard sample rate. Adjusting it changes speed+pitch together.
        if (Math.Abs(options.Speed - 1.0) > 0.01)
        {
            // "asetrate=44100*0.8,aresample=44100" -> Slows down and pitches down
            filters.Add($"asetrate=44100*{options.Speed},aresample=44100");
        }

        // 2. Reverb (Simple Echo filter as Reverb)
        // 0.8:0.9 -> Input gain : Output gain
        // 1000 -> Delay in ms (1 second delay makes it spacious)
        // 0.3 -> Decay (0.3 is decent tail)
        if (options.EnableReverb)
        {
            filters.Add("aecho=0.8:0.88:60:0.4");
        }

        // 3. Volume
        if (Math.Abs(options.VolumeGain - 1.0) > 0.01)
        {
            filters.Add($"volume={options.VolumeGain}");
        }

        // Join filters with comma
        string filterString = string.Join(",", filters);

        // Safety check: if no filters, just copy
        string args;
        if (string.IsNullOrEmpty(filterString))
        {
            args = $"-y -i \"{inputFile}\" -c copy \"{outputFile}\"";
        }
        else
        {
            // Note: Filters require re-encoding. We use libmp3lame (standard mp3 encoder)
            // -q:a 2 is high quality variable bitrate
             args = $"-y -i \"{inputFile}\" -af \"{filterString}\" {settings.EncoderArgs} \"{outputFile}\"";
        }

        try
        {
            var conversion = FFmpeg.Conversions.New();
            progress?.Report(10); // Fake start progress
            await conversion.Start(args);
            progress?.Report(100);
            return outputFile;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Effects Error]: {ex}");
            throw;
        }
    }

    public async Task<string> RemoveSectionAsync(string inputFile, TimeSpan cutStart, TimeSpan cutEnd, IProgress<double> progress)
    {
        await GuardEnsureFFmpeg();

        // 1. Get total duration
        var mediaInfo = await FFmpeg.GetMediaInfo(inputFile);
        var totalDuration = mediaInfo.Duration;

        // 2. Define temp paths
        string part1Path = GenerateOutputPath(inputFile, "_part1", ".m4a"
            );
        string part2Path = GenerateOutputPath(inputFile, "_part2", ".m4a");
        string finalPath = GenerateOutputPath(inputFile, "_cut", ".m4a");

        try
        {
            // 3. Create Part A (Start -> CutStart)
            // -ss 0 -t cutStart
            string args1 = $"-y -i \"{inputFile}\" -ss 0 -t {cutStart.TotalSeconds} -c copy -avoid_negative_ts 1 \"{part1Path}\"";

            // 4. Create Part B (CutEnd -> End)
            // -ss cutEnd
            string args2 = $"-y -i \"{inputFile}\" -ss {cutEnd.TotalSeconds} -c copy -avoid_negative_ts 1 \"{part2Path}\"";

            var conversion = FFmpeg.Conversions.New();

            progress?.Report(20); // Started
            await conversion.Start(args1); // Process Part 1

            progress?.Report(50); // Halfway
            await conversion.Start(args2); // Process Part 2

            // 5. Merge A + B
            // We reuse your existing Merge Logic here manually to avoid circular dependencies if you like,
            // or just write the concat list logic directly here for speed.

            string listFile = Path.Combine(Path.GetTempPath(), $"concat_{Guid.NewGuid()}.txt");
            await File.WriteAllLinesAsync(listFile, new[] { $"file '{part1Path}'", $"file '{part2Path}'" });

            string argsMerge = $"-f concat -safe 0 -i \"{listFile}\" -c copy \"{finalPath}\"";
            await conversion.Start(argsMerge);

            // Cleanup
            if (File.Exists(listFile)) File.Delete(listFile);
            if (File.Exists(part1Path)) File.Delete(part1Path);
            if (File.Exists(part2Path)) File.Delete(part2Path);

            progress?.Report(100);
            return finalPath;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Cut Error: {ex}");
            return null;
        }
    }

    // NERD FEATURE: 8D Audio (Spatial Panning)
    // Makes the audio circle around your head. Very popular for "immersiveness".
    public async Task<string> Apply8DAudioAsync(string inputFile, IProgress<double> progress)
    {
        await GuardEnsureFFmpeg();
        string outputFile = GenerateOutputPath(inputFile, "_8D", ".m4a");

        // apulsator filter:
        // mode=sine: shape of the movement
        // hz=0.125: Speed of rotation (0.125hz = 1 rotation every 8 seconds)
        // amount=1: Full panning (Hard Left to Hard Right)
        string filter = "apulsator=mode=sine:hz=0.125:amount=1";

        string args = $"-y -i \"{inputFile}\" -af \"{filter}\" -c:a libmp3lame -q:a 2 \"{outputFile}\"";

        try
        {
            var conversion = FFmpeg.Conversions.New();
            progress?.Report(10);
            await conversion.Start(args);
            progress?.Report(100);
            return outputFile;
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"8D Error: {ex}"); throw; }
    }
    private (string Extension, string EncoderArgs) GetEncoderSettings(AudioFormat format)
    {
        switch (format)
        {
            case AudioFormat.Aac:
                // aac is built-in. -b:a 192k is excellent quality.
                // .m4a container is standard.
                return (".m4a", "-c:a aac -b:a 192k");

            case AudioFormat.Opus:
                // libopus is the best encoder.
                // .ogg is the standard container for Opus.
                return (".ogg", "-c:a libopus -b:a 128k -vbr on");

            case AudioFormat.Wav:
                // pcm_s16le is standard CD quality.
                return (".wav", "-c:a pcm_s16le");

            case AudioFormat.Mp3:
            default:
                // q:a 2 is variable bitrate (VBR) roughly ~190-250kbps
                return (".mp3", "-c:a libmp3lame -q:a 2");
        }
    }
}