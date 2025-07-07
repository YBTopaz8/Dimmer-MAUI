namespace Dimmer.Utilities.FileProcessorUtils;
public interface IAudioFileProcessor
{
    Task<List<FileProcessingResult>> ProcessFiles(IEnumerable<string> filePaths);
    Task<FileProcessingResult> ProcessFile(string filePath);
}
