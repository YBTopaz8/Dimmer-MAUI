using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Utilities.StatsUtils;
public static class NerdySongStats
{
    // 1. Is play count a Fibonacci number?
    public static bool IsPlayCountFibonacci(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        int playCount = SongStats.GetPlayCount(song, events);
        int a = 0, b = 1;
        while (b < playCount)
        { int t = b; b += a; a = t; }
        return playCount == 0 || playCount == 1 || playCount == b;
    }

    // 2. Is skip count a prime number?
    public static bool IsSkipCountPrime(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        int skip = SongStats.GetSkipCount(song, events);
        if (skip < 2)
            return false;
        for (int i = 2; i <= Math.Sqrt(skip); i++)
            if (skip % i == 0)
                return false;
        return true;
    }

    // 3. Number of palindromic seconds listened (e.g., 121, 232, etc.)
    public static int CountPalindromicSeconds(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
        => events.Where(e => e.SongId == song.Id)
                 .Count(e => IsPalindrome((int)e.PositionInSeconds));

    static bool IsPalindrome(int n)
    {
        var s = n.ToString();
        return s.SequenceEqual(s.Reverse());
    }

    // 4. Play count modulo 7
    public static int PlayCountMod7(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
        => SongStats.GetPlayCount(song, events) % 7;

    // 5. Play count as binary string
    public static string PlayCountBinary(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
        => Convert.ToString(SongStats.GetPlayCount(song, events), 2);

    // 6. Is duration a perfect square (seconds)?
    public static bool IsDurationPerfectSquare(SongModel song)
    {
        var sec = (int)song.DurationInSeconds;
        var root = Math.Sqrt(sec);
        return root == (int)root;
    }

    // 7. Play count digits sum
    public static int PlayCountDigitSum(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
        => SongStats.GetPlayCount(song, events).ToString().ToCharArray().Sum(c => c - '0');

    // 8. Song title vowel count
    public static int TitleVowelCount(SongModel song)
        => song.Title.Count(c => "aeiouAEIOU".Contains(c));

    // 9. Average play time (in golden ratio)
    public static double AvgPlayTimeGoldenRatio(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
        => SongStats.GetTotalListeningTime(song, events) / 1.618;

    // 10. Play count in hexadecimal
    public static string PlayCountHex(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
        => SongStats.GetPlayCount(song, events).ToString("X");

    // 11. Days between first and last play (is it a leap year span?)
    public static bool IsSpanLeapYear(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        var first = SongStats.GetFirstPlayedDate(song, events);
        var last = SongStats.GetLastPlayedDate(song, events);
        if (first == null || last == null)
            return false;
        int year = first.Value.Year;
        while (year <= last.Value.Year)
        {
            if (DateTime.IsLeapYear(year))
                return true;
            year++;
        }
        return false;
    }

    // 12. Length of song title (prime number?)
    public static bool IsTitleLengthPrime(SongModel song)
    {
        int n = song.Title.Length;
        if (n < 2)
            return false;
        for (int i = 2; i <= Math.Sqrt(n); i++)
            if (n % i == 0)
                return false;
        return true;
    }

    // 13. Number of times played on Friday the 13th
    public static int PlayedOnFriday13th(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
        => events.Where(e => e.SongId == song.Id && e.DatePlayed.Day == 13 && e.DatePlayed.DayOfWeek == DayOfWeek.Friday).Count();

    // 14. Play count as Roman numeral
    public static string PlayCountRoman(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        int n = SongStats.GetPlayCount(song, events);
        if (n == 0)
            return "N";
        var numerals = new[] {
            (1000, "M"), (900, "CM"), (500, "D"), (400, "CD"), (100, "C"),
            (90, "XC"), (50, "L"), (40, "XL"), (10, "X"),
            (9, "IX"), (5, "V"), (4, "IV"), (1, "I")
        };
        var result = "";
        foreach (var (val, sym) in numerals)
            while (n >= val)
            { result += sym; n -= val; }
        return result;
    }

    // 15. Last played hour (is it a palindrome?)
    public static bool WasLastPlayedHourPalindrome(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        var last = SongStats.GetLastPlayedDate(song, events);
        if (last == null)
            return false;
        var s = last.Value.Hour.ToString();
        return s.SequenceEqual(s.Reverse());
    }

    // 16. Played on all days of the week?
    public static bool WasPlayedAllDaysOfWeek(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        var days = events.Where(e => e.SongId == song.Id).Select(e => e.DatePlayed.DayOfWeek).Distinct().ToHashSet();
        return Enum.GetValues(typeof(DayOfWeek)).Cast<DayOfWeek>().All(d => days.Contains(d));
    }

    // 17. Duration seconds (Armstrong number?)
    public static bool IsDurationArmstrong(SongModel song)
    {
        int n = (int)song.DurationInSeconds;
        int sum = 0, temp = n, len = n.ToString().Length;
        while (temp > 0)
        {
            sum += (int)Math.Pow(temp % 10, len);
            temp /= 10;
        }
        return sum == n;
    }

    // 18. Times played at exact minute 42 (i.e., :42 in the hour)
    public static int PlayedAtMinute42(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
        => events.Where(e => e.SongId == song.Id && e.DatePlayed.Minute == 42).Count();

    // 19. Duration divisible by play count?
    public static bool IsDurationDivisibleByPlayCount(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        int p = SongStats.GetPlayCount(song, events);
        if (p == 0)
            return false;
        return ((int)song.DurationInSeconds) % p == 0;
    }

    // 20. Number of distinct digit pairs in song title
    public static int DistinctDigitPairsInTitle(SongModel song)
    {
        var digits = song.Title.Where(char.IsDigit).ToArray();
        var pairs = new HashSet<string>();
        for (int i = 0; i < digits.Length - 1; i++)
            pairs.Add($"{digits[i]}{digits[i + 1]}");
        return pairs.Count;
    }

    // 21. Number of vowels in artist name
    public static int VowelCountInArtist(SongModel song)
        => song.ArtistName.Count(c => "aeiouAEIOU".Contains(c));

    // 22. Skip count as Morse code
    public static string SkipCountMorse(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        int n = SongStats.GetSkipCount(song, events);
        var dict = new[] {
            ".----", "..---", "...--", "....-", ".....", "-....", "--...", "---..", "----.", "-----"
        };
        return string.Join(" ", n.ToString().Select(c => dict[c - '0']));
    }

    // 23. Play count digit palindrome?
    public static bool PlayCountIsDigitPalindrome(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        var s = SongStats.GetPlayCount(song, events).ToString();
        return s.SequenceEqual(s.Reverse());
    }

    // 24. Played on Pi Day? (March 14)
    public static bool PlayedOnPiDay(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
        => events.Any(e => e.SongId == song.Id && e.DatePlayed.Month == 3 && e.DatePlayed.Day == 14);

    // 25. Title starts and ends with same letter?
    public static bool TitleIsCircular(SongModel song)
        => !string.IsNullOrEmpty(song.Title) && song.Title.First() == song.Title.Last();


    // Helper: Fibonacci sequence
    private static readonly Dictionary<int, long> fibCache = new Dictionary<int, long>();
    private static long Fibonacci(int n)
    {
        if (n < 0)
            throw new ArgumentException("Input must be non-negative", nameof(n));
        if (n == 0)
            return 0;
        if (n == 1)
            return 1;
        if (fibCache.TryGetValue(n, out long value))
            return value;
        long result = Fibonacci(n - 1) + Fibonacci(n - 2);
        fibCache[n] = result;
        return result;
    }

    // 1. Play Count as Fibonacci Index (closest if not exact)
    public static int GetPlayCountFibonacciIndex(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        int playCount = SongStats.GetPlayCount(song, events);
        int i = 0;
        while (true)
        {
            long fibVal = Fibonacci(i);
            if (fibVal >= playCount)
                return i;
            i++;
            if (i > 50)
                return -1; // Safety break for large play counts
        }
    }

    // 2. "Golden Ratio" of Plays to Skips
    // (Plays + Skips) / Plays, compared to ~1.618
    public static double GetPlaySkipGoldenRatioApproximation(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        int plays = SongStats.GetPlayCount(song, events);
        int skips = SongStats.GetSkipCount(song, events);
        if (plays == 0)
            return 0;
        return (double)(plays + skips) / plays;
    }

    // 3. Is song duration (seconds) a prime number?
    public static bool IsDurationPrime(SongModel song)
    {
        if (song.DurationInSeconds <= 1)
            return false;
        int durationInt = (int)Math.Floor(song.DurationInSeconds);
        if (durationInt != song.DurationInSeconds)
            return false; // Not an integer
        for (int i = 2; i * i <= durationInt; i++)
        {
            if (durationInt % i == 0)
                return false;
        }
        return true;
    }

    // 4. Sum of digits of total play count
    public static int GetPlayCountDigitSum(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        int playCount = SongStats.GetPlayCount(song, events);
        return playCount.ToString().Select(c => int.Parse(c.ToString())).Sum();
    }

    // 5. "Eventfulness Score": (Plays + Skips + Pauses + Seeks) / DaysSinceFirstPlayed
    // (Requires SongStats.GetPauseCount, SongStats.GetSeekCount, SongStats.GetDaysSinceFirstPlayed)
    //public static double GetEventfulnessScore(SongModel song, IReadOnlyCollection<DimmerPlayEvent> Events)
    //{
    //    int plays = SongStats.GetPlayCount(song, Events);
    //    int skips = SongStats.GetSkipCount(song, Events);
    //    //int pauses = SongStats.GetPauseCount(song, Events); // Assuming this exists
    //    //int seeks = SongStats.GetSeekCount(song, Events);   // Assuming this exists
    //    //double daysSinceFirst = SongStats.GetDaysSinceFirstPlayed(song, Events); // Assuming this exists
    //    //if (daysSinceFirst == 0)
    //        //return plays + skips + pauses + seeks; // Avoid division by zero
    //    //return (plays + skips + pauses + seeks) / daysSinceFirst;
    //}

    // 6. Binary representation of track number (if exists)
    public static string? GetTrackNumberBinary(SongModel song)
    {
        if (song.TrackNumber.HasValue && song.TrackNumber.Value > 0)
        {
            return Convert.ToString(song.TrackNumber.Value, 2);
        }
        return null;
    }

    // 7. Is rating a perfect square?
    public static bool IsRatingPerfectSquare(SongModel song)
    {
        if (song.Rating <= 0)
            return false;
        double sqrt = Math.Sqrt(song.Rating);
        return sqrt == Math.Floor(sqrt);
    }

    // 8. "Entropy" of PlayTypes (Higher if many different PlayTypes occurred)
    // Shannon entropy based on PlayType distribution. (More complex to implement accurately)
    // Simplified: Number of distinct PlayTypes that have occurred for this song.
    public static int GetDistinctPlayTypesCount(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        if (song == null || events == null)
            return 0;
        return events.Where(e => e.SongId == song.Id).Select(e => e.PlayType).Distinct().Count();
    }

    // 9. Product of (non-zero) digits in ReleaseYear
    public static long? GetReleaseYearDigitProduct(SongModel song)
    {
        if (!song.ReleaseYear.HasValue)
            return null;
        long product = 1;
        bool hasNonZero = false;
        foreach (char c in song.ReleaseYear.Value.ToString())
        {
            int digit = int.Parse(c.ToString());
            if (digit != 0)
            {
                product *= digit;
                hasNonZero = true;
            }
        }
        return hasNonZero ? product : (long?)null; // Or return 0 if all digits are zero (unlikely for year)
    }

    // 10. "Consistency Score": 1 / (Standard Deviation of daily play counts)
    // (Requires more complex stats on daily plays, lower std dev = higher consistency)
    // Simplified: Ratio of (Average plays on playing days) to (Max plays on any single day)
    public static double GetSimplifiedPlayConsistency(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        var dailyPlays = events
            .Where(e => e.SongId == song.Id)
            .GroupBy(e => e.DatePlayed.Date)
            .Select(g => g.Count())
            .ToList();

        if (!dailyPlays.Any())
            return 0;
        double avgPlaysOnPlayingDays = dailyPlays.Average();
        int maxPlaysInADay = dailyPlays.Max();
        if (maxPlaysInADay == 0)
            return 1; // Perfectly consistent (no plays, or plays are zero)
        return avgPlaysOnPlayingDays / maxPlaysInADay;
    }

    // 11. Number of plays on days that are prime numbers of the month (e.g., 2nd, 3rd, 5th, 7th...)
    public static int GetPlaysOnPrimeDaysOfMonth(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        Func<int, bool> isPrime = n =>
        {
            if (n <= 1)
                return false;
            for (int i = 2; i * i <= n; i++)
                if (n % i == 0)
                    return false;
            return true;
        };
        return events.Count(e => e.SongId == song.Id && isPrime(e.DatePlayed.Day));
    }

    // 12. "Palindrome Play Day": Was it ever played on a day like MM/DD where DDMM is a palindrome (e.g., 12/21 -> 2112)
    public static bool WasPlayedOnPalindromeDay(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        return events.Any(e =>
        {
            if (e.SongId != song.Id)
                return false;
            string mm = e.DatePlayed.Month.ToString("00");
            string dd = e.DatePlayed.Day.ToString("00");
            string combined = dd + mm;
            return combined == new string(combined.Reverse().ToArray());
        });
    }

    // 13. Ratio of Vowels to Consonants in Song Title
    public static double GetTitleVowelToConsonantRatio(SongModel song)
    {
        if (string.IsNullOrEmpty(song.Title))
            return 0;
        string vowels = "aeiouAEIOU";
        int vowelCount = song.Title.Count(c => vowels.Contains(c));
        int consonantCount = song.Title.Count(c => char.IsLetter(c) && !vowels.Contains(c));
        if (consonantCount == 0)
            return vowelCount > 0 ? double.PositiveInfinity : 0;
        return (double)vowelCount / consonantCount;
    }

    // 14. "Fibonacci Skip Interval": Were skips N, N+M, N+M+K where N,M,K are Fibonacci numbers?
    // (Very complex to track specific skip Events, this is more conceptual)
    // Simplified: Is the total number of skips a Fibonacci number?
    public static bool IsSkipCountFibonacci(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        int skipCount = SongStats.GetSkipCount(song, events);
        if (skipCount < 0)
            return false; // Should not happen
        int i = 0;
        while (true)
        {
            long fibVal = Fibonacci(i);
            if (fibVal == skipCount)
                return true;
            if (fibVal > skipCount || i > 50)
                return false; // Safety break
            i++;
        }
    }

    // 15. Hamming distance between binary representation of song duration and album track count
    public static int? GetDurationTrackCountHammingDistance(SongModel song)
    {
        if (song.Album == null || song.DurationInSeconds <= 0 || song.Album.NumberOfTracks <= 0)
            return null;
        string durBin = Convert.ToString((int)song.DurationInSeconds, 2);
        string trackBin = Convert.ToString(song.Album.NumberOfTracks, 2);

        // Pad shorter string with leading zeros
        int len = Math.Max(durBin.Length, trackBin.Length);
        durBin = durBin.PadLeft(len, '0');
        trackBin = trackBin.PadLeft(len, '0');

        int distance = 0;
        for (int i = 0; i < len; i++)
        {
            if (durBin[i] != trackBin[i])
                distance++;
        }
        return distance;
    }

    // 16. How many times was the song played on the Nth day of the year, where N is the track number?
    public static int GetPlaysOnTrackNumberDayOfYear(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        if (!song.TrackNumber.HasValue || song.TrackNumber.Value <= 0)
            return 0;
        return events.Count(e => e.SongId == song.Id && e.DatePlayed.DayOfYear == song.TrackNumber.Value);
    }

    // 17. "Title Length Play Bonus": Play Count * Title.Length
    public static long GetTitleLengthPlayBonus(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        return (long)SongStats.GetPlayCount(song, events) * (song.Title?.Length ?? 0);
    }

    // 18. Is the number of distinct artists a prime number?
    public static bool IsArtistCountPrime(SongModel song)
    {
        int artistCount = song.ArtistIds?.Count ?? 0;
        if (artistCount <= 1)
            return false;
        for (int i = 2; i * i <= artistCount; i++)
        {
            if (artistCount % i == 0)
                return false;
        }
        return true;
    }

    // 19. "Lunar Phase Play": How many plays occurred during a full moon? (Requires a lunar phase library/API)
    // Placeholder - actual implementation needs external data.
    public static int GetPlaysDuringFullMoon(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events, Func<DateTimeOffset, bool> isFullMoonOracle)
    {
        return events.Count(e => e.SongId == song.Id && isFullMoonOracle(e.DatePlayed));
    }

    // 20. "Bitwise Playfulness": XOR sum of (PlayType * DayOfWeek) for all play Events.
    public static int GetBitwisePlayfulnessScore(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        if (song == null || events == null)
            return 0;
        int score = 0;
        foreach (var e in events.Where(ev => ev.SongId == song.Id))
        {
            score ^= (e.PlayType * (int)e.DatePlayed.DayOfWeek);
        }
        return score;
    }

    // 21. "Geographic Device Diversity": Number of distinct (simulated) countries based on DeviceModel or hypothetical location data.
    // Placeholder - actual implementation needs more data.
    // Simplified: Number of distinct DeviceManufacturer (as a proxy for geographic diversity)
    public static int GetDeviceManufacturerDiversity(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        if (song == null || events == null)
            return 0;
        return events.Where(e => e.SongId == song.Id && !string.IsNullOrEmpty(e.DeviceManufacturer))
                     .Select(e => e.DeviceManufacturer!)
                     .Distinct()
                     .Count();
    }

    // 22. Time since last play, expressed in multiples of song's own duration.
    public static double? GetTimeSinceLastPlayInSongDurations(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        var lastPlayDate = SongStats.GetLastPlayedDate(song, events);
        if (!lastPlayDate.HasValue || song.DurationInSeconds <= 0)
            return null;
        TimeSpan timeSince = DateTimeOffset.UtcNow - lastPlayDate.Value;
        return timeSince.TotalSeconds / song.DurationInSeconds;
    }

    // 23. "Pascal's Triangle Play Count": Does the play count appear in the first N rows of Pascal's Triangle?
    public static bool IsPlayCountInPascalsTriangle(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events, int rowsToCheck = 20)
    {
        int playCount = SongStats.GetPlayCount(song, events);
        if (playCount <= 0)
            return false;

        List<long> currentRow = new List<long> { 1 };
        for (int i = 0; i < rowsToCheck; i++)
        {
            if (currentRow.Contains(playCount))
                return true;
            List<long> nextRow = new List<long> { 1 };
            for (int j = 0; j < currentRow.Count - 1; j++)
            {
                nextRow.Add(currentRow[j] + currentRow[j + 1]);
            }
            nextRow.Add(1);
            currentRow = nextRow;
        }
        return currentRow.Contains(playCount); // Check last generated row too
    }

    // 24. "Zenith Play Hour": The hour of day with the absolute highest number of plays for this song.
    public static int? GetZenithPlayHour(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        if (song == null || events == null)
            return null;
        var hourlyPlays = events
            .Where(e => e.SongId == song.Id)
            .GroupBy(e => e.DatePlayed.Hour)
            .Select(g => new { Hour = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .FirstOrDefault();
        return hourlyPlays?.Hour;
    }

    // 25. "Consecutive Day Play Streak" (current or longest)
    // Longest Streak implementation
    public static int GetLongestConsecutiveDayPlayStreak(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        if (song == null || events == null)
            return 0;
        var playDays = events
            .Where(e => e.SongId == song.Id)
            .Select(e => e.DatePlayed.Date)
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        if (!playDays.Any())
            return 0;

        int longestStreak = 0;
        int currentStreak = 0;
        DateTime? previousDay = null;

        foreach (var day in playDays)
        {
            if (previousDay.HasValue && day == previousDay.Value.AddDays(1))
            {
                currentStreak++;
            }
            else
            {
                longestStreak = Math.Max(longestStreak, currentStreak);
                currentStreak = 1; // Start new streak
            }
            previousDay = day;
        }
        longestStreak = Math.Max(longestStreak, currentStreak); // Check last streak
        return longestStreak;
    }
    // 1. Eddington Number (maximum E such that song was played on E different days, each with at least E plays)
    public static int GetEddingtonNumber(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        var playsByDay = events
            .Where(e => e.SongId == song.Id && e.PlayType == 0)
            .GroupBy(e => e.DatePlayed.Date)
            .Select(g => g.Count())
            .OrderByDescending(x => x)
            .ToList();
        int E = 0;
        for (int i = 0; i < playsByDay.Count; i++)
            if (playsByDay[i] >= i + 1)
                E = i + 1;
            else
                break;
        return E;
    }

    // 2. Play Count is a Golden Ratio Number? (n and n+1 are consecutive play counts that approximate φ)
    public static bool IsPlayCountGoldenRatio(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        int playCount = SongStats.GetPlayCount(song, events);
        // Check if playCount/(playCount-1) ≈ 1.618
        if (playCount < 2)
            return false;
        double ratio = (double)playCount / (playCount - 1);
        return Math.Abs(ratio - 1.618) < 0.02;
    }

    // 3. How many times play count crosses multiples of Pi (e.g., π, 2π, 3π...)?
    public static int PlayCountCrossesPi(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        int playCount = SongStats.GetPlayCount(song, events);
        return (int)(playCount / Math.PI);
    }

    // 4. Play count in base e (natural log)
    public static double PlayCountLogE(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        int playCount = SongStats.GetPlayCount(song, events);
        return playCount > 0 ? Math.Log(playCount) : 0;
    }

    // 5. Duration/PlayCount ratio close to e?
    public static bool DurationToPlayCountIsE(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        int playCount = SongStats.GetPlayCount(song, events);
        if (playCount == 0)
            return false;
        double ratio = song.DurationInSeconds / playCount;
        return Math.Abs(ratio - Math.E) < 0.2;
    }

    // 6. Song position in masterlist is a Fibonacci number?
    public static bool IsSongIndexFibonacci(SongModel song, IReadOnlyCollection<SongModel> masterList)
    {
        int idx = masterList.ToList().FindIndex(s => s.Id == song.Id) + 1;
        int a = 0, b = 1;
        while (b < idx)
        { int t = b; b += a; a = t; }
        return idx == 0 || idx == 1 || idx == b;
    }

    // 7. Collatz sequence steps for play count
    public static int CollatzSteps(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        int n = SongStats.GetPlayCount(song, events);
        int steps = 0;
        while (n > 1)
        {
            n = n % 2 == 0 ? n / 2 : 3 * n + 1;
            steps++;
        }
        return steps;
    }

    // 8. Euler’s Totient (number of play counts less than play count and coprime to it)
    public static int EulerTotientOfPlayCount(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        int n = SongStats.GetPlayCount(song, events);
        int count = 0;
        for (int i = 1; i < n; i++)
            if (Gcd(i, n) == 1)
                count++;
        return count;

        int Gcd(int a, int b)
        {
            while (b != 0)
            { int t = b; b = a % b; a = t; }
            return a;
        }
    }

    // 9. Is play count divisible by 42? (The answer to life)
    public static bool IsPlayCountDivisibleBy42(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
        => SongStats.GetPlayCount(song, events) % 42 == 0 && SongStats.GetPlayCount(song, events) != 0;

    // 10. Does the ratio (play count / skip count) approximate π?
    public static bool PlayToSkipRatioIsPi(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        int plays = SongStats.GetPlayCount(song, events);
        int skips = SongStats.GetSkipCount(song, events);
        if (skips == 0)
            return false;
        double ratio = (double)plays / skips;
        return Math.Abs(ratio - Math.PI) < 0.1;
    }
}
