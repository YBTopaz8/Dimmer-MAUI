using Parse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.ParseSection.Models;

[ParseClassName("UserDeviceSession")]
public class UserDeviceSession : ParseObject
{
    [ParseFieldName("sessionId")]
    public string SessionId
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    [ParseFieldName("userId")]
    public string UserId
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    [ParseFieldName("deviceId")]
    public string DeviceId
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
