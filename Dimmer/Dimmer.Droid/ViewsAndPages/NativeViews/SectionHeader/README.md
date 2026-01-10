# Android Sticky Section Headers Implementation

## Overview
This implementation adds sticky section headers to the Android songs list in the Dimmer-MAUI app. Headers adapt to the current sort mode and provide interactive menu options.

## Architecture

### Components

#### 1. Data Models
- **`SectionHeaderModel`**: Represents a section header with title, position, song count, and type
- **`SectionType`**: Enum defining header types (Alphabetical, DateBased, Shuffle, None)
- **`ListItem`**: Wrapper class that can hold either a header or a song for the adapter

#### 2. View Components
- **`SectionHeaderViewHolder`**: ViewHolder for rendering section headers
- **`SectionHeaderAdapter`**: Standalone adapter for headers (kept for flexibility, though integrated approach is used)
- **`StickyHeaderDecoration`**: ItemDecoration that makes headers stick to the top while scrolling
- **`SectionHeaderMenuBottomSheet`**: BottomSheet dialog showing actions when header is tapped

#### 3. Logic Components
- **`SectionGroupingHelper`**: Computes section headers based on query plan and sort mode
- **Modified `SongAdapter`**: Now supports multiple view types (headers and songs)
- **Modified `BaseViewModel`**: Tracks `CurrentQueryPlan` for section computation

### Data Flow

1. **Search/Sort Query** → `BaseViewModel.SearchSongForSearchResultHolder()`
2. **TQL Processing** → Creates `RealmQueryPlan` with sort descriptions
3. **Query Execution** → Updates `SearchResultsHolder` and `CurrentQueryPlan`
4. **Section Computation** → `SectionGroupingHelper.ComputeSections()` analyzes songs and query plan
5. **List Building** → `SongAdapter.RebuildListItems()` creates flat list with headers and songs
6. **Rendering** → Adapter displays both headers and songs
7. **Sticky Behavior** → `StickyHeaderDecoration` draws sticky header on scroll

## Section Grouping Logic

### Alphabetical Sections
- **Triggered by**: Sorting by `Title`, `ArtistName`, or `AlbumName`
- **Behavior**: Groups songs by first letter (A-Z, #)
- **Example**: "A", "B", "C"... for title sorting

### Date-Based Sections
- **Triggered by**: Sorting by fields containing "date", "played", "added", "modified"
- **Behavior**: Groups songs by month (YYYY-MM format)
- **Example**: "2024-01", "2024-02" for date added sorting

### Shuffle Mode
- **Triggered by**: Query plan contains `ShuffleNode`
- **Behavior**: Single static header "Shuffle"
- **Example**: All songs shown under one "Shuffle" header

### Fallback
- **Triggered by**: No sort specified or unsupported field
- **Behavior**: Single "All Songs" or "Sorted by [field]" header

## Menu Actions

When a section header is tapped, a BottomSheet menu appears with:

1. **Jump to Section Start**: Scrolls to the first song in the section
2. **Scroll to Top**: Scrolls to the top of the list
3. **Scroll to Current Song**: Jumps to currently playing song
4. **Change Sort Mode**: Shows dialog with sort options
   - Title (A-Z / Z-A)
   - Date Added (Newest / Oldest)
   - Last Played (Recent / Oldest)
5. **Exit Shuffle**: Only shown in shuffle mode, exits shuffle to default sort

## Integration Points

### HomePageFragment
```csharp
// Setup sticky decoration
var stickyDecoration = new StickyHeaderDecoration(ctx, _adapter.GetSections());
_songListRecycler.AddItemDecoration(stickyDecoration);

// Update when query plan changes
MyViewModel.WhenPropertyChange(nameof(BaseViewModelAnd.CurrentQueryPlan), vm => vm.CurrentQueryPlan)
    .Subscribe(_ => {
        stickyDecoration.UpdateSections(_adapter.GetSections());
        _songListRecycler.Invalidate();
    });
```

### SongAdapter
```csharp
// Multiple view types
public override int GetItemViewType(int position)
{
    return _listItems[position].Type == ListItem.ItemType.Header 
        ? VIEW_TYPE_HEADER 
        : VIEW_TYPE_SONG;
}

// Rebuild list on data changes
private void RebuildListItems()
{
    _sections = SectionGroupingHelper.ComputeSections(_songs, MyViewModel.CurrentQueryPlan);
    // Flatten headers + songs into _listItems
}
```

## Performance Considerations

### Optimizations
1. **Precomputed Sections**: Sections are computed once when data changes, not during scroll
2. **Efficient Sticky Rendering**: Uses single ViewHolder instance for sticky header
3. **Position Mapping**: Fast lookup between song positions and flat list positions
4. **Minimal Redraws**: Only invalidates when query plan changes

### Future Improvements
- Implement granular notify methods instead of `NotifyDataSetChanged()`
- Add section index jump list (A-Z sidebar)
- Cache section computations for identical query plans
- Add animations for header transitions

## Testing Checklist

- [ ] Headers appear for all sort modes
- [ ] Headers stick correctly under toolbar
- [ ] Shuffle mode shows single header
- [ ] Header taps open menu
- [ ] Menu actions work correctly
- [ ] Scroll performance is smooth
- [ ] Headers update on sort change
- [ ] No crashes with empty lists
- [ ] Proper handling of special characters
- [ ] Date grouping works for all date fields

## Known Limitations

1. Currently uses `NotifyDataSetChanged()` for simplicity - could be optimized
2. Date sections are monthly - could add daily/yearly options
3. No section index overlay (A-Z jump list) yet
4. Menu actions are basic - could add more contextual options

## Usage Examples

### Default Behavior
```
User opens app → Shows "All Songs" with DESC date added sort
Headers: "2024-12", "2024-11", "2024-10"...
```

### Title Sort
```
User searches: "sort:title asc"
Headers: "A", "B", "C"... "#"
```

### Shuffle
```
User searches: "shuffle 50"
Header: "Shuffle"
```

### Custom Sort
```
User searches: "genre:rock sort:played desc"
Headers: "2024-12", "2024-11"... (monthly grouping of play dates)
```

## Maintenance

### Adding New Sort Fields
1. Update `DetermineSectionType()` in `SectionGroupingHelper`
2. Add field mapping in `GetDateValueFromSong()` or `GetTextValueFromSong()`
3. Test section computation

### Customizing Header Appearance
1. Modify `SectionHeaderViewHolder.Create()`
2. Update colors, fonts, spacing as needed
3. Ensure dark mode compatibility

### Adding Menu Actions
1. Add button in `SectionHeaderMenuBottomSheet.OnCreateView()`
2. Wire up click handler
3. Pass necessary callbacks from `SongAdapter`
