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
}
