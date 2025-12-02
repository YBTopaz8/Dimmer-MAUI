using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public partial class UserStats : RealmObject
{
    [PrimaryKey]
    public string Id { get; set; } = "Global";
    public int TotalPlays { get; set; }
    public int TotalSkips { get; set; }
    public int TotalFavorites { get; set; }

    public int DailyPlayStreak { get; set; }
    public DateTimeOffset? LastPlayDate { get; set; }

    // --- Achievements ---
    // We store IDs here so we don't spam notifications
    public IList<string> UnlockedGlobalAchievementIds { get; }
}


public class AchievementRule
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public AchievementCategory Category { get; set; }
    public int Threshold { get; set; }
    public Func<Realm, UserStats, SongModel, bool> CustomCheck { get; set; }
}
public enum AchievementCategory
{
    SongCount, SongSkip, SongFav,
    AlbumCount, AlbumFav,
    ArtistCount, ArtistFav,
    GlobalCount, GlobalTime, Streak,
    Genre, Misc
}