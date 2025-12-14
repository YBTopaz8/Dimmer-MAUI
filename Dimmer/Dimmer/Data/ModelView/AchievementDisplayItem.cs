using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Data.ModelView;


public partial class AchievementDisplayItem : ObservableObject
{
    public AchievementRule Rule { get; }

    [ObservableProperty]
    public partial bool IsUnlocked { get; set; }

    // Computed property for the UI Title
    public string DisplayTitle => Rule.Name;

    // Computed property: If locked, hide the description to keep it a surprise!
    public string DisplayDescription => IsUnlocked
        ? Rule.Description
        : "??? (Keep playing to unlock)";

    // Visuals based on Tier
    public Brush TierColor => Rule.Tier switch
    {
        (int)AchievementTier.Bronze => new SolidColorBrush(Colors.Brown),   // Bronze
        (int)AchievementTier.Silver => new SolidColorBrush(Colors.Silver),  // Silver
        (int)AchievementTier.Gold => new SolidColorBrush(Colors.Gold),      // Gold
        (int)AchievementTier.Platinum => new SolidColorBrush(Colors.DarkSlateBlue),// Platinum
        _ => new SolidColorBrush(Colors.Gray)
    };

    // Icon for the Tier
    public string TierIcon => Rule.Tier switch
    {
        (int)AchievementTier.Bronze => "🥉",
        (int)AchievementTier.Silver => "🥈",
        (int)AchievementTier.Gold => "🥇",
        (int)AchievementTier.Platinum => "💎",
        _ => "🔒"
    };

    public double Opacity => IsUnlocked ? 1.0 : 0.4;

    public string OtherDisplayDescription { get; internal set; }

    public AchievementDisplayItem(AchievementRule rule, bool isUnlocked)
    {
        Rule = rule;
        IsUnlocked = isUnlocked;
    }
}