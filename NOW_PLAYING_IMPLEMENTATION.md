# Now Playing Page Implementation

## Overview
This implementation adds a dedicated "Now Playing" page for WinUI and enhances the Android lyrics view with tap-to-seek functionality, as specified in the issue.

## Files Added/Modified

### WinUI - New Files
1. **`Dimmer/Dimmer.WinUI/Views/WinuiPages/NowPlayingPage.xaml`**
   - XAML layout for the Now Playing page
   - Features:
     - 300x300 cover image on the left
     - Metadata chips (Artist, Genre, Year, Queue) below the cover
     - Scrollable lyrics section on the right
     - Blurred background using the cover image
     - "No Lyrics" placeholder with search button

2. **`Dimmer/Dimmer.WinUI/Views/WinuiPages/NowPlayingPage.xaml.cs`**
   - Code-behind implementation
   - Features:
     - Lyrics loading from `LyricsMgtFlow`
     - Click-to-seek functionality on lyric lines
     - Real-time lyric synchronization and highlighting
     - Past lyrics: white color
     - Current lyric: white and bold
     - Upcoming lyrics: grey with 0.8 opacity
     - Auto-scroll to current lyric
     - Cursor changes to hand on lyric hover

### WinUI - Modified Files
3. **`Dimmer/Dimmer.WinUI/Views/CustomViews/WinuiViews/MediaPlaybackSection.xaml`**
   - Added navigation button to access Now Playing page
   - Button uses music note icon (Glyph: &#xE8FD;)

4. **`Dimmer/Dimmer.WinUI/Views/CustomViews/WinuiViews/MediaPlaybackSection.xaml.cs`**
   - Added `ViewNowPlayingPageButton_Click` handler
   - Navigates to `NowPlayingPage` when button is clicked

### Android - Modified Files
5. **`Dimmer/Dimmer.Droid/ViewsAndPages/NativeViews/LyricsViewFragment.cs`**
   - Enhanced `LyricsAdapter` to support tap-to-seek
   - Added click listeners to each lyric line
   - Added visual ripple effect feedback on tap
   - Made lyrics clickable and focusable

## Key Features Implemented

### WinUI Now Playing Page
1. **Layout**
   - Left section: Cover art (300x300) with metadata chips below
   - Right section: Scrollable lyrics
   - Background: Blurred version of cover art

2. **Metadata Chips**
   - Artist name (with icon)
   - Genre (with icon)
   - Year (with icon)
   - Queue button (clickable, shows queue icon)

3. **Lyrics Display**
   - Synchronized with playback
   - Click any line to seek to that position
   - Color coding:
     - White: Past lyrics
     - White + Bold: Current lyric
     - Grey (80% opacity): Upcoming lyrics
   - Auto-scrolls to keep current lyric at 30% from top

4. **No Lyrics Handling**
   - Shows placeholder icon and message
   - Provides "Search or Add Lyrics" button
   - Navigates to lyrics editor page

### Android Enhancements
1. **Tap-to-Seek**
   - Each lyric line is now clickable
   - Tapping seeks to that lyric's timestamp
   - Visual ripple effect on tap

2. **Maintained Features**
   - Existing highlighting behavior
   - Smooth scrolling to current lyric
   - Blurred background

## Usage

### WinUI
1. Play a song with synced lyrics
2. Click the new music note button in the media playback control section (top bar)
3. The Now Playing page will open showing:
   - Large cover art on the left
   - Metadata chips below the cover
   - Lyrics on the right (if available)
4. Click any lyric line to seek to that position in the song

### Android
1. Navigate to the existing Lyrics View Fragment
2. Tap any lyric line to seek to that position
3. Visual ripple feedback confirms the tap

## Technical Details

### Dependencies
- Uses existing `LyricsMgtFlow` for lyrics management
- Uses existing `ImageFilterUtils.ApplyFilter` for blur effect
- Uses existing `BaseViewModelWin` for view model
- Follows MVVM pattern with `ObservableObject` and reactive subscriptions

### Observables
The page subscribes to:
- `_lyricsMgtFlow.CurrentLyricIndex` - Updates highlight as song plays
- `_viewModel.CurrentSongChanged` - Reloads page when song changes

### Navigation
- Integrated into existing WinUI Frame navigation system
- Button added to `MediaPlaybackSection` control (always visible in top bar)

## Testing Checklist

### WinUI
- [ ] Navigate to Now Playing page from media control button
- [ ] Verify cover image displays correctly
- [ ] Verify metadata chips show correct information
- [ ] Verify blurred background is applied
- [ ] Verify lyrics load and display
- [ ] Click different lyric lines and verify seeking works
- [ ] Verify lyric highlighting changes as song plays
- [ ] Test with song that has no lyrics (should show placeholder)
- [ ] Test "Search or Add Lyrics" button functionality
- [ ] Test Queue chip click behavior

### Android
- [ ] Open Lyrics View Fragment
- [ ] Tap different lyric lines
- [ ] Verify seeking to tapped position works
- [ ] Verify visual ripple effect appears on tap
- [ ] Verify existing highlighting still works

## Known Limitations
- Build requires MAUI workloads to be installed
- Queue chip click handler not fully implemented (can be extended based on existing queue functionality)
- Focus mode is implicit (the page itself is the "focus mode" - a distraction-free view)

## Future Enhancements
- Add fullscreen toggle for true "focus mode"
- Add lyrics text size controls
- Add ability to edit lyrics inline
- Add lyrics translation support
- Add sharing functionality from Now Playing page
