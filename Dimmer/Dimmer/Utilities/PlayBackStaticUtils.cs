namespace Dimmer.Utilities;
public static class PlayBackStaticUtils
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

        //TODO: SET THIS AS PREFERENCE FOR USERS

        string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments), "DimmerDB", "CoverImagesDimmer");

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


    public static byte[]? GetCoverImage(string? filePath, bool isToGetByteArrayImages)
    {
        Track LoadTrack = new Track(filePath);
        byte[]? coverImage = null;

        if (LoadTrack.EmbeddedPictures?.Count > 0)
        {
            string? mimeType = LoadTrack.EmbeddedPictures?.FirstOrDefault()?.MimeType;
            if (mimeType == "image/jpg" || mimeType == "image/jpeg" || mimeType == "image/png")
            {
                coverImage = LoadTrack.EmbeddedPictures?.FirstOrDefault()?.PictureData;
            }
        }


        if (coverImage is null || coverImage.Length < 1)
        {
            string fileNameWithoutExtension = Path.GetFileName(filePath);
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DimmerDB", "CoverImagesDimmer");
            // Ensure the directory exists
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }


            string[] imageFiles =
            [
                .. Directory.GetFiles(folderPath, $"{fileNameWithoutExtension}.jpg", SearchOption.TopDirectoryOnly)
,
                .. Directory.GetFiles(folderPath, $"{fileNameWithoutExtension}.jpeg", SearchOption.TopDirectoryOnly),
                .. Directory.GetFiles(folderPath, $"{fileNameWithoutExtension}.png", SearchOption.TopDirectoryOnly),
            ];

            if (imageFiles.Length > 0)
            {
                coverImage = File.ReadAllBytes(imageFiles[0]);

                return coverImage;
            }
        }

        return coverImage;
    }
}
