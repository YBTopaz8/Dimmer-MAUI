namespace Dimmer.DimmerLive.Models;

[ParseClassName("AppUpdate")]
public partial class AppUpdateModel : ParseObject
{
    [ParseFieldName("title")]
    public string title
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    [ParseFieldName("url")]
    public string url
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    [ParseFieldName("appVersion")]
    public string appVersion
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    [ParseFieldName("appStage")]
    public string appStage
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    [ParseFieldName("notes")]
    public string notes
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    [ParseFieldName("author")]
    public ParseUser author
    {
        get => GetProperty<ParseUser>();
        set => SetProperty(value);
    }

    [ParseFieldName("channel")]
    public string channel
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    [ParseFieldName("isMandatory")]
    public bool isMandatory
    {
        get => GetProperty<bool>();
        set => SetProperty(value);
    }

    [ParseFieldName("minVersion")]
    public string minVersion
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    [ParseFieldName("fileSize")]
    public long fileSize
    {
        get => GetProperty<long>();
        set => SetProperty(value);
    }

    [ParseFieldName("checksum")]
    public string checksum
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    [ParseFieldName("rolloutPercentage")]
    public int rolloutPercentage
    {
        get => GetProperty<int>();
        set => SetProperty(value);
    }
}
