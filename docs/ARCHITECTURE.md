# Dimmer-MAUI Architecture Documentation

## Overview

Dimmer is a cross-platform music player application built using **.NET MAUI** (Multi-platform App UI), targeting Windows and Android platforms. The application follows modern software architecture patterns including MVVM (Model-View-ViewModel), dependency injection, and reactive programming.

## Technology Stack

### Core Frameworks
- **.NET 9.0**: Target framework
- **MAUI**: Cross-platform UI framework
- **C# (Preview Language Version)**: Programming language

### Key Libraries & Dependencies

#### UI & User Experience
- **CommunityToolkit.Maui** (v12.2.0): Enhanced MAUI controls and utilities
- **CommunityToolkit.Mvvm** (v8.4.0): MVVM helpers and source generators
- **SkiaSharp** (v3.119.1): 2D graphics rendering

#### Data Management
- **Realm** (v20.1.0): Mobile database for local data persistence
- **System.Reactive** (v6.1.0): Reactive extensions for .NET
- **DynamicData** (v9.4.1): Reactive collections and observable list management

#### Audio Processing
- **ATL** (AudioToolsLibrary): Audio file metadata reading/writing
- **AudioSwitcher.AudioApi.CoreAudio**: Windows audio device management
- **Xabe.FFmpeg** (v6.0.2): Audio/video processing

#### Networking & Cloud Services
- **Parse SDK**: Backend-as-a-Service for user authentication, cloud data, and live queries
- **Last.fm API (Hqub.Lastfm)**: Music metadata and scrobbling
- **LrcLib**: Online lyrics provider

#### Utilities
- **Fastenshtein** (v1.0.11): Fuzzy string matching (Levenshtein distance)
- **System.Linq.Dynamic.Core** (v1.6.9): Dynamic LINQ queries
- **Microsoft.Extensions.Configuration.Json**: Application configuration management

## Project Structure

```
Dimmer-MAUI/
├── Dimmer/                          # Main application code
│   ├── Dimmer/                      # Shared cross-platform code
│   │   ├── Data/                    # Data models and Realm schemas
│   │   │   ├── Models/              # Core data models
│   │   │   ├── ModelView/           # View models for UI binding
│   │   │   └── RealmStaticFilters/  # Predefined Realm queries
│   │   ├── DimmerLive/              # Real-time collaboration features
│   │   │   ├── Models/              # Live session data models
│   │   │   ├── Orchestration/       # Live session orchestrators
│   │   │   └── Interfaces/          # Live session contracts
│   │   ├── DimmerSearch/            # Advanced search engine (TQL)
│   │   │   ├── TQL/                 # TQL parser implementation
│   │   │   ├── TQLActions/          # TQL command executors
│   │   │   ├── TQLDoc/              # TQL documentation
│   │   │   └── Exceptions/          # Search-related exceptions
│   │   ├── Interfaces/              # Service contracts
│   │   │   ├── IDatabase/           # Database interfaces
│   │   │   └── Services/            # Service interfaces
│   │   ├── Orchestration/           # Business logic orchestrators
│   │   ├── ViewModel/               # ViewModels for MVVM
│   │   ├── UIUtils/                 # UI helper classes
│   │   ├── Utilities/               # Cross-cutting concerns
│   │   │   ├── Enums/               # Enumerations
│   │   │   ├── Events/              # Event definitions
│   │   │   ├── Extensions/          # Extension methods
│   │   │   ├── CustomAnimations/    # Animation utilities
│   │   │   ├── StatsUtils/          # Statistics helpers
│   │   │   └── ViewsUtils/          # View helpers
│   │   ├── Charts/                  # Data visualization
│   │   ├── GraphSupport/            # Music relationship graphs
│   │   ├── Hoarder/                 # File caching system
│   │   ├── LastFM/                  # Last.fm integration
│   │   └── Resources/               # App resources
│   │       ├── Fonts/               # Custom fonts (Material Icons, FontAwesome)
│   │       ├── Images/              # Image assets
│   │       ├── Localization/        # i18n resources
│   │       ├── Raw/                 # Raw assets
│   │       └── Styles/              # XAML styles
│   ├── Dimmer.WinUI/                # Windows-specific implementation
│   └── Dimmer.Droid/                # Android-specific implementation
├── Dimmer.Tests/                    # Unit tests
├── DimmerTQLUnitTest/              # TQL query language tests
├── AudioSwitcher-master/            # Audio device switching library
├── atldotnet-main/                  # Audio metadata library
└── docs/                            # Documentation (this folder)
```

