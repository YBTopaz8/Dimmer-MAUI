namespace Dimmer.DimmerLive.Models;

[ParseClassName("FeedbackComment")]
public class FeedbackComment : ParseObject
{
    [ParseFieldName("issue")]
    public FeedbackIssue Issue
    {
        get => GetProperty<FeedbackIssue>();
        set => SetProperty(value);
    }

    [ParseFieldName("text")]
    public string Text
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
}
