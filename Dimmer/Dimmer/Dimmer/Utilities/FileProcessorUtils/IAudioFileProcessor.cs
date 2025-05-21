using ATL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Utilities.FileProcessorUtils;
   public interface IAudioFileProcessor
    {
        Task<List<FileProcessingResult>> ProcessFilesAsync(IEnumerable<string> filePaths);
        Task<FileProcessingResult> ProcessFileAsync(string filePath);
    }
