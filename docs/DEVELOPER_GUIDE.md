# Dimmer-MAUI Developer Guide

## Getting Started

This guide will help you set up your development environment and understand the development workflow for Dimmer-MAUI.

## Prerequisites

### Required Software

1. **Visual Studio 2022** (Version 17.8 or later)
   - Workloads required:
     - `.NET Multi-platform App UI development`
     - `Universal Windows Platform development` (for Windows)
     - `Mobile development with .NET` (for Android)

2. **.NET 9.0 SDK**
   - Download from [dotnet.microsoft.com](https://dotnet.microsoft.com/download)
   - Verify installation: `dotnet --version`

3. **Git**
   - Download from [git-scm.com](https://git-scm.com/)

### Optional but Recommended

- **Android Device or Emulator** (API 30+) for Android testing
- **Windows 10/11** for Windows platform development
- **Visual Studio Code** with C# extension (for quick edits)

## Repository Setup

### 1. Clone the Repository

```bash
git clone https://github.com/YBTopaz8/Dimmer-MAUI.git
cd Dimmer-MAUI
```

### 2. Initialize Submodules

The project includes external dependencies as submodules:

```bash
git submodule update --init --recursive
```

### 3. Restore Dependencies

```bash
dotnet restore Dimmer-MAUI.sln
```

### 4. Configure External Dependencies

The project depends on external projects located outside the repository:

- **Last.fm SDK**: `../../Last.fm/src/Hqub.Lastfm/Hqub.Lastfm.csproj`
- **Parse LiveQuery**: `../../Parse-LiveQueries-DOTNET/ParseLiveQuery/ParseLiveQuery.csproj`

You have two options:

#### Option A: Clone Required Repositories (Recommended)

```bash
# Navigate to the parent directory
cd ..

# Clone Last.fm SDK
git clone https://github.com/harriyott/Hqub.Lastfm Last.fm

# Clone Parse LiveQuery
git clone https://github.com/YBTopaz8/Parse-LiveQueries-DOTNET

# Return to Dimmer-MAUI
cd Dimmer-MAUI
```

#### Option B: Update Project References

Edit `Dimmer/Dimmer/Dimmer.csproj` to point to your local copies of these projects or use NuGet packages if available.

### 5. Configure API Keys

Create `appsettings.json` in `Dimmer/Dimmer/` with your API credentials:

```json
{
  "Lastfm": {
    "ApiKey": "YOUR_LASTFM_API_KEY",
    "ApiSecret": "YOUR_LASTFM_SECRET"
  },
  "YBParse": {
    "ApplicationId": "YOUR_PARSE_APP_ID",
    "ServerUri": "YOUR_PARSE_SERVER_URI",
    "DotNetKEY": "YOUR_PARSE_DOTNET_KEY"
  }
}
```

**Note**: Ensure `appsettings.json` has Build Action set to "Embedded resource" in project properties.

## Building the Project

### Build All Projects

```bash
dotnet build Dimmer-MAUI.sln -c Debug
```

### Build for Windows

```bash
dotnet build Dimmer/Dimmer.WinUI/Dimmer.WinUI.csproj -c Debug
```

### Build for Android

```bash
dotnet build Dimmer/Dimmer.Droid/Dimmer.Droid.csproj -c Debug
```

## Running the Application

### Run from Visual Studio

1. Open `Dimmer-MAUI.sln` in Visual Studio 2022
2. Set the startup project:
   - **Dimmer.WinUI** for Windows
   - **Dimmer.Droid** for Android
3. Select target device/emulator from dropdown
4. Press **F5** to build and run with debugging

### Run from Command Line

#### Windows
```bash
dotnet run --project Dimmer/Dimmer.WinUI/Dimmer.WinUI.csproj
```

#### Android (requires device/emulator)
```bash
dotnet build Dimmer/Dimmer.Droid/Dimmer.Droid.csproj -t:Run
```

## Testing

### Run All Tests

```bash
dotnet test Dimmer-MAUI.sln
```

### Run Specific Test Project

```bash
# Unit tests
dotnet test Dimmer.Tests/Dimmer.Tests.csproj

# TQL tests
dotnet test DimmerTQLUnitTest/DimmerTQLUnitTest.csproj
```

### Run Tests with Coverage

```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Development Workflow

### 1. Create a Feature Branch

```bash
git checkout -b feature/your-feature-name
```

### 2. Make Changes

Follow the coding standards outlined in the [Contributing Guide](CONTRIBUTING_GUIDE.md).

### 3. Test Your Changes

- Run relevant unit tests
- Test on both Windows and Android if changes affect shared code
- Test edge cases and error scenarios

### 4. Commit Your Changes

```bash
git add .
git commit -m "Add: Brief description of changes"
```

Use conventional commit prefixes:
- `Add:` for new features
- `Fix:` for bug fixes
- `Update:` for updates to existing features
- `Refactor:` for code refactoring
- `Docs:` for documentation changes
- `Test:` for test additions/changes

### 5. Push and Create Pull Request

```bash
git push origin feature/your-feature-name
```

Then create a Pull Request on GitHub.

## Project Structure Overview

### Shared Code (`Dimmer/Dimmer/`)

Contains cross-platform business logic and UI:

- **Data/**: Data models and ViewModels
- **Interfaces/**: Service contracts
- **ViewModel/**: MVVM ViewModels
- **Utilities/**: Helper classes and extensions
- **Resources/**: Images, fonts, styles, localization

### Platform-Specific Code

- **Dimmer.WinUI/**: Windows-specific implementations
- **Dimmer.Droid/**: Android-specific implementations

Each platform project:
- Implements platform-specific services
- Contains platform-specific UI customizations
- Handles platform-specific lifecycle events

## Common Development Tasks

### Adding a New Service

1. **Define Interface** in `Dimmer/Dimmer/Interfaces/`:
   ```csharp
   public interface IMyService
   {
       Task<Result> DoSomethingAsync();
   }
   ```

2. **Implement Service** in `Dimmer/Dimmer/Interfaces/Services/`:
   ```csharp
   public class MyService : IMyService
   {
       public async Task<Result> DoSomethingAsync()
       {
           // Implementation
       }
   }
   ```

3. **Register Service** in `ServiceRegistration.cs`:
   ```csharp
   services.AddSingleton<IMyService, MyService>();
   ```

4. **Inject in ViewModel**:
   ```csharp
   public class MyViewModel(IMyService myService)
   {
       private readonly IMyService _myService = myService;
   }
   ```

### Adding a New ViewModel

1. **Create ViewModel** in `Dimmer/Dimmer/ViewModel/`:
   ```csharp
   public partial class MyViewModel : ObservableObject
   {
       [ObservableProperty]
       private string _myProperty;
       
       [RelayCommand]
       private async Task MyCommandAsync()
       {
           // Command implementation
       }
   }
   ```

2. **Register ViewModel** in `ServiceRegistration.cs` or platform-specific `MauiProgram.cs`:
   ```csharp
   services.AddSingleton<MyViewModel>();
   ```

3. **Create View** (XAML):
   ```xml
   <ContentPage x:DataType="vm:MyViewModel">
       <Label Text="{Binding MyProperty}" />
       <Button Command="{Binding MyCommandCommand}" Text="Click Me" />
   </ContentPage>
   ```

### Adding a New Model

1. **Create Model** in `Dimmer/Dimmer/Data/Models/`:
   ```csharp
   public class MyModel : RealmObject
   {
       [PrimaryKey]
       public string Id { get; set; } = Guid.NewGuid().ToString();
       
       public string Name { get; set; }
       public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
   }
   ```

2. **Use Repository** to interact with data:
   ```csharp
   public class MyService(IRepository<MyModel> repository)
   {
       public async Task<MyModel> GetByIdAsync(string id)
       {
           return await repository.GetByIdAsync(id);
       }
   }
   ```

### Working with TQL (Text Query Language)

The custom query language is located in `DimmerSearch/`:

1. **Parser**: `SemanticParser.cs` - Converts text to structured queries
2. **Query Model**: `SemanticQuery.cs` - Represents parsed query
3. **Actions**: `TQLActions/` - Query execution logic
4. **Documentation**: `TQLDoc/` - In-app help system

To extend TQL:

1. Add new operators/keywords to the parser
2. Update query execution in TQL Actions
3. Add documentation to `TqlDocumentation.cs`
4. Add tests in `DimmerTQLUnitTest/`

### Platform-Specific Code

Use conditional compilation or platform-specific implementations:

```csharp
#if WINDOWS
    // Windows-specific code
#elif ANDROID
    // Android-specific code
#endif
```

Or use dependency injection with platform-specific service implementations.

## Debugging Tips

### Enable Verbose Logging

In `App.xaml.cs`, set debug flags:

```csharp
#if DEBUG
    builder.Logging.AddDebug();
    builder.Logging.SetMinimumLevel(LogLevel.Trace);
#endif
```

### View Crash Logs

Crash logs are saved to:
- Windows: `Documents\DimmerCrashLogs\`
- Android: `Android/data/com.ybtopaz.dimmer/files/DimmerCrashLogs/`

### Debugging on Android

1. Enable USB Debugging on your Android device
2. Connect device via USB
3. Run from Visual Studio with device selected
4. View logs in Visual Studio Output window or use `adb logcat`

### Debugging Realm Database

Use Realm Studio to inspect the database:
1. Download [Realm Studio](https://www.mongodb.com/products/realm-studio)
2. Locate database file:
   - Windows: `%LOCALAPPDATA%\Packages\<PackageName>\LocalState\`
   - Android: Use `adb pull` to extract database

## Performance Profiling

### Visual Studio Profiler

1. Debug > Performance Profiler
2. Select profiling targets (CPU, Memory, etc.)
3. Start profiling session
4. Analyze results

### Memory Leak Detection

Use weak event handlers and dispose patterns:

```csharp
public void Subscribe()
{
    WeakEventManager<SourceType, EventArgs>.AddHandler(
        source, nameof(source.Event), Handler);
}

public void Dispose()
{
    WeakEventManager<SourceType, EventArgs>.RemoveHandler(
        source, nameof(source.Event), Handler);
}
```

## Troubleshooting

### Build Errors

**"Project not found"**: Ensure external dependencies are cloned in the correct location.

**"Workload not installed"**: Run `dotnet workload install maui` in terminal.

**"Unable to find package"**: Run `dotnet restore` again.

### Runtime Errors

**"appsettings.json not found"**: Ensure Build Action is set to "Embedded resource".

**"Realm schema mismatch"**: Delete and regenerate database or increment schema version.

**"Parse connection failed"**: Check Parse server credentials in `appsettings.json`.

### Android-Specific Issues

**"App crashes on startup"**: Check Android permissions in `AndroidManifest.xml`.

**"Unable to deploy"**: Ensure USB debugging is enabled and device is authorized.

## Resources

- [.NET MAUI Documentation](https://learn.microsoft.com/dotnet/maui/)
- [Realm .NET Documentation](https://www.mongodb.com/docs/realm/sdk/dotnet/)
- [ReactiveUI Documentation](https://reactiveui.net/)
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/)

## Getting Help

- **Issues**: Check [GitHub Issues](https://github.com/YBTopaz8/Dimmer-MAUI/issues)
- **Discussions**: Use [GitHub Discussions](https://github.com/YBTopaz8/Dimmer-MAUI/discussions)
- **Wiki**: See the [project wiki](https://github.com/YBTopaz8/Dimmer-MAUI/wiki)

## Next Steps

- Read the [Architecture Documentation](ARCHITECTURE.md) to understand the system design
- Review the [Contributing Guide](CONTRIBUTING_GUIDE.md) before submitting changes
- Explore the [TQL Documentation](TQL_DOCUMENTATION.md) to understand the query language
- Check the [Project Diary](PROJECT_DIARY.md) for historical context on design decisions
