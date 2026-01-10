namespace Dimmer.DimmerLive.Models;

[ParseClassName("FeedbackIssue")]
public class FeedbackIssue : ParseObject
{
    [ParseFieldName("title")]
    public string Title
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    [ParseFieldName("type")]
    public string Type // "Bug" or "Feature"
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    [ParseFieldName("description")]
    public string Description
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    [ParseFieldName("status")]
    public string Status // "open", "planned", "in-progress", "shipped", "rejected"
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    [ParseFieldName("upvoteCount")]
    public int UpvoteCount
    {
        get => GetProperty<int>();
        set => SetProperty(value);
    }

    [ParseFieldName("platform")]
    public string Platform // "Windows", "Android", "All"
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    [ParseFieldName("appVersion")]
    public string AppVersion
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    [ParseFieldName("author")]
    public UserModelOnline Author
    {
        get => GetProperty<UserModelOnline>();
        set => SetProperty(value);
    }

    [ParseFieldName("authorUsername")]
    public string AuthorUsername
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    [ParseFieldName("commentCount")]
    public int CommentCount
    {
        get => GetProperty<int>();
        set => SetProperty(value);
    }
}

public static class FeedbackIssueType
{
    public const string Bug = "Bug";
    public const string Feature = "Feature";
}

public static class FeedbackIssueStatus
{
    public const string Open = "open";
    public const string Planned = "planned";
    public const string InProgress = "in-progress";
    public const string Shipped = "shipped";
    public const string Rejected = "rejected";
}
