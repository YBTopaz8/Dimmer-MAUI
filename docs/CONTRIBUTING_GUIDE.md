# Contributing to Dimmer-MAUI

Thank you for your interest in contributing to Dimmer-MAUI! This document provides guidelines and best practices for contributing to the project.

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [How to Contribute](#how-to-contribute)
- [Coding Standards](#coding-standards)
- [Pull Request Process](#pull-request-process)
- [Testing Guidelines](#testing-guidelines)
- [Documentation](#documentation)
- [Community](#community)

## Code of Conduct

### Our Standards

- **Be respectful**: Treat everyone with respect and consideration
- **Be collaborative**: Work together and help each other
- **Be constructive**: Provide helpful feedback and suggestions
- **Be patient**: Remember that people have different skill levels and backgrounds

### Unacceptable Behavior

- Harassment, discrimination, or personal attacks
- Trolling, insulting, or derogatory comments
- Publishing others' private information without permission
- Other conduct that would be inappropriate in a professional setting

## Getting Started

1. **Fork the repository** on GitHub
2. **Clone your fork** locally:
   ```bash
   git clone https://github.com/YOUR-USERNAME/Dimmer-MAUI.git
   ```
3. **Set up your development environment** following the [Developer Guide](DEVELOPER_GUIDE.md)
4. **Create a new branch** for your contribution:
   ```bash
   git checkout -b feature/your-feature-name
   ```

## How to Contribute

### Reporting Bugs

Before creating a bug report, please:

1. **Check existing issues** to avoid duplicates
2. **Use the bug report template** when creating a new issue
3. **Provide detailed information**:
   - Steps to reproduce
   - Expected behavior vs actual behavior
   - Screenshots or videos if applicable
   - Platform (Windows/Android) and version
   - Error messages or crash logs

### Suggesting Features

Feature suggestions are welcome! Please:

1. **Check if the feature has been suggested** before
2. **Use the feature request template**
3. **Explain the problem** your feature would solve
4. **Describe the proposed solution** in detail
5. **Consider alternatives** you've thought about

### Fixing Bugs

1. Find a bug in the [Issues](https://github.com/YBTopaz8/Dimmer-MAUI/issues) section
2. Comment on the issue to let others know you're working on it
3. Follow the [Pull Request Process](#pull-request-process)

### Adding Features

1. Discuss your idea in [Discussions](https://github.com/YBTopaz8/Dimmer-MAUI/discussions) or create a feature request issue
2. Wait for feedback from maintainers
3. Once approved, follow the [Pull Request Process](#pull-request-process)

## Coding Standards

### C# Style Guide

#### Naming Conventions

```csharp
// PascalCase for classes, interfaces, methods, properties
public class MusicPlayer { }
public interface IAudioService { }
public void PlaySong() { }
public string SongTitle { get; set; }

// camelCase for local variables and parameters
string songTitle = "Example";
void ProcessSong(string fileName) { }

// _camelCase for private fields
private readonly IAudioService _audioService;

// UPPERCASE for constants
private const int MAX_RETRIES = 3;
```

#### Code Organization

```csharp
// Order: fields, constructors, properties, methods
public class MyService
{
    // 1. Fields (private)
    private readonly IRepository _repository;
    private const int DEFAULT_TIMEOUT = 30;
    
    // 2. Constructor(s)
    public MyService(IRepository repository)
    {
        _repository = repository;
    }
    
    // 3. Properties (public, then internal, then private)
    public string Name { get; set; }
    
    // 4. Methods (public, then internal, then private)
    public async Task<Result> ProcessAsync()
    {
        return await InternalProcessAsync();
    }
    
    private async Task<Result> InternalProcessAsync()
    {
        // Implementation
    }
}
```

#### MVVM Pattern

Use `CommunityToolkit.Mvvm` attributes:

```csharp
public partial class MyViewModel : ObservableObject
{
    // Use [ObservableProperty] for properties
    [ObservableProperty]
    private string _songTitle;
    
    // Use [RelayCommand] for commands
    [RelayCommand]
    private async Task PlaySongAsync()
    {
        // Command logic
    }
    
    // Or with CanExecute
    [RelayCommand(CanExecute = nameof(CanPlay))]
    private async Task PlaySongAsync()
    {
        // Command logic
    }
    
    private bool CanPlay() => !string.IsNullOrEmpty(SongTitle);
}
```

#### Async/Await

```csharp
// Always use Async suffix for async methods
public async Task<Song> GetSongAsync(string id)
{
    // Use ConfigureAwait(false) in library code
    var song = await _repository.GetByIdAsync(id).ConfigureAwait(false);
    return song;
}

// Don't use async void (except for event handlers)
private async void OnButtonClicked(object sender, EventArgs e)
{
    try
    {
        await ProcessAsync();
    }
    catch (Exception ex)
    {
        // Handle exception
    }
}
```

#### Null Handling

```csharp
// Use nullable reference types
public string? GetSongTitle(Song? song)
{
    // Use null-conditional operators
    return song?.Title;
}

// Use null-coalescing operators
public string GetDisplayName() => Title ?? "Unknown";

// Use pattern matching
if (song is { Title: not null, Artist: not null })
{
    // Song has both title and artist
}
```

#### Error Handling

```csharp
// Use specific exceptions
if (string.IsNullOrEmpty(songId))
{
    throw new ArgumentException("Song ID cannot be null or empty", nameof(songId));
}

// Catch specific exceptions
try
{
    await ProcessAsync();
}
catch (FileNotFoundException ex)
{
    // Handle missing file
    _logger.LogError(ex, "File not found: {FileName}", ex.FileName);
}
catch (Exception ex)
{
    // Handle unexpected errors
    _logger.LogError(ex, "Unexpected error in ProcessAsync");
    throw; // Re-throw if you can't handle it
}
```

### XAML Style Guide

```xml
<!-- Use x:DataType for compiled bindings -->
<ContentPage x:DataType="vm:MyViewModel">
    
    <!-- Use property element syntax for complex properties -->
    <Label>
        <Label.FormattedText>
            <FormattedString>
                <Span Text="Bold" FontAttributes="Bold" />
            </FormattedString>
        </Label.FormattedText>
    </Label>
    
    <!-- Use clear, descriptive x:Name values -->
    <Button x:Name="PlayButton" 
            Text="Play"
            Command="{Binding PlayCommand}" />
    
    <!-- Group related properties -->
    <Grid RowDefinitions="Auto,*"
          ColumnDefinitions="*,Auto"
          Padding="20"
          RowSpacing="10"
          ColumnSpacing="10">
        <!-- Content -->
    </Grid>
    
</ContentPage>
```

### Comments and Documentation

```csharp
// Use XML documentation for public APIs
/// <summary>
/// Plays the specified song asynchronously.
/// </summary>
/// <param name="song">The song to play.</param>
/// <param name="startPosition">Optional start position in seconds.</param>
/// <returns>A task representing the asynchronous operation.</returns>
/// <exception cref="ArgumentNullException">Thrown when song is null.</exception>
public async Task PlayAsync(Song song, double startPosition = 0)
{
    // Implementation
}

// Use single-line comments for implementation details
// Calculate the total duration in milliseconds
var totalMs = duration.TotalMilliseconds;

// Avoid obvious comments
// BAD: Increment counter by 1
counter++;

// GOOD: Reset retry counter after successful connection
counter = 0;
```

### File Organization

```
Dimmer/Dimmer/
├── Data/
│   ├── Models/          # Domain models
│   └── ModelView/       # ViewModels for UI binding
├── Interfaces/
│   └── Services/        # Service interfaces
├── ViewModel/           # MVVM ViewModels
├── Utilities/           # Helper classes
└── Resources/           # App resources
```

## Pull Request Process

### Before Submitting

1. **Ensure your code builds** without errors or warnings
2. **Run all tests** and ensure they pass
3. **Update documentation** if you changed public APIs
4. **Add tests** for new functionality
5. **Follow the coding standards** outlined above

### Commit Messages

Use conventional commit format:

```
<type>(<scope>): <subject>

<body>

<footer>
```

**Types**:
- `Add`: New feature
- `Fix`: Bug fix
- `Update`: Update to existing feature
- `Refactor`: Code refactoring
- `Docs`: Documentation changes
- `Test`: Adding or updating tests
- `Chore`: Maintenance tasks

**Examples**:

```
Add: TQL fuzzy search operator

Implement fuzzy string matching using Levenshtein distance
for typo-tolerant searches.

Closes #123
```

```
Fix: Crash when loading empty playlist

Added null check in PlaylistViewModel.LoadAsync to prevent
NullReferenceException when playlist has no songs.

Fixes #456
```

### Pull Request Template

When creating a PR, include:

1. **Description**: What does this PR do?
2. **Related Issue**: Link to related issue(s)
3. **Type of Change**: Bug fix, feature, refactoring, etc.
4. **Testing**: How was this tested?
5. **Screenshots**: If UI changes, include screenshots
6. **Checklist**:
   - [ ] Code builds without errors
   - [ ] All tests pass
   - [ ] Documentation updated
   - [ ] Follows coding standards

### Review Process

1. Maintainers will review your PR
2. Address any feedback or requested changes
3. Once approved, a maintainer will merge your PR

## Testing Guidelines

### Unit Tests

```csharp
[Test]
public async Task GetSongAsync_WithValidId_ReturnsSong()
{
    // Arrange
    var expectedSong = new Song { Id = "123", Title = "Test Song" };
    _mockRepository.Setup(r => r.GetByIdAsync("123"))
        .ReturnsAsync(expectedSong);
    var service = new MusicService(_mockRepository.Object);
    
    // Act
    var result = await service.GetSongAsync("123");
    
    // Assert
    Assert.That(result, Is.Not.Null);
    Assert.That(result.Id, Is.EqualTo("123"));
    Assert.That(result.Title, Is.EqualTo("Test Song"));
}

[Test]
public void GetSongAsync_WithNullId_ThrowsArgumentException()
{
    // Arrange
    var service = new MusicService(_mockRepository.Object);
    
    // Act & Assert
    Assert.ThrowsAsync<ArgumentException>(async () => 
        await service.GetSongAsync(null));
}
```

### Integration Tests

Test platform-specific functionality on actual devices/emulators.

### TQL Tests

For TQL features, add tests in `DimmerTQLUnitTest/`:

```csharp
[Test]
public void Parser_WithFuzzyOperator_ParsesCorrectly()
{
    // Arrange
    var parser = new SemanticParser();
    var query = "artist:~beatels";
    
    // Act
    var result = parser.Parse(query);
    
    // Assert
    Assert.That(result.Clauses, Has.Count.EqualTo(1));
    Assert.That(result.Clauses[0].FieldName, Is.EqualTo("Artist"));
    Assert.That(result.Clauses[0].IsFuzzy, Is.True);
}
```

## Documentation

### Code Documentation

- Add XML comments to public APIs
- Keep comments up-to-date with code changes
- Explain *why*, not *what* (code shows what)

### User Documentation

- Update README.md for user-facing changes
- Update docs/ folder for developer documentation
- Add examples for new features

### API Changes

If you change public APIs:

1. Update XML documentation
2. Update ARCHITECTURE.md if architecture changed
3. Add migration guide if breaking change

## Community

### Communication Channels

- **GitHub Issues**: Bug reports and feature requests
- **GitHub Discussions**: Questions, ideas, and general discussion
- **Pull Requests**: Code contributions and reviews

### Getting Help

- Read the [Developer Guide](DEVELOPER_GUIDE.md)
- Check existing issues and discussions
- Ask questions in GitHub Discussions

### Recognition

Contributors will be:
- Listed in release notes for their contributions
- Credited in the project README
- Mentioned in commit messages

## License

By contributing to Dimmer-MAUI, you agree that your contributions will be licensed under the same license as the project (see LICENSE file).

## Questions?

If you have questions about contributing, feel free to:
- Open a discussion on GitHub
- Comment on relevant issues
- Reach out to the maintainers

Thank you for contributing to Dimmer-MAUI! 🎵
