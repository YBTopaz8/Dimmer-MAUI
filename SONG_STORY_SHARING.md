# Song Story Sharing Feature

## Overview
The Song Story Sharing feature allows users to share songs as beautiful story cards, similar to Spotify's and Apple Music's story features. Users can create shareable cards with album artwork, song information, and optionally selected lyrics lines.

## Features

### Card Design
- **Dynamic Background**: Uses the dominant color extracted from the album artwork
- **Fallback Color**: Uses dark slate blue (#483D8B) when no album artwork is available
- **Adaptive Layout**: 
  - Large album artwork when no lyrics are selected
  - Smaller album artwork with prominent lyrics display when lyrics are included
- **Branding**: Includes "Played on Dimmer" tag at the bottom
- **Smart Text Colors**: Automatically calculates contrasting text colors for optimal readability

### Lyrics Selection
- **Multi-line Selection**: Users can select up to 5 lines of lyrics to feature on the card
- **Smart Parsing**: Automatically removes timestamps from synced lyrics
- **Fallback Support**: Works with both synced and unsynced lyrics
- **Interactive UI**: 
  - Android: Custom RecyclerView-based dialog with checkboxes
  - Windows: ContentDialog with checkbox list

### Platform Implementation

#### Android
- **Location**: Share button in Now Playing screen
- **Rendering**: Uses SkiaSharp for high-quality card generation
- **Sharing**: Native Android share sheet via Intent.ACTION_SEND
- **File Provider**: Secure file sharing using AndroidX FileProvider
- **Dialog**: Material Design 3 styled lyrics selection dialog

#### Windows
- **Rendering**: Uses SkiaSharp for card generation
- **Sharing**: Windows DataTransferManager for native share experience
- **Dialog**: WinUI 3 ContentDialog for lyrics selection
- **Output**: Saves to temporary directory

## Technical Architecture

### Core Components

1. **ISongStoryService** (`Dimmer/Dimmer/Interfaces/ISongStoryService.cs`)
   - Platform-agnostic service for preparing story data
   - Color extraction from album artwork
   - Contrast calculation for text colors

2. **ISongStoryShareService** (`Dimmer/Dimmer/Interfaces/ISongStoryShareService.cs`)
   - Platform-specific interface for card generation and sharing
   - Lyrics selection UI

3. **SongStoryData** (`Dimmer/Dimmer/Data/ModelView/SongStoryData.cs`)
   - Model containing all necessary information for card generation
   - Song metadata, selected lyrics, colors

### Platform-Specific Implementations

#### Android
- **AndroidSongStoryCardRenderer** (`Dimmer.Droid/NativeServices/AndroidSongStoryCardRenderer.cs`)
  - Generates 1080x1920 PNG cards using SkiaSharp
  - Handles text wrapping and layout

- **AndroidSongStoryShareService** (`Dimmer.Droid/NativeServices/AndroidSongStoryShareService.cs`)
  - Implements sharing via Android Intent
  - Manages file provider URIs

- **LyricsSelectionDialogFragment** (`Dimmer.Droid/ViewsAndPages/NativeViews/LyricsSelectionDialogFragment.cs`)
  - Material Design 3 dialog
  - RecyclerView with custom adapter
  - Max 5 selection enforcement

#### Windows
- **WindowsSongStoryShareService** (`Dimmer.WinUI/NativeServices/WindowsSongStoryShareService.cs`)
  - Generates cards using SkiaSharp
  - Integrates with Windows sharing via DataTransferManager

- **LyricsSelectionDialog** (`Dimmer.WinUI/Views/CustomViews/WinuiViews/LyricsSelectionDialog.xaml`)
  - WinUI 3 ContentDialog
  - ItemsRepeater with CheckBox items
  - Max 5 selection enforcement

## Usage

### For Users

#### Android
1. Play a song
2. Tap the "Share" button in the Now Playing screen
3. If the song has lyrics, a dialog will appear to select up to 5 lines
4. The app generates a beautiful story card
5. Choose where to share the card using Android's native share sheet

#### Windows
1. Play a song
2. Click the share button (to be added to UI)
3. If the song has lyrics, a dialog will appear to select up to 5 lines
4. The app generates the card
5. Use Windows' share menu to share the card

### For Developers

#### Adding the Share Button to a New Location

**Android:**
```csharp
private async Task ShareCurrentSong()
{
    var storyService = MainApplication.ServiceProvider?.GetService<ISongStoryService>();
    var shareService = MainApplication.ServiceProvider?.GetService<ISongStoryShareService>();
    
    var currentSong = /* get current song */;
    var storyData = await storyService.PrepareSongStoryAsync(currentSong);
    var cardPath = await shareService.GenerateStoryCardAsync(storyData);
    await shareService.ShareStoryAsync(cardPath, "Shared from Dimmer");
}
```

**Windows:**
```csharp
private async Task ShareCurrentSong()
{
    var storyService = IPlatformApplication.Current.Services.GetService<ISongStoryService>();
    var shareService = IPlatformApplication.Current.Services.GetService<ISongStoryShareService>();
    
    var currentSong = /* get current song */;
    var storyData = await storyService.PrepareSongStoryAsync(currentSong);
    var cardPath = await shareService.GenerateStoryCardAsync(storyData);
    await shareService.ShareStoryAsync(cardPath, "Shared from Dimmer");
}
```

## Color Extraction Algorithm

The color extraction uses a sampling-based approach:
1. Sample pixels from the album artwork (every nth pixel for performance)
2. Filter out very light, very dark, or transparent pixels
3. Quantize colors to reduce variations
4. Select the most common color as the dominant color
5. Calculate relative luminance using WCAG formula
6. Determine contrasting text color (black or white)

## File Specifications

### Generated Card
- **Format**: PNG
- **Dimensions**: 1080x1920 pixels (9:16 aspect ratio, optimized for mobile sharing)
- **Quality**: 100% PNG encoding
- **Location**: 
  - Android: App cache directory
  - Windows: System temp directory

## Dependencies

- **SkiaSharp**: Cross-platform 2D graphics library for card rendering
- **AndroidX.Palette** (Android only): Color extraction library (not currently used, as SkiaSharp implementation is preferred)
- **AndroidX.Core.Content.FileProvider** (Android only): Secure file sharing
- **Windows.ApplicationModel.DataTransfer** (Windows only): Native Windows sharing

## Future Enhancements

- [ ] Add share button to more locations (e.g., song detail pages, playlists)
- [ ] Custom card templates
- [ ] Animated story cards (video)
- [ ] Instagram/Facebook stories direct export
- [ ] QR code generation for song sharing
- [ ] Share history

## Known Limitations

- Maximum 5 lyrics lines can be selected (by design for optimal card appearance)
- Color extraction is sampling-based and may not always capture the "perfect" dominant color
- Windows sharing requires the app to be in the foreground
- Generated cards are temporary and are not automatically saved to user's gallery

## Troubleshooting

### Android
- **Issue**: "File provider not found" error
  - **Solution**: Ensure the FileProvider is properly declared in AndroidManifest.xml and file_paths.xml exists

- **Issue**: Share sheet doesn't appear
  - **Solution**: Check that the file path is valid and the app has proper permissions

### Windows
- **Issue**: Share UI doesn't appear
  - **Solution**: Ensure the app is running in the foreground and DataTransferManager is properly initialized

## Performance Considerations

- Card generation typically takes 500ms-2s depending on device performance
- Color extraction uses pixel sampling to reduce processing time
- Cards are generated on background threads to avoid UI blocking
- Generated cards are cached temporarily and cleaned up by the OS
