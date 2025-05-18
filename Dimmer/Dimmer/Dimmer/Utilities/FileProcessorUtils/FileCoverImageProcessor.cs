using ATL;

namespace Dimmer.Utilities.FileProcessorUtils;
public static class FileCoverImageProcessor
{
    public static string SaveOrGetCoverImageToFilePath(string? fullfilePath, byte[]? imageData = null, bool isDoubleCheckingBeforeFetch = true)
    {
        if (imageData is null)
        {
            Track song = new Track(fullfilePath);
            string? mimeType = song.EmbeddedPictures?.FirstOrDefault()?.MimeType;
            if (mimeType == "image/jpg" || mimeType == "image/jpeg" || mimeType == "image/png")
            {
                imageData = song.EmbeddedPictures?.FirstOrDefault()?.PictureData;
            }
        }
        if (fullfilePath is null)
        {
            return string.Empty;
        }
        string fileNameWithExtension = Path.GetFileName(fullfilePath);

        string sanitizedFileName = string.Join("_", fileNameWithExtension.Split(Path.GetInvalidFileNameChars()));
        string folderPath =string.Empty;
        folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments), "DimmerDB", "CoverImagesDimmer");
        //folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CoverImagesDimmer");

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }
        string filePath = Path.Combine(folderPath, $"{sanitizedFileName}.png");
        string filePathjpg = Path.Combine(folderPath, $"{sanitizedFileName}.jpg");


        if (isDoubleCheckingBeforeFetch)
        {
            if (File.Exists(filePath))
            {
                return filePath;
            }
            if (File.Exists(filePathjpg))
            {
                return filePathjpg;
            }
        }
        if (imageData is null)
        {
            return string.Empty;
        }

        try
        {
            File.WriteAllBytes(filePath, imageData);
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Error saving file: " + ex.Message);
        }

        return filePath;
    }

}
