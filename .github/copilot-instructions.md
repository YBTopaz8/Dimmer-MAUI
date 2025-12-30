# Copilot Instructions for Dimmer-MAUI

## Project Overview

Dimmer is a cross-platform music player application built with .NET MAUI, targeting Windows and Android platforms. The app provides music playback, lyrics display, library management, social features, and achievements.

## Technology Stack

- **Framework**: .NET 9.0 with .NET MAUI
- **UI**: MAUI with CommunityToolkit.Maui
- **Architecture**: MVVM pattern using CommunityToolkit.Mvvm
- **Database**: Realm for local data storage
- **Reactive Programming**: System.Reactive (Rx.NET) with DynamicData
- **Testing**: xUnit with Moq for unit tests
- **Audio Processing**: ATL (Audio Tools Library), Xabe.FFmpeg
- **External Services**: Last.fm integration for music metadata
- **Platform-Specific**: AudioSwitcher for Windows, platform-specific projects for WinUI and Android

## Project Structure

- **Dimmer/Dimmer/** - Core shared library with business logic, ViewModels, and services
- **Dimmer/Dimmer.WinUI/** - Windows-specific implementation
- **Dimmer/Dimmer.Droid/** - Android-specific implementation
- **Dimmer.Tests/** - Unit tests
- **atldotnet-main/** - Audio Tools Library (embedded)
- **AudioSwitcher-master/** - Audio switching library for Windows (embedded)

## Key Architecture Patterns

### Dependency Injection
- Services are registered in `ServiceRegistration.cs`
- Use constructor injection for all dependencies
- Register singletons for stateful services, transient for lightweight or per-request services
- Follow the existing registration pattern in `AddDimmerCoreServices` extension method

### MVVM Pattern
- ViewModels inherit from `ObservableObject` (CommunityToolkit.Mvvm)
- Use `[ObservableProperty]` attribute with partial properties for bindable properties
- Use `[RelayCommand]` attribute for command methods
- Place ViewModels in `Dimmer/Dimmer/ViewModel/` directory
- Name ViewModels with "ViewModel" or "VM" suffix (e.g., `AchievementsViewModel`)

### Service Layer
- Define interfaces in `Dimmer/Dimmer/Interfaces/`
- Implement services in appropriate subdirectories
- Common service patterns:
  - State management services (e.g., `IDimmerStateService`)
  - Data access services using `IRepository<T>`
  - Business logic services (e.g., `AchievementService`, `MusicMetadataService`)

### Repository Pattern
- Generic `IRepository<T>` interface for data access
- Realm-based implementation in `RealmCoreRepo<T>`
- Use `IRealmFactory` for obtaining Realm instances

### Error Handling
- Use `IErrorHandler` service for centralized error handling
- Log errors appropriately with meaningful context
- Present user-facing errors through `IUiErrorPresenter`

## Coding Conventions

### C# Style
- Use modern C# features including file-scoped namespaces, primary constructors, pattern matching, and collection expressions
- Enable nullable reference types (already configured)
- Use `var` for local variables when the type is obvious
- Follow standard C# naming conventions:
  - PascalCase for public members, types, and namespaces
  - _camelCase with underscore prefix for private fields
  - camelCase for parameters and local variables

### Async/Await
- Use async/await for I/O-bound operations
- Suffix async methods with "Async" (e.g., `LoadDataAsync`)
- Always use `ConfigureAwait(false)` in library code unless UI context is required
- For MAUI UI operations, use `MainThread.BeginInvokeOnMainThread` when needed

### Reactive Extensions (Rx.NET)
- The codebase uses reactive programming patterns with System.Reactive
- Use `IObservable<T>` for event streams and data changes
- Dispose of subscriptions properly using `IDisposable` and `CompositeDisposable`
- DynamicData is used for observable collections

### Platform-Specific Code
- Use conditional compilation directives for platform-specific code:
  - `#if WINDOWS` for Windows-specific code
  - `#if ANDROID` for Android-specific code
- Place platform-specific implementations in respective project directories
- Use dependency injection to provide platform-specific service implementations

## Building and Testing

### Build Commands
```bash
# Restore dependencies
dotnet restore

# Build the entire solution
dotnet build Dimmer-MAUI.sln

# Build Windows project
dotnet build Dimmer/Dimmer.WinUI/Dimmer.WinUI.csproj -f net9.0-windows10.0.19041.0

# Build Android project
dotnet build Dimmer/Dimmer.Droid/Dimmer.Droid.csproj -f net9.0-android
```

### Testing
```bash
# Run all tests
dotnet test

# Run tests with detailed output
dotnet test --logger "console;verbosity=detailed"
```

### Important Notes for CI/CD
- External dependencies (Last.fm, Parse-LiveQueries-DOTNET, atldotnet, AudioSwitcher) are checked out separately in CI
- `appsettings.json` is generated during CI build from secrets
- The workflow targets the `master` branch for releases and `IntegrateAll` for pre-releases

## Common Development Tasks

### Adding a New ViewModel
1. Create interface if needed in `Dimmer/Dimmer/Interfaces/`
2. Create ViewModel class in `Dimmer/Dimmer/ViewModel/`
3. Inherit from `ObservableObject`
4. Use `[ObservableProperty]` and `[RelayCommand]` attributes
5. Register in `ServiceRegistration.cs`
6. Implement `IDisposable` if using subscriptions or resources

### Adding a New Service
1. Define interface in `Dimmer/Dimmer/Interfaces/`
2. Implement in appropriate directory
3. Register in `ServiceRegistration.AddDimmerCoreServices()`
4. Inject via constructor where needed
5. Consider lifecycle (Singleton vs Transient)

### Working with Realm Database
- Use `IRealmFactory.GetRealmInstance()` to obtain Realm instances
- Define models with `RealmObject` inheritance
- Use transactions for write operations: `realm.Write(() => { ... })`
- Dispose of Realm instances when done

### Working with UI
- XAML files are co-located with code-behind in respective platform projects
- Use MVVM data binding to connect Views to ViewModels
- Leverage CommunityToolkit.Maui for enhanced UI components
- Use Material Design icons (MaterialIcons fonts are configured)

## Security Considerations

- Never commit `appsettings.json` (it's in .gitignore)
- Use `PasswordEncryptionService` for sensitive data
- API keys and secrets should be stored in GitHub Secrets for CI/CD
- Validate user input before processing

## External Dependencies

The project references several external projects that are not in the main repository:
- **Last.fm** (Hqub.Lastfm) - Music metadata and scrobbling
- **Parse-LiveQueries-DOTNET** - Real-time data synchronization
- **atldotnet** - Audio file metadata reading/writing
- **AudioSwitcher** - Windows audio device management

These are cloned separately during CI builds and path references are adjusted.

## Useful Resources

- [.NET MAUI Documentation](https://learn.microsoft.com/en-us/dotnet/maui/)
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)
- [Realm .NET Documentation](https://www.mongodb.com/docs/realm/sdk/dotnet/)
- [Reactive Extensions](http://reactivex.io/)
- [Project Wiki](https://github.com/YBTopaz8/Dimmer-MAUI/wiki)

## Additional Notes

- Target platforms: Windows 10+ (19041) and Android 10+ (API 30)
- iOS/macOS support has been discontinued
- The app includes social features and achievements
- Lyrics are supported via .lrc files and metadata
- The project uses Fody for IL weaving (configured in FodyWeavers.xml)
