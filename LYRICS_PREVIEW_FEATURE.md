# Lyrics Preview Feature

## Overview
The lyrics preview feature allows users to preview lyrics before applying them to songs. This gives users more control over the lyrics management process and provides options to edit or timestamp lyrics before saving.

## Features

### 1. Preview Dialog
When a user searches for lyrics and clicks "View Lyrics" on any search result, a comprehensive preview dialog appears showing:
- **Song Metadata**: Track name, artist, and album
- **Lyrics Type**: Indicates whether lyrics are synced or plain
- **Duration**: Shows the duration of the track
- **Instrumental Indicator**: Warns if the track is marked as instrumental
- **Lyrics Content**: Displays the actual lyrics in tabs (Synced/Plain)

### 2. Actions Available

#### Apply
- Saves the lyrics to the song immediately
- Available for both synced and plain lyrics
- Automatically navigates back after applying

#### Edit
- Opens the lyrics in the editor
- Allows users to make modifications before saving
- Useful for correcting errors or adding custom annotations

#### Timestamp (Conditional)
- Only appears when lyrics are plain (unsynced)
- Launches the timestamping tool to add time codes to lyrics
- Uses the existing manual sync page functionality

### 3. Platform Support

#### Windows (WinUI)
- Uses ContentDialog with TabView for synced/plain lyrics
- Smooth animations and transitions
- Keyboard shortcuts supported
- Native Windows look and feel

#### Android
- Uses native AlertDialog
- Material Design styling
- Touch-optimized interface
- Consistent with Android design guidelines

## User Flow

### Standard Flow
1. User searches for lyrics using the search form
2. Search results appear in a list
3. User clicks "View Lyrics" on a result
4. Preview dialog opens showing lyrics and metadata
5. User can:
   - Click "Apply" to save immediately
   - Click "Edit" to modify before saving
   - Click "Timestamp" to add time codes (if plain lyrics)
   - Click "Close" to cancel

### Edit Flow
1. User clicks "Edit" in the preview dialog
2. Lyrics are loaded into the editor
3. User makes changes
4. User saves the edited lyrics

### Timestamp Flow
1. User clicks "Timestamp" in the preview dialog (plain lyrics only)
2. Timestamping session starts with the lyrics loaded
3. User navigates to the manual sync page
4. User adds timestamps using the sync tool
5. Synced lyrics are saved to the song

## Technical Implementation

### Windows (WinUI)
- **File**: `Dimmer.WinUI/Views/WinuiPages/SingleSongPage/SubPage/LyricsEditorPage.xaml`
- **Code-Behind**: `LyricsEditorPage.xaml.cs`
- **Key Methods**:
  - `ViewLyrics_Click`: Opens the preview dialog
  - `LyricsPreviewDialog_PrimaryButtonClick`: Handles Apply action
  - `LyricsPreviewDialog_SecondaryButtonClick`: Handles Edit action
  - `TimestampButton_Click`: Handles Timestamp action

### Android
- **File**: `Dimmer.Droid/ViewsAndPages/NativeViews/SingleSong/DownloadLyricsFragment.cs`
- **Key Methods**:
  - `ShowLyricsPreviewDialog`: Creates and shows the preview dialog
  - `ApplyLyrics`: Applies lyrics to the song
  - `LoadLyricsForEditing`: Opens editor with lyrics
  - `StartTimestampingSession`: Starts timestamping workflow

### ViewModel Commands Used
- `SelectLyricsCommand`: Applies lyrics to the song
- `LoadLyricsForEditingCommand`: Loads lyrics into editor
- `StartLyricsEditingSessionCommand`: Starts timestamping session

## Benefits

1. **Preview Before Commit**: Users can verify lyrics before saving
2. **Flexibility**: Multiple options for handling lyrics (apply/edit/timestamp)
3. **Error Prevention**: Reduces chances of saving incorrect lyrics
4. **Better UX**: Clear presentation of lyrics type and metadata
5. **Cross-Platform**: Consistent experience on Windows and Android

## Future Enhancements

Potential improvements for future versions:
- Add ability to switch between synced/plain lyrics in preview
- Support for multiple language lyrics preview
- Add lyrics quality indicators
- Preview lyrics from multiple sources side-by-side
- Add ability to merge lyrics from different sources
