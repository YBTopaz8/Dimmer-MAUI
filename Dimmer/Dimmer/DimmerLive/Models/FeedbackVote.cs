namespace Dimmer.DimmerLive.Models;

[ParseClassName("FeedbackVote")]
public class FeedbackVote : ParseObject
{
    [ParseFieldName("issue")]
    public FeedbackIssue Issue
    {
        get => GetProperty<FeedbackIssue>();
        set => SetProperty(value);
    }

    [ParseFieldName("user")]
    public UserModelOnline User
    {
        get => GetProperty<UserModelOnline>();
        set => SetProperty(value);
    }

    [ParseFieldName("userId")]
    public string UserId
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }
}
