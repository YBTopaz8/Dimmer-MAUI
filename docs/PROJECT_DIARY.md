# Dimmer-MAUI Project Diary

## Introduction

This document serves as a "development diary" for Dimmer-MAUI, capturing the key decisions, challenges, and evolution of the project. It's written for **DOT** (Developer Over Time) - the future version of yourself or other developers who will work on this codebase.

The purpose is to explain **why** certain architectural choices were made, what problems were solved, and the reasoning behind major features. This context helps prevent repeating past mistakes and preserves the institutional knowledge that might otherwise be lost.

## Project Genesis

### Why Dimmer Exists

Dimmer was born from a personal need: the music apps I relied on were discontinued:
- **Dopamine** (Windows) - Discontinued
- **Retro Music Player** (Android) - Discontinued

Rather than settling for alternatives that didn't match my workflow, I decided to build a cross-platform music player that could run on both Windows and Android, with the exact features I wanted.

### Technology Choice: .NET MAUI

**Decision**: Use .NET MAUI for cross-platform development

**Why MAUI over alternatives?**

1. **Single Codebase**: Write once, run on Windows and Android
2. **C# Expertise**: Leverage existing C# knowledge
3. **Native Performance**: Compiles to native code, not web-based
4. **Active Ecosystem**: Microsoft-backed with strong community
5. **Xamarin Evolution**: Natural progression from Xamarin.Forms

**Challenges Accepted**:
- MAUI was relatively new (and still maturing)
- Platform-specific quirks require workarounds
- Some features need platform-specific implementations

**Lesson**: The cross-platform dream isn't free, but the shared codebase (70-80%) justifies the occasional platform-specific detours.

## Major Architectural Decisions

### Decision 1: Realm as the Database

**Problem**: Need a fast, local database for music metadata that works across platforms.

**Considered**:
- SQLite: Traditional choice, requires more boilerplate
- LiteDB: .NET-native, but less mature
- **Realm**: Object database with reactive queries

**Why Realm?**
- **Object-Oriented**: No SQL, just POCO classes
- **Reactive**: Built-in change notifications
- **Fast**: Optimized for mobile
- **Cross-Platform**: Works on Windows and Android
- **Zero-Copy Architecture**: Direct object access without serialization

**Trade-offs**:
- Learning curve for object database paradigm
- Schema migrations can be tricky
- Less flexibility than SQL for complex queries (but we built TQL to compensate!)

**Lesson**: Realm's reactive nature pairs perfectly with MVVM and reactive UI patterns.

### Decision 2: MVVM + Reactive Programming

**Problem**: Keep UI responsive while handling async operations (file scanning, playback, network requests).

**Approach**: MVVM + Reactive Extensions (Rx) + DynamicData

**Why This Combination?**

1. **MVVM**: Separation of concerns, testability, data binding
2. **Rx (System.Reactive)**: Composable async event streams
3. **DynamicData**: Reactive collections with automatic UI updates
4. **CommunityToolkit.Mvvm**: Reduces boilerplate with source generators

**Example Use Case**: Music Library Scanning

```csharp
// Old way: Manually update ObservableCollection
foreach (var song in scannedSongs)
{
    Songs.Add(song); // UI freezes on each add
}

// Reactive way: Stream updates
_sourceList.AddRange(scannedSongs); // DynamicData handles UI updates efficiently
```

**Lesson**: Reactive programming has a steep learning curve, but once you "get it", async UI updates become trivial.

### Decision 3: TQL (Text Query Language)

**Problem**: Music libraries grow large (1000s of songs). Users need powerful search beyond simple keyword matching.

**Inspiration**:
- Gmail's search operators
- Spotify's filter syntax
- SQL WHERE clauses (but human-friendly)

**Design Goals**:
1. **Human-Readable**: `artist:Drake year:>2015` not `artist='Drake' AND year>2015`
2. **Forgiving**: Handle typos with fuzzy matching
3. **Powerful**: Support complex boolean logic, ranges, sorting
4. **Fast**: Parse queries in milliseconds

**Implementation Highlights**:
- Custom parser written from scratch (no parser generator)
- Converts text → structured query → predicates
- Integrates with DynamicData for reactive filtering

**Why Not Use Existing Solutions?**
- Lucene: Too complex for end users
- SQL: Too technical
- Simple keyword search: Too limited

**Lesson**: Building a custom DSL (Domain-Specific Language) is hard but incredibly rewarding. TQL became a signature feature that sets Dimmer apart.

### Decision 4: Parse Backend for Cloud Features

**Problem**: Enable social features (friends, chat, shared playlists) and cross-device sync.

**Why Parse?**
- **Open Source**: Self-hostable backend-as-a-service
- **Live Queries**: Real-time data sync out of the box
- **User Management**: Built-in authentication
- **Cost**: Free for self-hosting, cheap hosted options
- **.NET SDK**: First-class .NET support

