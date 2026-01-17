using Dimmer.Data.Models;
using Dimmer.Interfaces;
using Dimmer.Interfaces.Services;
using Moq;
using MongoDB.Bson;
using Xunit;

namespace Dimmer.Tests;

/// <summary>
/// Tests for Musical Work linking and rendition management functionality
/// </summary>
public class MusicalWorkServiceTests
{
    private readonly Mock<IRepository<MusicalWorkModel>> _mockWorkRepo;
    private readonly Mock<IRepository<SongModel>> _mockSongRepo;
    private readonly MusicalWorkService _service;

    public MusicalWorkServiceTests()
    {
        _mockWorkRepo = new Mock<IRepository<MusicalWorkModel>>();
        _mockSongRepo = new Mock<IRepository<SongModel>>();
        _service = new MusicalWorkService(_mockWorkRepo.Object, _mockSongRepo.Object);
    }

    [Fact]
    public void CreateWork_WithValidTitle_ShouldCreateWork()
    {
        // Arrange
        var title = "Bohemian Rhapsody";
        var composer = "Freddie Mercury";
        var artist = "Queen";

        _mockWorkRepo.Setup(r => r.Create(It.IsAny<MusicalWorkModel>()))
            .Returns((MusicalWorkModel w) => w);

        // Act
        var result = _service.CreateWork(title, composer, artist);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(title, result.Title);
        Assert.Equal(composer, result.Composer);
        Assert.Equal(artist, result.CanonicalArtist);
        Assert.Equal(0, result.RenditionCount);
        _mockWorkRepo.Verify(r => r.Create(It.IsAny<MusicalWorkModel>()), Times.Once);
    }

