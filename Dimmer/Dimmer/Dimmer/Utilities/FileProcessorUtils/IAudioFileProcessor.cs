namespace Dimmer.Utilities.FileProcessorUtils;
   public interface IAudioFileProcessor
    {
        Task<List<FileProcessingResult>> ProcessFilesAsync(IEnumerable<string> filePaths);
        Task<FileProcessingResult> ProcessFileAsync(string filePath);
    }
