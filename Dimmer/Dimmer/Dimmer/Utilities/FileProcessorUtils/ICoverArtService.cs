﻿using ATL;

namespace Dimmer.Utilities.FileProcessorUtils;
public interface ICoverArtService
{
    Task<string?> SaveOrGetCoverImageAsync(string audioFilePath, PictureInfo? embeddedPictureInfo);
    string? GetExistingCoverImageAsync(string audioFilePath);
}

public class CoverArtService : ICoverArtService
{
    private readonly ProcessingConfig _config;
    private static readonly string[] _supportedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp" }; // Add more if needed

    public CoverArtService()
    {

        _config = new ProcessingConfig();
    }
    public CoverArtService(ProcessingConfig config)
    {
        _config = config;
        // Ensure the base directory for covers exists
        if (!Directory.Exists(_config.CoverArtBasePath))
        {
            Directory.CreateDirectory(_config.CoverArtBasePath);
        }
    }

    private static string SanitizeFileName(string fileName)
    {
        return string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));
    }

    private static string? GetExtensionFromMimeType(string? mimeType)
    {
        return mimeType?.ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/jpg" => ".jpg",
            "image/png" => ".png",
            "image/gif" => ".gif",
            "image/bmp" => ".bmp",
            _ => null, // Fallback or decide default (e.g., ".png")
        };
    }

    public string? GetExistingCoverImageAsync(string audioFilePath)
    {

        if (string.IsNullOrWhiteSpace(audioFilePath))
            return null;

        string baseFileName = Path.GetFileNameWithoutExtension(audioFilePath);
        string sanitizedBaseFileName = SanitizeFileName(baseFileName);

        foreach (var ext in _supportedExtensions)
        {
            string potentialPath = Path.Combine(_config.CoverArtBasePath, sanitizedBaseFileName + ext);
            if (File.Exists(potentialPath))
            {
                return potentialPath;
            }
        }
        return null;
    }


    public async static Task<bool> SaveSongLyrics(string audioFilePath, string SyncLyrics)
    {
        if (string.IsNullOrEmpty(audioFilePath) || string.IsNullOrEmpty(audioFilePath))
        {
            return false;
        }
        try
        {

            string baseFileName = Path.GetFileNameWithoutExtension(audioFilePath);
            string fileFolderPath = Path.GetPathRoot(baseFileName);
            string sanitizedBaseFileName = SanitizeFileName(baseFileName);
            string? extension = ".lrc";
            string targetFilePath = Path.Combine(fileFolderPath, sanitizedBaseFileName + extension);
            await File.WriteAllTextAsync(targetFilePath, SyncLyrics);
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            return false;
        }
;
    }

    public async Task<string?> SaveOrGetCoverImageAsync(string audioFilePath, PictureInfo? embeddedPictureInfo)
    {
        if (string.IsNullOrWhiteSpace(audioFilePath))
            return null;

        // 1. Check if an image already exists for this file name
        string? existingPath = GetExistingCoverImageAsync(audioFilePath);
        if (existingPath != null)
        {
            return existingPath;
        }

        // 2. If no existing image and no embedded data, nothing to save
        if (embeddedPictureInfo?.PictureData == null || embeddedPictureInfo.PictureData.Length == 0)
        {
            return null;
        }

        // 3. Save the new image
        string baseFileName = Path.GetFileNameWithoutExtension(audioFilePath);
        string sanitizedBaseFileName = SanitizeFileName(baseFileName);
        string? extension = GetExtensionFromMimeType(embeddedPictureInfo.MimeType) ?? ".png"; // Default to .png if MIME type is unknown/unsupported
        string targetFilePath = Path.Combine(_config.CoverArtBasePath, sanitizedBaseFileName + extension);

        try
        {
            await File.WriteAllBytesAsync(targetFilePath, embeddedPictureInfo.PictureData);
            return targetFilePath;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error saving cover image for {audioFilePath}: {ex.Message}");
            // Consider proper logging
            return null;
        }
    }
}