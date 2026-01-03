# Song Story Sharing Implementation Summary

## 📋 Issue Requirements
**Goal**: Implement native song story sharing similar to Spotify/Apple Music

**Key Requirements:**
- ✅ Card-based sharing with song information
- ✅ Dominant color from cover art (fallback to darkslateblue)
- ✅ Optional lyrics selection (max 5 lines)
- ✅ Adaptive layout: large cover without lyrics, small cover with lyrics
- ✅ "Played on Dimmer" branding
- ✅ Native implementation (no MAUI UI)

## 🏗️ Architecture Overview

```
┌─────────────────────────────────────────────────────────┐
│                 Shared Core Layer                        │
│  (Dimmer/Dimmer/)                                       │
├─────────────────────────────────────────────────────────┤
│  • ISongStoryService - Story data preparation           │
│  • ISongStoryShareService - Platform sharing interface  │
│  • SongStoryData - Data model                          │
│  • SongStoryService - Color extraction & preparation    │
└─────────────────────────────────────────────────────────┘
                          │
            ┌─────────────┴─────────────┐
            ▼                           ▼
┌──────────────────────┐    ┌──────────────────────┐
│   Android Platform   │    │  Windows Platform    │
│  (Dimmer.Droid/)    │    │  (Dimmer.WinUI/)    │
├──────────────────────┤    ├──────────────────────┤
│ • CardRenderer       │    │ • CardRenderer       │
│ • ShareService       │    │ • ShareService       │
│ • LyricsDialog       │    │ • LyricsDialog       │
│ • FileProvider       │    │ • DataTransfer API   │
└──────────────────────┘    └──────────────────────┘
```

## 📦 Implementation Details

### Core Components (Platform-Agnostic)

#### 1. ISongStoryService
**Location**: `Dimmer/Dimmer/Interfaces/ISongStoryService.cs`
- Prepares song story data
- Extracts dominant colors from cover art
- Calculates contrasting text colors

#### 2. SongStoryData Model
**Location**: `Dimmer/Dimmer/Data/ModelView/SongStoryData.cs`
```csharp
public class SongStoryData
{
    string Title, ArtistName, AlbumName
    string? CoverImagePath
    List<string> SelectedLyrics (max 5)
    Color BackgroundColor (dominant or #483D8B)
    Color TextColor (auto-calculated contrast)
    bool HasLyrics
}
```

#### 3. SongStoryService Implementation
**Location**: `Dimmer/Dimmer/Interfaces/Services/SongStoryService.cs`
**Key Features:**
- SkiaSharp-based color extraction
- Pixel sampling for performance
- WCAG contrast calculation
- Fallback to darkslateblue

### Android Implementation

#### 1. AndroidSongStoryCardRenderer
**Location**: `Dimmer.Droid/NativeServices/AndroidSongStoryCardRenderer.cs`
**Specs:**
- Card size: 1080x1920 pixels (9:16 ratio)
- Format: PNG, 100% quality
- Layout: Dynamic based on lyrics presence
- Text wrapping: Automatic with proper line breaks

**Layout Logic:**
```
WITH LYRICS:               WITHOUT LYRICS:
┌────────────┐            ┌────────────┐
│ Small Cover │           │ Large Cover │
│   (400px)   │           │   (800px)   │
├────────────┤            ├────────────┤
│   Title    │            │   Title    │
│  Artist    │            │  Artist    │
├────────────┤            │  Album     │
│  Lyric 1   │            │            │
│  Lyric 2   │            │            │
│  Lyric 3   │            │            │
│  Lyric 4   │            │            │
│  Lyric 5   │            │            │
├────────────┤            ├────────────┤
│ "Played on │            │ "Played on │
│   Dimmer"  │            │   Dimmer"  │
└────────────┘            └────────────┘
```

#### 2. LyricsSelectionDialogFragment
**Location**: `Dimmer.Droid/ViewsAndPages/NativeViews/LyricsSelectionDialogFragment.cs`
**Features:**
- RecyclerView with checkbox items
- Max 5 selection enforcement
- Live counter display
- Material Design 3 styling

#### 3. AndroidSongStoryShareService
**Location**: `Dimmer.Droid/NativeServices/AndroidSongStoryShareService.cs`
**Features:**
- Intent.ACTION_SEND for sharing
- FileProvider URI generation
- Toast notifications for feedback

#### 4. FileProvider Configuration
**Files:**
- `AndroidManifest.xml` - Provider declaration
- `Resources/xml/file_paths.xml` - Path configuration

### Windows Implementation

#### 1. WindowsSongStoryShareService
**Location**: `Dimmer.WinUI/NativeServices/WindowsSongStoryShareService.cs`
**Features:**
- SkiaSharp card rendering
- Windows DataTransferManager integration
- Segoe UI fonts for Windows-native feel

