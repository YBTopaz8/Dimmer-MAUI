using Dimmer.Data.Models;
using Dimmer.Interfaces;
using Dimmer.Interfaces.Services;
using Xunit;

namespace Dimmer.Tests;

public class WeightedShuffleServiceTests
{
    private readonly IWeightedShuffleService _service;

    public WeightedShuffleServiceTests()
    {
        _service = new WeightedShuffleService();
    }

    [Fact]
    public void CalculateWeight_HiddenSong_ReturnsZero()
    {
        // Arrange
        var song = new SongModel { IsHidden = true };

        // Act
        var weight = _service.CalculateWeight(song);

        // Assert
        Assert.Equal(0, weight);
    }

    [Fact]
    public void CalculateWeight_BaseSong_ReturnsPositiveWeight()
    {
        // Arrange
        var song = new SongModel 
        { 
            IsHidden = false,
            Rating = 0,
            IsFavorite = false,
            ManualFavoriteCount = 0,
            PlayCompletedCount = 0,
            SkipCount = 0
        };

        // Act
        var weight = _service.CalculateWeight(song);

        // Assert
        Assert.True(weight > 0);
        Assert.Equal(130, weight); // Base (100) + never played bonus (30)
    }

    [Fact]
    public void CalculateWeight_HighlyRatedSong_ReturnsHigherWeight()
    {
        // Arrange
        var baseSong = new SongModel 
        { 
            IsHidden = false,
            Rating = 0
        };
        
        var ratedSong = new SongModel 
        { 
            IsHidden = false,
            Rating = 5
        };

        // Act
        var baseWeight = _service.CalculateWeight(baseSong);
        var ratedWeight = _service.CalculateWeight(ratedSong);

        // Assert
        Assert.True(ratedWeight > baseWeight);
        Assert.Equal(100, ratedWeight - baseWeight); // 5 * 20 = 100
    }

    [Fact]
    public void CalculateWeight_FavoriteSong_ReturnsHigherWeight()
    {
        // Arrange
        var normalSong = new SongModel 
        { 
            IsHidden = false,
            IsFavorite = false
        };
        
        var favoriteSong = new SongModel 
        { 
            IsHidden = false,
            IsFavorite = true
        };

        // Act
        var normalWeight = _service.CalculateWeight(normalSong);
        var favoriteWeight = _service.CalculateWeight(favoriteSong);

        // Assert
        Assert.True(favoriteWeight > normalWeight);
        Assert.Equal(50, favoriteWeight - normalWeight);
    }

    [Fact]
    public void CalculateWeight_HighSkipCount_ReducesWeight()
    {
        // Arrange
        var normalSong = new SongModel 
        { 
            IsHidden = false,
            SkipCount = 0
        };
        
        var skippedSong = new SongModel 
        { 
            IsHidden = false,
            SkipCount = 10
        };

        // Act
        var normalWeight = _service.CalculateWeight(normalSong);
        var skippedWeight = _service.CalculateWeight(skippedSong);

        // Assert
        Assert.True(skippedWeight < normalWeight);
    }

    [Fact]
    public void CalculateWeight_PlayCompletedCount_IncreasesWeight()
    {
        // Arrange
        var song1 = new SongModel 
        { 
            IsHidden = false,
            PlayCompletedCount = 0
        };
        
        var song2 = new SongModel 
        { 
            IsHidden = false,
            PlayCompletedCount = 10
        };

        // Act
        var weight1 = _service.CalculateWeight(song1);
        var weight2 = _service.CalculateWeight(song2);

        // Assert
        Assert.True(weight2 > weight1);
        Assert.Equal(20, weight2 - weight1); // 10 * 2 = 20
    }

    [Fact]
    public void CalculateWeight_RecentlyPlayed_HasLowerBonus()
    {
        // Arrange
        var recentSong = new SongModel 
        { 
            IsHidden = false,
            LastPlayed = DateTimeOffset.UtcNow.AddHours(-1)
        };
        
        var oldSong = new SongModel 
        { 
            IsHidden = false,
            LastPlayed = DateTimeOffset.UtcNow.AddDays(-10)
        };

        // Act
        var recentWeight = _service.CalculateWeight(recentSong);
        var oldWeight = _service.CalculateWeight(oldSong);

        // Assert
        Assert.True(oldWeight > recentWeight);
    }

    [Fact]
    public void CalculateWeight_NeverPlayed_HasModerateBonus()
    {
        // Arrange
        var neverPlayedSong = new SongModel 
        { 
            IsHidden = false,
            LastPlayed = default
        };

        // Act
        var weight = _service.CalculateWeight(neverPlayedSong);

        // Assert
        Assert.True(weight >= 130); // Base (100) + never played bonus (30)
    }