**Alternative Considered**:
- Firebase: Vendor lock-in, expensive at scale
- Custom API: Too much work for MVP

**Trade-off**: Parse has some quirks (especially with .NET SDK), but the productivity gain is massive.

**Lesson**: Use BaaS (Backend-as-a-Service) for MVP. Build custom backend only if needed.

### Decision 5: Dependency Injection Everywhere

**Problem**: Managing service lifetimes and dependencies manually leads to spaghetti code.

**Solution**: Use .NET's built-in DI container

**Pattern**:
```csharp
// Register services
services.AddSingleton<IAudioService, AudioService>();

// Inject via constructor
public class HomeViewModel(IAudioService audioService)
{
    private readonly IAudioService _audio = audioService;
}
```

**Benefits**:
- Testability (mock dependencies)
- Loose coupling
- Clear dependencies (constructor shows what's needed)
- Lifetime management (singleton, transient, scoped)

**Lesson**: DI isn't just for "enterprise" apps. Even small projects benefit from explicit dependency management.

## Feature Evolution

### Phase 1: MVP (Minimum Viable Player)

**Goal**: Play music from local files

**Features**:
- File scanning
- Basic playback (play, pause, skip)
- Simple playlist queue
- Album art display

**Lesson**: Start simple. Shipping something that works is better than planning the perfect architecture forever.

### Phase 2: Metadata & Organization

**Added**:
- Album/Artist grouping
- Genre classification
- Ratings and favorites
- Basic search

**Challenge**: Metadata extraction from various audio formats
**Solution**: ATL (AudioToolsLibrary) handles 30+ audio formats

**Lesson**: Don't reinvent the wheel for domain-specific problems. Use specialized libraries.

### Phase 3: Lyrics Integration

**User Request**: Display synchronized lyrics while playing

**Implementation**:
1. Extract embedded lyrics from audio files
2. Parse .lrc (LRC format) files
3. Fetch lyrics from online APIs (LrcLib)
4. Display with real-time highlighting

**Challenge**: Syncing lyrics with playback across different audio backends (Windows vs Android)

**Solution**: Abstract timing logic into platform-independent service

**Lesson**: User-requested features often become the most beloved. Listen to your users.

### Phase 4: TQL (Advanced Search)

**Problem**: Simple search couldn't handle complex queries like "rock songs from the 90s by bands starting with 'B', sorted by rating"

**Solution**: Build a custom query language

**Development Time**: 2-3 weeks of intense work

**Result**: Most technically impressive feature, but also most complex codebase

**Lesson**: Sometimes the best solution is the one you build yourself. But only if you're willing to maintain it.

### Phase 5: Social Features (DimmerLive)

**Vision**: Share music experiences with friends

**Features**:
- See what friends are listening to
- Share songs/playlists
- Real-time chat
- Multi-device sync

**Challenge**: Real-time sync is hard
**Solution**: Parse LiveQuery handles the heavy lifting

**Status**: Experimental but functional

**Lesson**: Social features are exciting but increase complexity exponentially. Start small (read-only sharing) before building full collaboration.

### Phase 6: Statistics & Achievements

**User Request**: "Show me listening stats like Spotify Wrapped"

**Implementation**:
- Track play counts, skip counts, listen time
- Calculate top artists/albums/songs
- Achievement system (milestones)
- Data visualization with charts

**Challenge**: Storing and querying time-series data efficiently in Realm

**Solution**: Aggregate data daily, store historical snapshots

**Lesson**: Users love data about themselves. Analytics features drive engagement.

## Technical Challenges & Solutions

### Challenge 1: Cross-Platform Audio Playback

**Problem**: Windows and Android have completely different audio APIs

**Solution**: Platform-specific implementations behind a shared interface

```csharp
// Shared interface
public interface IDimmerAudioService
{
    Task PlayAsync(string filePath);
    Task PauseAsync();
    // ...
}

// Windows implementation (AudioSwitcher + CoreAudio)
public class WindowsAudioService : IDimmerAudioService { }

// Android implementation (MediaPlayer)
public class AndroidAudioService : IDimmerAudioService { }
```

**Lesson**: Abstraction is your friend. Hide platform differences behind interfaces.

### Challenge 2: Large Library Performance

**Problem**: Scanning 10,000+ songs freezes the UI

**Solutions**:
1. **Background Scanning**: Use Task.Run for I/O operations
2. **Batched Updates**: Update UI in chunks, not per-file
3. **Virtual Scrolling**: Render only visible items (CollectionView)
4. **Indexed Queries**: Realm indexes on frequently-searched fields

**Lesson**: Performance optimization is iterative. Profile first, optimize second.

### Challenge 3: Album Art Memory Usage

**Problem**: Loading high-res album art for thousands of songs causes OutOfMemory exceptions

**Solutions**:
1. **Lazy Loading**: Load art only when visible
2. **Image Caching**: Cache resized thumbnails (Hoarder system)
3. **Weak References**: Allow GC to reclaim unused images
4. **Format Optimization**: Convert to efficient formats (WebP)

**Lesson**: Images are often the biggest memory hog. Optimize aggressively.

### Challenge 4: Realm Schema Migrations

**Problem**: Changing data models breaks existing databases

**Solution**: Implement migration logic for schema changes

```csharp
var config = new RealmConfiguration
{
    SchemaVersion = 5,
    MigrationCallback = (migration, oldVersion) =>
    {
        if (oldVersion < 5)
        {
            // Migrate from v4 to v5
            var oldSongs = migration.OldRealm.All<Song>();
            var newSongs = migration.NewRealm.All<Song>();
            // ... migration logic
        }
    }
};
```

**Lesson**: Design schema carefully. Migrations are painful but necessary for evolving apps.

### Challenge 5: Parse .NET SDK Quirks

**Problem**: Parse's .NET SDK is less mature than JavaScript/iOS/Android SDKs

**Issues Encountered**:
- LiveQuery disconnects randomly
- Serialization issues with custom types
- Limited documentation

**Solutions**:
- Wrapper classes to abstract Parse quirks
- Custom retry logic for LiveQuery
- Extensive testing and error handling

**Lesson**: Bleeding-edge libraries come with sharp edges. Have a fallback plan.

## Design Patterns in Use

### 1. Repository Pattern
Abstracts data access layer (Realm) from business logic.

### 2. Service Layer Pattern
Encapsulates business logic into services (AudioService, LibraryScannerService, etc.).

### 3. MVVM Pattern
Separates UI (Views) from logic (ViewModels).

### 4. Observer Pattern (Reactive)
Components react to changes via observable streams.

### 5. Factory Pattern
`RealmFactory` creates Realm instances with correct configuration.

### 6. Strategy Pattern
Different audio backends (Windows/Android) implement same interface.

### 7. Facade Pattern
`BaseViewModel` provides simplified interface to common ViewModel functionality.

## Lessons Learned

### 1. Start Simple, Iterate Often
MVP → User Feedback → Iterate. Don't build features nobody asked for.

### 2. Platform Abstractions are Key
Design shared interfaces early. Platform-specific code should be isolated.

### 3. Reactive Programming Pays Off
Initial complexity is high, but async UI updates become trivial.

### 4. Test Edge Cases
Users will do things you never imagined. Null checks, empty states, and error handling matter.

### 5. Documentation is for Future You
Write docs as you code, not after. Future you will be grateful.

### 6. User Feedback Drives Features
The best features (lyrics, TQL, stats) came from user requests.

### 7. Performance Matters
A beautiful app that lags is a bad app. Profile early, optimize often.

### 8. Open Source Libraries Save Time
Don't reinvent audio parsing, image processing, or HTTP clients. Focus on your unique value.

## Future Directions

### Planned Features

1. **Cloud Sync**: Sync library metadata across devices (not files, just metadata)
2. **Smart Playlists**: Auto-updating playlists based on TQL queries
3. **Music Discovery**: Recommendations based on listening history
4. **Podcast Support**: Extend beyond music
5. **Equalizer**: Built-in EQ for sound customization
6. **Car Mode**: Simplified UI for driving
7. **Sleep Timer**: Auto-pause after duration
8. **Crossfade**: Smooth transitions between songs

### Technical Debt to Address

1. **Refactor Large ViewModels**: Some ViewModels have grown too large (500+ lines)
2. **Improve Test Coverage**: Current coverage is ~40%, target 70%+
3. **Reduce Parse Coupling**: Abstract Parse behind generic interfaces for easier switching
4. **Optimize Startup Time**: App takes 2-3 seconds to start on Android
5. **Improve Error Messages**: Make errors more user-friendly

### Dream Features (Maybe Someday)

- **iOS Support**: If Apple's restrictions loosen
- **Web Player**: Browser-based remote control
- **AI-Powered Features**: Mood detection, auto-tagging, smart mixes
- **Streaming Integration**: Spotify/YouTube Music integration
- **Collaboration**: Real-time shared listening sessions

## Closing Thoughts

Building Dimmer has been a journey of learning, iteration, and occasional frustration. The codebase isn't perfect (no codebase is), but it represents the best decisions I could make with the knowledge and constraints at each point in time.

If you're reading this as a new contributor or future maintainer:

1. **Don't be afraid to refactor**: If something doesn't make sense, it probably needs improvement.
2. **Ask questions**: Use GitHub Discussions or Issues. There's no shame in not knowing.
3. **Respect user data**: Music libraries are personal. Never lose user data.
4. **Have fun**: This is a music player. Music is joy. Let that guide your work.

Thank you for being part of Dimmer's journey. 🎵

---

**Last Updated**: December 30, 2025  
**Author**: Yvan Brunel (YBTopaz8)  
**Version**: 1.5.6
