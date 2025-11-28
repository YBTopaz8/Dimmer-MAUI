namespace Dimmer.DimmerLive.Models;

[ParseClassName("UserDeviceSession")]
public class UserDeviceSession : ParseObject
{
    [ParseFieldName("sessionId")]
    public string SessionId
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    [ParseFieldName("userOwner")] // Changed from "userId" to be more descriptive of the pointer
    public UserModelOnline? UserOwner // Storing as a Pointer to _User class
    {
        get => GetProperty<UserModelOnline?>();
        set => SetProperty(value);
    }

    [ParseFieldName("deviceId")]
    public string DeviceId
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }
    [ParseFieldName("deviceName")]
    public string DeviceName
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }
    [ParseFieldName("deviceIdiom")]
    public string DeviceIdiom
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }
    [ParseFieldName("deviceManufacturer")]
    public string DeviceManufacturer
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }
    [ParseFieldName("devicePlatform")]
    public string DevicePlatform
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }
    [ParseFieldName("deviceModel")]
    public string DeviceModel
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }
    [ParseFieldName("deviceOSVersion")]
    public string DeviceOSVersion
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    [ParseFieldName("sessionStartTime")]
    public DateTime SessionStartTime
    {
        get => GetProperty<DateTime>();
        set => SetProperty(value);
    }

    // new ParseFile holder
    [ParseFieldName("fileData")]
    public ParseFile FileData
    {
        get => GetProperty<ParseFile>();
        set => SetProperty(value);
    }
    // new isActive holder
    [ParseFieldName("isActive")]
    public bool IsActive
    {
        get => GetProperty<bool>();
        set => SetProperty(value);
    }

    /// <summary>
    /// Attach & upload bytes as a ParseFile.
    /// </summary>
    public async Task AttachFileAsync(byte[] bytes, string fileName)
    {
        var file = new ParseFile(fileName, bytes);
        await file.SaveAsync(ParseClient.Instance);
        FileData = file;
    }

}
