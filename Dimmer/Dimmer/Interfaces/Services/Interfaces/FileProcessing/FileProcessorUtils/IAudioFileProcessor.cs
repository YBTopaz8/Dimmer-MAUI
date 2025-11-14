
namespace Dimmer.Interfaces.Services.Interfaces.FileProcessing.FileProcessorUtils;
public interface IAudioFileProcessor
{
    FileProcessingResult ProcessFile(string filePath);
    Task<List<FileProcessingResult>> ProcessFilesAsync(IEnumerable<string> filePaths);
}