## Architecture Patterns

### 1. MVVM (Model-View-ViewModel)

The application strictly follows the MVVM pattern:

- **Models** (`Data/Models/`): Core domain entities (Song, Album, Artist, Playlist, etc.)
- **ViewModels** (`ViewModel/`): Presentation logic and state management
  - Uses `CommunityToolkit.Mvvm` for property change notifications
  - Implements `INotifyPropertyChanged` via source generators
  - Contains command implementations using `RelayCommand`
- **Views** (XAML files): UI declaration with data binding

### 2. Dependency Injection

All services are registered in `ServiceRegistration.cs` and injected via constructor injection:

```csharp
// Service registration
services.AddSingleton<IAudioService, AudioService>();
services.AddSingleton<ILibraryScannerService, LibraryScannerService>();

// Constructor injection in ViewModels
public class HomeViewModel(IAudioService audioService, ILibraryScannerService scanner)
{
    private readonly IAudioService _audioService = audioService;
    private readonly ILibraryScannerService _scanner = scanner;
}
```

### 3. Repository Pattern

Data access is abstracted through the repository pattern:

- **Generic Repository**: `IRepository<T>` implemented by `RealmCoreRepo<T>`
- **Specialized Repositories**: Type-specific repositories for complex queries
- **Realm Database**: Primary data store for all music metadata

### 4. Reactive Programming

The application heavily uses reactive extensions (Rx):

- **Observable Collections**: `DynamicData` for reactive collection transformations
- **Event Streams**: `System.Reactive` for asynchronous event handling
- **Live Queries**: Parse LiveQuery for real-time cloud data synchronization

### 5. Service Layer Architecture

Services are organized into logical domains:

#### Core Services
- **ISettingsService**: Application configuration management
- **IDimmerStateService**: Global application state
- **IErrorHandler**: Centralized error handling and logging
- **IDialogueService**: UI dialog management

#### Audio Services
- **IDimmerAudioService**: Platform-specific audio playback (Windows/Android)
- **IDimmerAudioEditorService**: Audio file editing capabilities

#### Library Management
- **ILibraryScannerService**: Music library scanning and indexing
- **IFolderMonitorService**: File system monitoring for changes
- **ICoverArtService**: Album artwork management
- **IDuplicateFinderService**: Duplicate track detection

#### Metadata Services
- **MusicMetadataService**: Audio file metadata extraction
- **MusicArtistryService**: Artist information management
- **MusicRelationshipService**: Song/album/artist relationship management

#### Lyrics Services
- **ILyricsMetadataService**: Lyrics extraction from files
- **IOnlineLyricsProvider**: Online lyrics fetching (LrcLib)
- **ILyricsPersistenceService**: Lyrics storage and caching

#### Social & Cloud Services
- **IAuthenticationService**: User authentication via Parse
- **ILiveSessionManagerService**: Real-time session management
- **IFriendshipService**: Friend connections
- **IChatService**: In-app messaging

#### Statistics & Analytics
- **StatisticsService**: Listening statistics and analytics
- **AchievementService**: User achievements and milestones

## Key Features & Implementations

### TQL (Text Query Language)

Dimmer includes a powerful custom query language for music searching:

- **Location**: `DimmerSearch/` folder
- **Parser**: `SemanticParser` converts text queries to structured queries
- **Features**:
  - Field-specific searches (artist:, album:, title:, year:, etc.)
  - Operators (>, <, ranges, fuzzy matching ~, exact phrases "")
  - Boolean logic (AND, OR, NOT, exclude)
  - Sorting directives (asc, desc)
  - Result limiting (first, last, random)

