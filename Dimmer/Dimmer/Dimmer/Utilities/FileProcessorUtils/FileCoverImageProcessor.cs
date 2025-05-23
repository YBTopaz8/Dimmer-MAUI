using ATL;

namespace Dimmer.Utilities.FileProcessorUtils;
public static class FileCoverImageProcessor
{
    public static string SaveOrGetCoverImageToFilePath(string? fullfilePath, byte[]? imageData = null, bool isDoubleCheckingBeforeFetch = true)
    {
        // If fullfilePath is null, we can't generate a meaningful filename based on it.
        // If imageData is also null at this point, there's nothing to process.
        // If imageData is NOT null but fullfilePath IS null, how would we name it?
        // For this function's current design, fullfilePath is essential for naming.
        if (string.IsNullOrEmpty(fullfilePath))
        {
            Debug.WriteLine("SaveOrGetCoverImageToFilePath: fullfilePath is null or empty, cannot proceed to generate filename or extract image if needed.");
            return string.Empty;
        }

        string? determinedMimeType = null;
        string targetExtension = ".png"; // Default extension if not determined otherwise

        // 1. Try to get imageData if it's null
        if (imageData is null)
        {
            try
            {
                if (File.Exists(fullfilePath)) // Ensure the audio file exists before trying to read it
                {
                    Track song = new Track(fullfilePath);
                    var firstPicture = song.EmbeddedPictures?.FirstOrDefault();
                    if (firstPicture?.PictureData != null && firstPicture.PictureData.Length > 0)
                    {
                        determinedMimeType = firstPicture.MimeType?.ToLowerInvariant();
                        if (determinedMimeType == "image/jpeg" || determinedMimeType == "image/jpg")
                        {
                            imageData = firstPicture.PictureData;
                            targetExtension = ".jpg";
                        }
                        else if (determinedMimeType == "image/png")
                        {
                            imageData = firstPicture.PictureData;
                            targetExtension = ".png";
                        }
                        else
                        {
                            // Unsupported image type or no image found
                            Debug.WriteLine($"SaveOrGetCoverImageToFilePath: Embedded picture found but MIME type '{determinedMimeType}' is not supported or no picture data for {fullfilePath}.");
                            // imageData remains null
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"SaveOrGetCoverImageToFilePath: No embedded pictures found or picture data is empty for {fullfilePath}.");
                    }
                }
                else
                {
                    Debug.WriteLine($"SaveOrGetCoverImageToFilePath: Audio file not found at {fullfilePath}.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SaveOrGetCoverImageToFilePath: Error reading track data for {fullfilePath}: {ex.Message}");
                // imageData remains null
            }
        }
        else
        {
            // imageData was provided. We don't know its original type here.
            // The original code always saved as .png in this case.
            // We could try to infer from magic bytes if needed, but for now, let's assume PNG if provided directly without type info.
            // Or, if you know the type when passing imageData, you could pass the extension too.
            // For simplicity, if imageData is provided, we'll still default to .png for saving,
            // but the check below will look for existing .jpg or .png.
            // If you want to be more precise, you might need to pass the desired extension or mime type along with imageData.
        }

        if (imageData is null && !isDoubleCheckingBeforeFetch)
        {
            Debug.WriteLine($"SaveOrGetCoverImageToFilePath: No imageData provided or extracted, and not double checking for existing. File: {fullfilePath}");
            return string.Empty;
        }


        // 3. Define folder path using LocalApplicationData
        string folderPath;
        try
        {
            string appDataRoot = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appNameFolder = "DimmerDB"; // Your application's name
            string targetSubFolder = "CoverImagesDimmer";
            folderPath = Path.Combine(appDataRoot, appNameFolder, targetSubFolder);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SaveOrGetCoverImageToFilePath: Could not determine LocalApplicationData path. {ex.Message}");
            return string.Empty; // Cannot proceed without a valid base path
        }


        // 4. Sanitize filename and create full file paths for checking/saving
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fullfilePath); // Use this to avoid original extension issues
        string sanitizedFileName = string.Join("_", fileNameWithoutExtension.Split(Path.GetInvalidFileNameChars()));

        // Paths to check for existing files
        string existingPngPath = Path.Combine(folderPath, $"{sanitizedFileName}.png");
        string existingJpgPath = Path.Combine(folderPath, $"{sanitizedFileName}.jpg");

        // Path for the new file we might save (respects extracted extension)
        string targetFilePathToSave = Path.Combine(folderPath, $"{sanitizedFileName}{targetExtension}");


        // 5. Create directory if it doesn't exist
        if (!Directory.Exists(folderPath))
        {
            try
            {
                Directory.CreateDirectory(folderPath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SaveOrGetCoverImageToFilePath: Error creating directory {folderPath}: {ex.Message}");
                return string.Empty; // Cannot proceed if directory creation fails
            }
        }

        // 6. Double check if file already exists (if requested)
        if (isDoubleCheckingBeforeFetch)
        {
            // Check for the most likely target extension first, then others
            if (targetExtension == ".png" && File.Exists(existingPngPath))
            {
                Debug.WriteLine($"SaveOrGetCoverImageToFilePath: Found existing PNG: {existingPngPath}");
                return existingPngPath;
            }
            if (targetExtension == ".jpg" && File.Exists(existingJpgPath))
            {
                Debug.WriteLine($"SaveOrGetCoverImageToFilePath: Found existing JPG: {existingJpgPath}");
                return existingJpgPath;
            }
            // Fallback checks if the targetExtension didn't match an existing file
            if (File.Exists(existingPngPath))
            {
                Debug.WriteLine($"SaveOrGetCoverImageToFilePath: Found existing PNG (fallback check): {existingPngPath}");
                return existingPngPath;
            }
            if (File.Exists(existingJpgPath))
            {
                Debug.WriteLine($"SaveOrGetCoverImageToFilePath: Found existing JPG (fallback check): {existingJpgPath}");
                return existingJpgPath;
            }
        }

        // 7. If no image data at this point, we can't save a new file.
        if (imageData is null)
        {
            Debug.WriteLine($"SaveOrGetCoverImageToFilePath: No imageData to save for {fullfilePath}. Searched for existing but found none or not checking.");
            return string.Empty;
        }

        // 8. Save the file
        try
        {
            File.WriteAllBytes(targetFilePathToSave, imageData);
            Debug.WriteLine($"SaveOrGetCoverImageToFilePath: Saved image to: {targetFilePathToSave}");
            return targetFilePathToSave;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SaveOrGetCoverImageToFilePath: Error saving file {targetFilePathToSave}: {ex.Message}");
            return string.Empty;
        }
    }
}