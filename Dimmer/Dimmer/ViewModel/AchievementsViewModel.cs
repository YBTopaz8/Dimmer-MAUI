using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.ViewModel;

public partial class AchievementsViewModel : ObservableObject, IDisposable
{
    private readonly AchievementService _svc;
    private readonly Realm _realm;
    private IDisposable _unlockSubscription;
    private List<AchievementDisplayItem> _allItems;

    [ObservableProperty]
    public partial ObservableCollection<AchievementDisplayItem> AchievementList { get; set; } = new();

    [ObservableProperty]
    public partial ObservableCollection<AchievementDisplayItem> FilteredItems { get; set; } = new();

    [ObservableProperty]
    public partial string UnlockProgressText { get; set; } 

    [ObservableProperty]
    public partial double UnlockProgressValue { get; set; }

    [ObservableProperty]
    public partial string SelectedFilter { get; set; } = "All";

    public List<string> FilterOptions { get; } = new() { "All", "Unlocked", "Locked", "Bronze", "Silver", "Gold", "Platinum" };


    public AchievementsViewModel(AchievementService svc, IRealmFactory realmFactory)
    {
        _svc = svc;
        _realm = realmFactory.GetRealmInstance();
        LoadAchievements();
        SubscribeToUnlocks();
    }

    private void LoadAchievements()
    {
        var stats = _realm.Find<UserStats>("Global");
        var unlockedIds = stats?.UnlockedGlobalAchievements.ToHashSet() ?? new HashSet<AchievementRule>();

        var allRules = _svc.GetAllRules();

        _allItems = new List<AchievementDisplayItem>();

        foreach (var rule in allRules)
        {
            // Only show Global/General/Streak rules here. 
            // Song-Specific rules (like "Play THIS song 50 times") usually clutter the global view.
            if (!IsSongSpecific((AchievementCategory)rule.Category))
            {
                bool unlocked = unlockedIds.Contains(rule);
                _allItems.Add(new AchievementDisplayItem(rule, unlocked));
            }
        }

        UpdateProgress();
        ApplyFilter();
    }

    private void UpdateProgress()
    {
        int total = _allItems.Count;
        int unlocked = _allItems.Count(x => x.IsUnlocked);
        UnlockProgressText = $"{unlocked} / {total} Unlocked";
        UnlockProgressValue = total == 0 ? 0 : ((double)unlocked / total) * 100;
    }

    partial void OnSelectedFilterChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        var query = _allItems.AsEnumerable();

        query = SelectedFilter switch
        {
            "Unlocked" => query.Where(x => x.IsUnlocked),
            "Locked" => query.Where(x => !x.IsUnlocked),
            "Bronze" => query.Where(x => x.Rule.Tier == (int)AchievementTier.Bronze),
            "Silver" => query.Where(x => x.Rule.Tier == (int)AchievementTier.Silver),
            "Gold" => query.Where(x => x.Rule.Tier == (int)AchievementTier.Gold),
            "Platinum" => query.Where(x => x.Rule.Tier == (int)AchievementTier.Platinum),
            _ => query
        };

        FilteredItems = new ObservableCollection<AchievementDisplayItem>(query);
    }
    private void SubscribeToUnlocks()
    {
        // Listen to the Service Observable
        _unlockSubscription = _svc.UnlockedAchievement
            .ObserveOn(SynchronizationContext.Current) // Ensure UI Thread
            .Subscribe(result =>
            {
                var (rule, song) = result;

                // 1. Show Toast Notification (Pseudo-code)
                // ToastService.Show($"🏆 {rule.Name}", rule.Description);

                // 2. Update the list item if it exists
                var item = AchievementList.FirstOrDefault(x => x.Rule.Id == rule.Id);
                if (item != null)
                {
                    item.IsUnlocked = true;
                    item.OtherDisplayDescription = rule.Description;
                    //item.Opacity = 1.0;
                }
            });
    }

    private bool IsSongSpecific(AchievementCategory cat) =>
     cat == AchievementCategory.SongCount || cat == AchievementCategory.SongSkip || cat == AchievementCategory.SongFav;
    public void Dispose()
    {
        _unlockSubscription?.Dispose();
        _realm?.Dispose();
    }
}
