using System;
using System.Collections.Generic;
using System.Text;

namespace Dimmer.Charts;

public partial class StatsModels
{
}

public record ChartPoint(string Label, double YValue, double XValue = 0);
public record DateChartPoint(DateTime Date, double Value);
public record TextStat(string Title, string Value, string Subtitle = "");
public record InsightStat(string Title, string Description, string Icon = "💡");
public record LeaderboardItem(string Rank, string Name, string SubValue, string ImagePath = "");