    [Fact]
    public void WeightedShuffle_EmptyList_ReturnsEmptyList()
    {
        // Arrange
        var songs = new List<SongModel>();

        // Act
        var result = _service.WeightedShuffle(songs, _service.CalculateWeight);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void WeightedShuffle_SingleItem_ReturnsSameItem()
    {
        // Arrange
        var songs = new List<SongModel> { new SongModel { IsHidden = false } };

        // Act
        var result = _service.WeightedShuffle(songs, _service.CalculateWeight);

        // Assert
        Assert.Single(result);
        Assert.Equal(songs[0], result[0]);
    }

    [Fact]
    public void WeightedShuffle_AllHiddenSongs_ReturnsEmptyList()
    {
        // Arrange
        var songs = new List<SongModel>
        {
            new SongModel { IsHidden = true },
            new SongModel { IsHidden = true },
            new SongModel { IsHidden = true }
        };

        // Act
        var result = _service.WeightedShuffle(songs, _service.CalculateWeight);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void WeightedShuffle_MixedWeights_ReturnsAllNonHiddenSongs()
    {
        // Arrange
        var songs = new List<SongModel>
        {
            new SongModel { IsHidden = false, Rating = 1 },
            new SongModel { IsHidden = true, Rating = 5 },
            new SongModel { IsHidden = false, Rating = 3 },
            new SongModel { IsHidden = false, Rating = 2 }
        };

        // Act
        var result = _service.WeightedShuffle(songs, _service.CalculateWeight);

        // Assert
        Assert.Equal(3, result.Count); // Only non-hidden songs
        Assert.DoesNotContain(result, s => s.IsHidden);
    }

    [Fact]
    public void WeightedShuffle_HigherWeightedItems_AppearMoreOftenAtStart()
    {
        // Arrange - Create songs with significantly different weights
        var songs = new List<SongModel>();
        
        // Add 10 low-rated songs
        for (int i = 0; i < 10; i++)
        {
            songs.Add(new SongModel 
            { 
                IsHidden = false, 
                Rating = 1,
                SkipCount = 10 
            });
        }
        
        // Add 1 highly-rated favorite song
        var favoriteSong = new SongModel 
        { 
            IsHidden = false, 
            Rating = 5,
            IsFavorite = true,
            ManualFavoriteCount = 5,
            PlayCompletedCount = 20
        };
        songs.Add(favoriteSong);

        // Act - Run shuffle multiple times and check if favorite appears early
        int timesInTopThree = 0;
        int iterations = 100;
        
        for (int i = 0; i < iterations; i++)
        {
            var result = _service.WeightedShuffle(songs, _service.CalculateWeight);
            if (result.Take(3).Contains(favoriteSong))
            {
                timesInTopThree++;
            }
        }

        // Assert - The highly weighted song should appear in top 3 positions 
        // more than random chance (which would be ~27% for 3 out of 11 positions)
        // We expect at least 50% due to much higher weight
        double percentage = (double)timesInTopThree / iterations;
        Assert.True(percentage > 0.5, 
            $"Highly weighted song appeared in top 3 only {percentage:P} of the time");
    }

    [Fact]
    public void WeightedShuffle_IsRandomized_NotAlwaysSameOrder()
    {
        // Arrange
        var songs = new List<SongModel>
        {
            new SongModel { IsHidden = false, Rating = 3 },
            new SongModel { IsHidden = false, Rating = 3 },
            new SongModel { IsHidden = false, Rating = 3 },
            new SongModel { IsHidden = false, Rating = 3 },
            new SongModel { IsHidden = false, Rating = 3 }
        };

        // Act - Shuffle twice
        var result1 = _service.WeightedShuffle(songs, _service.CalculateWeight);
        var result2 = _service.WeightedShuffle(songs, _service.CalculateWeight);

        // Assert - Results should be different (with high probability)
        bool allSameOrder = true;
        for (int i = 0; i < result1.Count; i++)
        {
            if (result1[i] != result2[i])
            {
                allSameOrder = false;
                break;
            }
        }
        
        Assert.False(allSameOrder, "Weighted shuffle should produce different orders");
    }

    [Fact]
    public void WeightedShuffle_ComplexScenario_ProducesValidResult()
    {
        // Arrange - Create a realistic music library scenario
        var songs = new List<SongModel>
        {
            // Favorite highly-rated songs
            new SongModel 
            { 
                IsHidden = false, 
                Rating = 5, 
                IsFavorite = true, 
                ManualFavoriteCount = 3,
                PlayCompletedCount = 50,
                SkipCount = 2,
                LastPlayed = DateTimeOffset.UtcNow.AddDays(-5)
            },
            // Good song but played recently
            new SongModel 
            { 
                IsHidden = false, 
                Rating = 4, 
                IsFavorite = true,
                PlayCompletedCount = 30,
                SkipCount = 1,
                LastPlayed = DateTimeOffset.UtcNow.AddHours(-2)
            },
            // Mediocre song
            new SongModel 
            { 
                IsHidden = false, 
                Rating = 2,
                PlayCompletedCount = 10,
                SkipCount = 8,
                LastPlayed = DateTimeOffset.UtcNow.AddDays(-20)
            },
            // Never played song
            new SongModel 
            { 
                IsHidden = false, 
                Rating = 3,
                LastPlayed = default
            },
            // Hidden song (should not appear)
            new SongModel 
            { 
                IsHidden = true, 
                Rating = 5,
                IsFavorite = true
            }
        };

        // Act
        var result = _service.WeightedShuffle(songs, _service.CalculateWeight);

        // Assert
        Assert.Equal(4, result.Count); // Hidden song excluded
        Assert.DoesNotContain(result, s => s.IsHidden);
        Assert.All(result, song => Assert.NotNull(song));
    }
}