    [Fact]
    public void CreateWork_WithEmptyTitle_ShouldThrowArgumentException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentException>(() => _service.CreateWork(""));
        Assert.Throws<ArgumentException>(() => _service.CreateWork("   "));
    }

    [Fact]
    public void CreateWork_WithNullTitle_ShouldThrowArgumentException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentException>(() => _service.CreateWork(null!));
    }

    [Fact]
    public void LinkSongToWork_WithValidIds_ShouldLinkSuccessfully()
    {
        // Arrange
        var songId = ObjectId.GenerateNewId();
        var workId = ObjectId.GenerateNewId();
        var work = new MusicalWorkModel { Id = workId, Title = "Test Work" };

        _mockWorkRepo.Setup(r => r.GetById(workId)).Returns(work);
        _mockSongRepo.Setup(r => r.Update(songId, It.IsAny<Action<SongModel>>()))
            .Returns(true);
        _mockSongRepo.Setup(r => r.Query(It.IsAny<System.Linq.Expressions.Expression<Func<SongModel, bool>>>()))
            .Returns(new List<SongModel>());
        _mockWorkRepo.Setup(r => r.Update(workId, It.IsAny<Action<MusicalWorkModel>>()))
            .Returns(true);

        // Act
        var result = _service.LinkSongToWork(songId, workId);

        // Assert
        Assert.True(result);
        _mockSongRepo.Verify(r => r.Update(songId, It.IsAny<Action<SongModel>>()), Times.Once);
    }

    [Fact]
    public void UnlinkSongFromWork_WithValidId_ShouldUnlinkSuccessfully()
    {
        // Arrange
        var songId = ObjectId.GenerateNewId();
        var workId = ObjectId.GenerateNewId();

        _mockSongRepo.Setup(r => r.Update(songId, It.IsAny<Action<SongModel>>()))
            .Returns(true)
            .Callback<ObjectId, Action<SongModel>>((id, action) =>
            {
                var song = new SongModel { Id = songId, MusicalWork = new MusicalWorkModel { Id = workId } };
                action(song);
            });

        _mockSongRepo.Setup(r => r.Query(It.IsAny<System.Linq.Expressions.Expression<Func<SongModel, bool>>>()))
            .Returns(new List<SongModel>());
        _mockWorkRepo.Setup(r => r.Update(workId, It.IsAny<Action<MusicalWorkModel>>()))
            .Returns(true);

        // Act
        var result = _service.UnlinkSongFromWork(songId);

        // Assert
        Assert.True(result);
        _mockSongRepo.Verify(r => r.Update(songId, It.IsAny<Action<SongModel>>()), Times.Once);
    }

    [Fact]
    public void GetRenditions_WithValidWorkId_ShouldReturnAllRenditions()
    {
        // Arrange
        var workId = ObjectId.GenerateNewId();
        var work = new MusicalWorkModel { Id = workId, Title = "Test Work" };
        var renditions = new List<SongModel>
        {
            new SongModel { Id = ObjectId.GenerateNewId(), Title = "Studio Version" },
            new SongModel { Id = ObjectId.GenerateNewId(), Title = "Live Version" },
            new SongModel { Id = ObjectId.GenerateNewId(), Title = "Acoustic Version" }
        };

        _mockWorkRepo.Setup(r => r.GetById(workId)).Returns(work);
        _mockSongRepo.Setup(r => r.Query(It.IsAny<System.Linq.Expressions.Expression<Func<SongModel, bool>>>()))
            .Returns(renditions);

        // Act
        var result = _service.GetRenditions(workId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void GetRenditions_WithInvalidWorkId_ShouldReturnEmptyList()
    {
        // Arrange
        var workId = ObjectId.GenerateNewId();
        _mockWorkRepo.Setup(r => r.GetById(workId)).Returns((MusicalWorkModel?)null);

        // Act
        var result = _service.GetRenditions(workId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void GetFilteredRenditions_WithInstrumentalFilter_ShouldReturnOnlyInstrumental()
    {
        // Arrange
        var workId = ObjectId.GenerateNewId();
        var work = new MusicalWorkModel { Id = workId, Title = "Test Work" };
        var renditions = new List<SongModel>
        {
            new SongModel { Id = ObjectId.GenerateNewId(), Title = "Vocal Version", IsInstrumental = false },
            new SongModel { Id = ObjectId.GenerateNewId(), Title = "Instrumental Version", IsInstrumental = true },
            new SongModel { Id = ObjectId.GenerateNewId(), Title = "Piano Instrumental", IsInstrumental = true }
        };

        _mockWorkRepo.Setup(r => r.GetById(workId)).Returns(work);
        _mockSongRepo.Setup(r => r.Query(It.IsAny<System.Linq.Expressions.Expression<Func<SongModel, bool>>>()))
            .Returns(renditions);

        // Act
        var result = _service.GetFilteredRenditions(workId, instrumentalOnly: true);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.True(r.IsInstrumental));
    }

    [Fact]
    public void GetFilteredRenditions_WithLiveFilter_ShouldReturnOnlyLive()
    {
        // Arrange
        var workId = ObjectId.GenerateNewId();
        var work = new MusicalWorkModel { Id = workId, Title = "Test Work" };
        var renditions = new List<SongModel>
        {
            new SongModel { Id = ObjectId.GenerateNewId(), Title = "Studio Version", IsLivePerformance = false },
            new SongModel { Id = ObjectId.GenerateNewId(), Title = "Live at Wembley", IsLivePerformance = true },
            new SongModel { Id = ObjectId.GenerateNewId(), Title = "Live at Madison Square Garden", IsLivePerformance = true }
        };

        _mockWorkRepo.Setup(r => r.GetById(workId)).Returns(work);
        _mockSongRepo.Setup(r => r.Query(It.IsAny<System.Linq.Expressions.Expression<Func<SongModel, bool>>>()))
            .Returns(renditions);

        // Act
        var result = _service.GetFilteredRenditions(workId, liveOnly: true);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.True(r.IsLivePerformance));
    }

    [Fact]
    public void GetFilteredRenditions_WithInstrumentationFilter_ShouldReturnMatchingInstrumentation()
    {
        // Arrange
        var workId = ObjectId.GenerateNewId();
        var work = new MusicalWorkModel { Id = workId, Title = "Test Work" };
        var renditions = new List<SongModel>
        {
            new SongModel { Id = ObjectId.GenerateNewId(), Title = "Piano Version", Instrumentation = "Piano" },
            new SongModel { Id = ObjectId.GenerateNewId(), Title = "Cello Version", Instrumentation = "Cello" },
            new SongModel { Id = ObjectId.GenerateNewId(), Title = "Piano and Strings", Instrumentation = "Piano, Strings" }
        };

        _mockWorkRepo.Setup(r => r.GetById(workId)).Returns(work);
        _mockSongRepo.Setup(r => r.Query(It.IsAny<System.Linq.Expressions.Expression<Func<SongModel, bool>>>()))
            .Returns(renditions);

        // Act
        var result = _service.GetFilteredRenditions(workId, instrumentation: "Piano");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.Contains("Piano", r.Instrumentation ?? "", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void UpdateRenditionMetadata_WithAllFields_ShouldUpdateSuccessfully()
    {
        // Arrange
        var songId = ObjectId.GenerateNewId();
        _mockSongRepo.Setup(r => r.Update(songId, It.IsAny<Action<SongModel>>()))
            .Returns(true);

        // Act
        var result = _service.UpdateRenditionMetadata(
            songId,
            renditionType: "Live",
            instrumentation: "Acoustic Guitar",
            isLive: true,
            isRemix: false,
            isCover: false,
            notes: "Recorded at Abbey Road Studios"
        );

        // Assert
        Assert.True(result);
        _mockSongRepo.Verify(r => r.Update(songId, It.IsAny<Action<SongModel>>()), Times.Once);
    }

    [Fact]
    public void UpdateWorkStatistics_WithRenditions_ShouldAggregateCorrectly()
    {
        // Arrange
        var workId = ObjectId.GenerateNewId();
        var work = new MusicalWorkModel { Id = workId, Title = "Test Work" };
        var renditions = new List<SongModel>
        {
            new SongModel 
            { 
                Id = ObjectId.GenerateNewId(), 
                PlayCount = 100, 
                PlayCompletedCount = 80,
                PopularityScore = 0.8,
                LastPlayed = DateTimeOffset.UtcNow.AddDays(-1)
            },
            new SongModel 
            { 
                Id = ObjectId.GenerateNewId(), 
                PlayCount = 50, 
                PlayCompletedCount = 40,
                PopularityScore = 0.6,
                LastPlayed = DateTimeOffset.UtcNow.AddDays(-5)
            }
        };

        _mockWorkRepo.Setup(r => r.GetById(workId)).Returns(work);
        _mockSongRepo.Setup(r => r.Query(It.IsAny<System.Linq.Expressions.Expression<Func<SongModel, bool>>>()))
            .Returns(renditions);
        _mockWorkRepo.Setup(r => r.Update(workId, It.IsAny<Action<MusicalWorkModel>>()))
            .Returns(true)
            .Callback<ObjectId, Action<MusicalWorkModel>>((id, action) =>
            {
                action(work);
            });

        // Act
        _service.UpdateWorkStatistics(workId);

        // Assert
        Assert.Equal(2, work.RenditionCount);
        Assert.Equal(150, work.TotalPlayCount);
        Assert.Equal(120, work.TotalPlayCompletedCount);
        Assert.Equal(0.7, work.PopularityScore, 0.01);
    }

    [Fact]
    public void DeleteWork_WithValidId_ShouldUnlinkRenditionsAndDelete()
    {
        // Arrange
        var workId = ObjectId.GenerateNewId();
        var work = new MusicalWorkModel { Id = workId, Title = "Test Work" };
        var renditions = new List<SongModel>
        {
            new SongModel { Id = ObjectId.GenerateNewId(), Title = "Version 1" },
            new SongModel { Id = ObjectId.GenerateNewId(), Title = "Version 2" }
        };

        _mockWorkRepo.Setup(r => r.GetById(workId)).Returns(work);
        _mockSongRepo.Setup(r => r.Query(It.IsAny<System.Linq.Expressions.Expression<Func<SongModel, bool>>>()))
            .Returns(renditions);
        _mockSongRepo.Setup(r => r.Update(It.IsAny<ObjectId>(), It.IsAny<Action<SongModel>>()))
            .Returns(true);
        _mockWorkRepo.Setup(r => r.Delete(work));

        // Act
        var result = _service.DeleteWork(workId);

        // Assert
        Assert.True(result);
        _mockSongRepo.Verify(r => r.Update(It.IsAny<ObjectId>(), It.IsAny<Action<SongModel>>()), Times.Exactly(2));
        _mockWorkRepo.Verify(r => r.Delete(work), Times.Once);
    }

    [Fact]
    public void SearchWorksByTitle_WithMatchingTerm_ShouldReturnWorks()
    {
        // Arrange
        var searchTerm = "Rhapsody";
        var works = new List<MusicalWorkModel>
        {
            new MusicalWorkModel { Id = ObjectId.GenerateNewId(), Title = "Bohemian Rhapsody" },
            new MusicalWorkModel { Id = ObjectId.GenerateNewId(), Title = "Rhapsody in Blue" }
        };

        _mockWorkRepo.Setup(r => r.Query(It.IsAny<System.Linq.Expressions.Expression<Func<MusicalWorkModel, bool>>>()))
            .Returns(works);

        // Act
        var result = _service.SearchWorksByTitle(searchTerm);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void SearchWorksByTitle_WithEmptyTerm_ShouldReturnEmptyList()
    {
        // Arrange & Act
        var result = _service.SearchWorksByTitle("");

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void SuggestMatchingWorks_ForUnlinkedSong_ShouldReturnSuggestions()
    {
        // Arrange
        var songId = ObjectId.GenerateNewId();
        var song = new SongModel 
        { 
            Id = songId, 
            Title = "Bohemian Rhapsody (Live)",
            ArtistName = "Queen",
            MusicalWork = null
        };

        var works = new List<MusicalWorkModel>
        {
            new MusicalWorkModel 
            { 
                Id = ObjectId.GenerateNewId(), 
                Title = "Bohemian Rhapsody",
                CanonicalArtist = "Queen"
            },
            new MusicalWorkModel 
            { 
                Id = ObjectId.GenerateNewId(), 
                Title = "We Will Rock You",
                CanonicalArtist = "Queen"
            }
        };

        _mockSongRepo.Setup(r => r.GetById(songId)).Returns(song);
        _mockWorkRepo.Setup(r => r.GetAll(false)).Returns(works);

        // Act
        var result = _service.SuggestMatchingWorks(songId, maxResults: 5);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        // The first suggestion should be "Bohemian Rhapsody" due to title similarity
        Assert.Contains(result, s => s.Work.Title == "Bohemian Rhapsody");
    }

    [Fact]
    public void SuggestMatchingWorks_ForAlreadyLinkedSong_ShouldReturnEmpty()
    {
        // Arrange
        var songId = ObjectId.GenerateNewId();
        var workId = ObjectId.GenerateNewId();
        var song = new SongModel 
        { 
            Id = songId, 
            Title = "Test Song",
            MusicalWork = new MusicalWorkModel { Id = workId }
        };

        _mockSongRepo.Setup(r => r.GetById(songId)).Returns(song);

        // Act
        var result = _service.SuggestMatchingWorks(songId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void SuggestMatchingSongs_ForWork_ShouldReturnUnlinkedMatches()
    {
        // Arrange
        var workId = ObjectId.GenerateNewId();
        var work = new MusicalWorkModel 
        { 
            Id = workId, 
            Title = "Bohemian Rhapsody",
            CanonicalArtist = "Queen"
        };

        var unlinkedSongs = new List<SongModel>
        {
            new SongModel 
            { 
                Id = ObjectId.GenerateNewId(), 
                Title = "Bohemian Rhapsody (Piano Version)",
                ArtistName = "Queen",
                MusicalWork = null
            },
            new SongModel 
            { 
                Id = ObjectId.GenerateNewId(), 
                Title = "Bohemian Rhapsody - Live at Wembley",
                ArtistName = "Queen",
                MusicalWork = null
            }
        };

        _mockWorkRepo.Setup(r => r.GetById(workId)).Returns(work);
        _mockSongRepo.Setup(r => r.Query(It.IsAny<System.Linq.Expressions.Expression<Func<SongModel, bool>>>()))
            .Returns(unlinkedSongs);

        // Act
        var result = _service.SuggestMatchingSongs(workId, maxResults: 10);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.All(result, s => Assert.NotNull(s.Song));
        Assert.All(result, s => Assert.True(s.ConfidenceScore > 0));
    }

    [Fact]
    public void GetWork_WithValidId_ShouldReturnWork()
    {
        // Arrange
        var workId = ObjectId.GenerateNewId();
        var work = new MusicalWorkModel { Id = workId, Title = "Test Work" };
        _mockWorkRepo.Setup(r => r.GetById(workId)).Returns(work);

        // Act
        var result = _service.GetWork(workId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(workId, result.Id);
        Assert.Equal("Test Work", result.Title);
    }

    [Fact]
    public void GetWork_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        var workId = ObjectId.GenerateNewId();
        _mockWorkRepo.Setup(r => r.GetById(workId)).Returns((MusicalWorkModel?)null);

        // Act
        var result = _service.GetWork(workId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetAllWorks_ShouldReturnAllWorks()
    {
        // Arrange
        var works = new List<MusicalWorkModel>
        {
            new MusicalWorkModel { Id = ObjectId.GenerateNewId(), Title = "Work 1" },
            new MusicalWorkModel { Id = ObjectId.GenerateNewId(), Title = "Work 2" },
            new MusicalWorkModel { Id = ObjectId.GenerateNewId(), Title = "Work 3" }
        };

        _mockWorkRepo.Setup(r => r.GetAll(false)).Returns(works);

        // Act
        var result = _service.GetAllWorks();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
    }
}
