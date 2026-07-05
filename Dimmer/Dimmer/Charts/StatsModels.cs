using System;
using System.Collections.Generic;
using System.Text;

namespace Dimmer.Charts;

public partial class StatsModels
{
}

public record TextStat(string Title, string Value, string Subtitle = "");
public record ChartPoint(string Label, double YValue, double XValue = 0);
public record DateChartPoint(DateTime Date, double Value);
public record InsightStat(string Title, string Description, string Icon = "💡", string ColorHex = "#885555FF");
public record LeaderboardItem(string Rank, string Name, string SubValue, string ImagePath = "", string Id = "");
public record TrendStat(string Period, int PlayCount, int ChangeVsPrevious);
public record PlaySession(DateTimeOffset StartTime, int EventCount, double TotalListeningTimeSeconds, string SessionSummary);
public record SongPairing(string PairedSongTitle, int TimesPlayedTogether, string Context, string? CoverImagePath, string? songTitleDurationKey, ObjectId? songId=null, bool isPresentOnDevice=false);
public record HealthIssue(string IssueType, string Description, int Severity); // For library health