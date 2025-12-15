using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.ViewModel;

public partial class SongAchievementsViewModel : ObservableObject
{
    private readonly AchievementService _svc;

    [ObservableProperty]
    public partial ObservableCollection<AchievementDisplayItem> SongAchievements { get; set;  }

    public SongAchievementsViewModel(AchievementService svc)
    {
        _svc = svc;
    }

    // Call this when the user navigates to a song page
    public void Initialize(SongModel song)
    {
        var allRules = _svc.GetAllRules();
        var earnedIds = song.EarnedAchievementIds.ToHashSet();

        var list = new List<AchievementDisplayItem>();

        foreach (var rule in allRules)
        {
            // Only allow Song-Specific categories
            if (IsSongSpecific((AchievementCategory)rule.Category))
            {
                bool unlocked = earnedIds.Contains(rule);

                // OPTIONAL: Only show if unlocked OR close to unlocking?
                // For now, let's show all to encourage grinding.
                list.Add(new AchievementDisplayItem(rule, unlocked));
            }
        }

        var ee = list.Where(x => x.IsUnlocked).OrderBy(x => x.Rule.Threshold);
        SongAchievements = new ObservableCollection<AchievementDisplayItem>(
        ee    
        );
    }

    private bool IsSongSpecific(AchievementCategory cat) =>
        cat == AchievementCategory.SongCount || cat == AchievementCategory.SongSkip || cat == AchievementCategory.SongFav;
}