**Example Queries**:
```
artist:Drake year:>2015 exclude album:"Views"
genre:Rock AND year:1990-1999 rating:>4 desc
```

See `docs/TQL_DOCUMENTATION.md` for complete TQL guide.

### DimmerLive (Real-time Collaboration)

Real-time features powered by Parse LiveQuery:

- **Device Sessions**: Track active listening sessions across devices
- **Live Sharing**: Share currently playing tracks with friends
- **Social Features**: Friend lists, chat, shared playlists
- **Sync State**: Synchronize playback state across devices

### Music Graph & Relationships

The application builds a relationship graph between songs:

- **Similarity Matching**: Find similar songs based on metadata
- **Artist Networks**: Discover related artists
- **Genre Clustering**: Group songs by genre relationships
- **Listening Patterns**: Analyze sequential listening patterns

### Statistics & Achievements

Comprehensive listening analytics:

- **Play Counts**: Track song, album, and artist play counts
- **Time-based Stats**: Daily, weekly, monthly listening reports
- **Achievement System**: Unlock achievements based on listening habits
- **Favorite Tracking**: Mark and manage favorite tracks

## Platform-Specific Implementations

### Windows (Dimmer.WinUI)

- **Audio Backend**: AudioSwitcher + Windows CoreAudio API
- **File System**: Full directory access for music libraries
- **Window Management**: Multi-window support (main player, mini player)
- **Animations**: Windows-specific animation service

### Android (Dimmer.Droid)

- **Audio Backend**: Android MediaPlayer / AudioTrack
- **Permissions**: Storage and notification permissions
- **Background Playback**: Foreground service for music playback
- **Notifications**: MediaSession integration for lock screen controls

## Data Flow

### Music Library Scanning

1. User selects music folder(s) via `IFolderMgtService`
2. `ILibraryScannerService` recursively scans for audio files
3. `MusicMetadataService` extracts metadata using ATL library
4. Songs stored in Realm database via `IRepository<Song>`
5. Album art extracted by `ICoverArtService`
6. `IFolderMonitorService` monitors folder for changes

### Playback Flow

1. User selects song(s) to play
2. Songs added to `IQueueManager<Song>`
3. `IDimmerAudioService` loads and plays audio file
4. Playback events broadcast via reactive streams
5. UI updates via ViewModel bindings
6. Statistics updated via `StatisticsService`
7. Optional: Scrobble to Last.fm via `ILastfmService`

### Search Flow (TQL)

1. User enters query in search box
2. `SemanticParser` parses query into `SemanticQuery` object
3. Query converted to predicates and filters
4. `DynamicData` applies reactive filtering/sorting
5. Results displayed in UI with live updates

## Configuration

Application configuration is loaded from `appsettings.json`:

```json
{
  "Lastfm": {
    "ApiKey": "...",
    "ApiSecret": "..."
  },
  "YBParse": {
    "ApplicationId": "...",
    "ServerUri": "...",
    "DotNetKEY": "..."
  }
}
```

## Error Handling & Logging

- **Crash Logs**: Stored in `Documents/DimmerCrashLogs/`
- **Exception Filtering**: `ExceptionFilterPolicy` filters noisy exceptions
- **First Chance Exceptions**: Logged for debugging
- **UI Error Presentation**: `IUiErrorPresenter` shows errors to users

## Testing

- **Unit Tests**: `Dimmer.Tests/` - Core logic tests
- **TQL Tests**: `DimmerTQLUnitTest/` - Query language validation
- **Integration Tests**: Platform-specific behavior validation

## Build Requirements

- **Visual Studio 2022** with MAUI workload
- **.NET 9.0 SDK**
- **Windows 10+** for Windows development
- **Android SDK API 30+** for Android development

## Further Reading

- [Developer Guide](DEVELOPER_GUIDE.md) - Setup and development workflow
- [TQL Documentation](TQL_DOCUMENTATION.md) - Complete query language reference
- [Project Diary](PROJECT_DIARY.md) - Development decisions and evolution
- [Contributing Guide](CONTRIBUTING_GUIDE.md) - How to contribute to the project
