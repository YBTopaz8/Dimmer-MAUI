namespace Dimmer.Utilities.FileProcessorUtils;
public interface IAudioFileProcessor
{
    List<FileProcessingResult> ProcessFiles(IEnumerable<string> filePaths);
    FileProcessingResult ProcessFile(string filePath);
}
