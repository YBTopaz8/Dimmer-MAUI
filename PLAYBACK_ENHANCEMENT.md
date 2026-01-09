# Enhanced Playback Behavior

## Overview

The playback system has been enhanced to provide intuitive, gesture-based control over the music queue. This addresses the ambiguity that occurred when playing songs from different contexts (home, albums, artists, search results, etc.).

## Core Principle

**ONE ACTIVE QUEUE** - There is always a single active playback queue. User actions mutate this queue rather than replacing it entirely, unless explicitly requested.

## User Intentions

The system now distinguishes between two different user intentions:

1. **"I want to listen to this later"** - Add to queue without interrupting current playback
2. **"I want this now"** - Stop what's playing and play this immediately

## Gesture-Based Actions

### Default Tap/Double-Click Behavior

**Intent:** "Add this to what I'm already listening to"

**What happens:**
- If playback is active:
  - Song is inserted at `currentIndex + 1` (plays next)
  - Current playback is **NOT** interrupted
  - Queue is **NOT** rebuilt
  - Playback context is maintained
  - **Special case:** If song is already in queue, jumps to it instead of adding duplicate
- If nothing is playing:
  - Builds queue from the tapped song's context
  - Starts playback normally

**Example:**
```
Current queue:
1. Drake - A (currently playing)
2. Drake - C
3. Drake - D

User taps: Rihanna - B

New queue:
1. Drake - A (currently playing)
2. Rihanna - B (inserted next)
3. Drake - C
4. Drake - D

Playback flow:
Drake A finishes → Rihanna B plays → Drake C plays → Drake D plays
```

### Long Press / Right-Click Behavior

**Intent:** Show context menu with all playback options

**Available Options:**

1. **Play Now**
   - Stops current playback
   - Clears the queue
   - Rebuilds queue from the song's context
   - Sets current index to tapped song
   - Updates playback context

2. **Play Next**
   - Same as default tap behavior
   - Inserts song after current track
   - Non-interrupting

3. **Add to Queue**
   - Appends song to the end of the queue
   - Non-interrupting

4. **View in Queue** (Android only)
   - Opens the queue view
   - Scrolls to the selected song

## Platform-Specific Implementation

### Windows
- **Double-tap:** Default "Play Next" behavior
- **Right-click:** Context menu with all options
- **Feedback:** InfoBar notification with auto-dismiss (3 seconds)

### Android
- **Tap:** Default "Play Next" behavior
- **Long press:** Bottom sheet menu with all options
- **Haptic feedback:** Vibration on long press
- **Feedback:** Toast notifications

## Queue Context Preservation

The system maintains the current playback context, which includes:
- The list of songs in the current queue
- The current playing position
- The source of the current playback (album, playlist, search, etc.)

Actions only rebuild the queue when explicitly requested (via "Play Now" option).

## Smart Same-Context Detection

When tapping a song that's already in the current queue:
- Instead of adding it again, the system jumps to that song
- This prevents duplicates and provides intuitive navigation within the current context

## User Feedback

All actions provide clear feedback to the user:

### Windows
- InfoBar notifications appear at the top of the view
- Messages include:
  - "Added [Song] to play next"
  - "Playing [Song]"
- Automatically dismiss after 3 seconds

### Android
- Toast notifications at the bottom of the screen
- Haptic feedback (vibration) for long press
- Messages include:
  - "Added [Song] to play next"
  - "Playing [Song]"
  - "Added [Song] to queue"

## Code Structure

### New Components

1. **PlaybackAction Enum** (`Dimmer/Dimmer/Utilities/Enums/PlaybackAction.cs`)
   - `PlayNext` - Insert after current (default tap)
   - `PlayNow` - Replace queue and play immediately
   - `AddToQueue` - Append to end
   - `JumpInQueue` - Navigate to song in current queue
   - `ReplaceQueue` - Build new queue from context

2. **BaseViewModel Methods**
   - `PlaySongWithActionAsync()` - Main entry point for all playback actions
   - `PlaySongNextAsync()` - Handles PlayNext action with smart detection
   - `PlaySongNowAsync()` - Handles PlayNow action with queue rebuild
   - `OnSongAddedToQueue` event - For UI feedback
   - `OnSongPlayingNow` event - For UI feedback

3. **UI Handlers**
   - Windows: `AllSongsListPage.xaml.cs` - Double-tap and right-click handlers
   - Android: `SongAdapter.cs` - Tap and long-press handlers with bottom sheet menu

## Migration Notes

### For Users
- Default behavior has changed from "replace queue" to "add next"
- To get the old "replace queue" behavior, use long press → "Play Now"

### For Developers
- Use `PlaySongWithActionAsync()` instead of `PlaySongAsync()` for new features
- Subscribe to `OnSongAddedToQueue` and `OnSongPlayingNow` events for feedback
- The existing `PlaySongAsync()` method still works for backward compatibility

## Future Enhancements

Potential improvements for future releases:
- User preference for default tap behavior
- Customizable gestures
- Queue history and undo functionality
- Drag-and-drop queue reordering (already partially implemented)
- Multi-song selection for batch operations
