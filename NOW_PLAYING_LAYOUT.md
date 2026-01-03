# Now Playing Page - Visual Layout

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        DIMMER - Now Playing                              │
├─────────────────────────────────────────────────────────────────────────┤
│  [Blurred album art background with dark overlay]                       │
│                                                                          │
│  ┌────────────────┐    ┌──────────────────────────────────────────┐    │
│  │                │    │  Song Title (Large, Bold)                │    │
│  │   Album Art    │    └──────────────────────────────────────────┘    │
│  │   300 x 300    │                                                     │
│  │                │    ┌──────────────────────────────────────────┐    │
│  │                │    │  Lyrics (Scrollable)                     │    │
│  │                │    │                                          │    │
│  └────────────────┘    │  [Past lyric - White]                   │    │
│                        │  [Past lyric - White]                   │    │
│  ┌────────────────┐    │  >> [Current lyric - White, Bold] <<    │    │
│  │ 👤 Artist Name │    │  [Upcoming lyric - Grey 80%]            │    │
│  └────────────────┘    │  [Upcoming lyric - Grey 80%]            │    │
│                        │  [Upcoming lyric - Grey 80%]            │    │
│  ┌────────────────┐    │                                          │    │
│  │ 🎵 Genre       │    │  (Click any line to seek)               │    │
│  └────────────────┘    │                                          │    │
│                        │                                          │    │
│  ┌────────────────┐    │                                          │    │
│  │ 📅 Year        │    │                                          │    │
│  └────────────────┘    └──────────────────────────────────────────┘    │
│                                                                          │
│  ┌────────────────┐    OR if no lyrics:                                │
│  │ 📜 View Queue  │    ┌──────────────────────────────────────────┐    │
│  └────────────────┘    │         🎵 (Large Icon)                 │    │
│                        │   No lyrics available for this song      │    │
│                        │   [Search or Add Lyrics Button]          │    │
│                        └──────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────────────┘
```

## Features Demonstrated:

### Left Column (340px wide):
1. **Album Art** - 300x300 rounded corners
2. **Metadata Chips** - 4 chips stacked vertically:
   - Artist (with person icon)
   - Genre (with music icon)
   - Year (with calendar icon)
   - Queue (with list icon, clickable)

### Right Column (Remaining width):
1. **Song Title** - Large, bold text at top
2. **Lyrics Section** - Scrollable area with:
   - Past lyrics in white
   - Current lyric in white + bold
   - Upcoming lyrics in grey (80% opacity)
   - Each line is clickable to seek

### Background:
- Blurred version of album art
- Dark overlay (AA000000) for text readability

### Interaction:
- Click any lyric line → Seek to that position
- Hover over lyric → Cursor changes to hand
- Current lyric auto-scrolls to 30% from top
- "Search or Add Lyrics" button → Opens lyrics editor

## Android Enhancement

The existing Android lyrics view now also supports:
- Tap any lyric line to seek
- Visual ripple effect on tap
- Same color scheme as WinUI

## Color Scheme

- Background: Blurred album art with 30% opacity + dark overlay
- Cover border: #1F1F1F
- Chips: #2A2A2A background, white text
- Title: White (#FFFFFF)
- Past/Current lyrics: White (#FFFFFF)
- Upcoming lyrics: Grey (#808080) with 80% opacity
- No lyrics icon: Grey (#808080)
- No lyrics text: Light grey (#CCCCCC)
