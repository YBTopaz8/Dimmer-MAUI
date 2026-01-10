namespace Dimmer.DimmerLive.Models;

[ParseClassName("FeedbackNotificationSettings")]
public class FeedbackNotificationSettings : ParseObject
{
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

    [ParseFieldName("issue")]
    public FeedbackIssue Issue
    {
        get => GetProperty<FeedbackIssue>();
        set => SetProperty(value);
    }

    [ParseFieldName("issueId")]
    public string IssueId
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    [ParseFieldName("notifyOnStatusChange")]
    public bool NotifyOnStatusChange
    {
        get => GetProperty<bool>();
        set => SetProperty(value);
    }

    [ParseFieldName("notifyOnComment")]
    public bool NotifyOnComment
    {
        get => GetProperty<bool>();
        set => SetProperty(value);
    }
}