#### 2. LyricsSelectionDialog
**Location**: `Dimmer.WinUI/Views/CustomViews/WinuiViews/LyricsSelectionDialog.xaml`
**Features:**
- WinUI 3 ContentDialog
- ItemsRepeater with CheckBox items
- Max 5 selection enforcement
- Live counter display

## 🎨 Color Extraction Algorithm

```
1. Load cover image with SkiaSharp
2. Sample pixels (every nth pixel for performance)
3. Filter:
   - Skip transparent pixels (alpha < 128)
   - Skip very dark (luminance < 20)
   - Skip very light (luminance > 235)
4. Quantize colors to reduce variations
5. Find most common color
6. Calculate WCAG relative luminance
7. Select white or black for contrast
```

## 🔧 Service Registration

### Core (ServiceRegistration.cs)
```csharp
services.AddSingleton<ISongStoryService, SongStoryService>();
```

### Android (Bootstrapper.cs)
```csharp
services.AddSingleton<ISongStoryShareService, AndroidSongStoryShareService>();
```

### Windows (MauiProgram.cs)
```csharp
services.AddSingleton<ISongStoryShareService, WindowsSongStoryShareService>();
```

## 🎯 User Experience Flow

### Android
1. User plays a song
2. User taps "Share" button in Now Playing
3. If song has lyrics → Lyrics selection dialog appears
4. User selects up to 5 lines (or skips)
5. Toast: "Creating story card..."
6. Card generation (~1-2 seconds)
7. Android share sheet appears
8. User selects app to share to

### Windows
1. User plays a song
2. User clicks share button (in Now Playing or Song Detail)
3. If song has lyrics → Lyrics selection dialog appears
4. User selects up to 5 lines (or cancels)
5. Card generation (~1-2 seconds)
6. Windows share UI appears
7. User selects app to share to

## 📊 Statistics

**Total Files Created**: 12
- Core: 4 files
- Android: 4 files
- Windows: 3 files
- Documentation: 1 file

**Total Files Modified**: 5
- ServiceRegistration.cs
- Bootstrapper.cs
- MauiProgram.cs
- NowPlayingFragment.cs
- AndroidManifest.xml

**Lines of Code**: ~1,400 LOC
- Core: ~300 LOC
- Android: ~700 LOC
- Windows: ~400 LOC

## ✅ Testing Checklist

### Functional Testing
- [ ] Android: Card generation with cover art
- [ ] Android: Card generation without cover art (fallback color)
- [ ] Android: Card with lyrics (1-5 lines)
- [ ] Android: Card without lyrics (large cover)
- [ ] Android: Lyrics selection dialog (max 5 enforcement)
- [ ] Android: Share sheet appears
- [ ] Android: Shared card displays correctly in other apps
- [ ] Windows: Card generation with cover art
- [ ] Windows: Card generation without cover art
- [ ] Windows: Card with/without lyrics
- [ ] Windows: Lyrics selection dialog
- [ ] Windows: Windows share UI appears

### Visual Testing
- [ ] Color extraction produces pleasing colors
- [ ] Text is readable on all background colors
- [ ] Layout is balanced with/without lyrics
- [ ] Fonts are appropriate for platform
- [ ] Images are not distorted

### Performance Testing
- [ ] Card generation < 3 seconds on mid-range devices
- [ ] No UI blocking during generation
- [ ] Memory usage is reasonable
- [ ] Temporary files are cleaned up

## 🚀 Future Enhancements

1. **Additional Share Locations**
   - Song detail page
   - Playlist items
   - Context menus

2. **Advanced Features**
   - Custom card templates
   - Animated story cards (video)
   - Instagram/TikTok direct export
   - QR code for song sharing

3. **Customization**
   - Font selection
   - Color scheme options
   - Layout variants

4. **Social Features**
   - Share history
   - View count tracking
   - Friend recommendations

## 🐛 Known Issues & Limitations

1. **Current Limitations**
   - Maximum 5 lyrics lines (by design)
   - Color extraction is sampling-based (may not be perfect)
   - Windows requires app in foreground for sharing
   - No automatic save to gallery

2. **Potential Issues**
   - Very long song titles may wrap awkwardly
   - Some fonts may not support all Unicode characters
   - Color extraction may struggle with grayscale images

## 📝 Notes for Maintainers

1. **Card Dimensions**: The 1080x1920 size is optimized for mobile sharing. Don't change without considering implications for various platforms.

2. **Color Extraction**: The algorithm uses sampling for performance. If accuracy is more important than speed, reduce the sample rate in `ExtractDominantColorAsync`.

3. **Text Wrapping**: The `WrapText` method is shared between Android and Windows. Changes should be tested on both platforms.

4. **File Cleanup**: Cards are saved to cache/temp directories. The OS will clean these up, but consider adding manual cleanup for disk-constrained devices.

5. **Dependencies**: SkiaSharp is the only major dependency. Ensure it stays updated for security and performance improvements.

## 🎉 Credits

Implemented by: GitHub Copilot
Architecture inspired by: Spotify, Apple Music story features
Color algorithm based on: WCAG 2.1 guidelines
