# Musical Work Linking and Rendition Management

## Overview

The Musical Work feature enables you to link related songs (e.g., instrumental, vocal, piano, cello versions) under a canonical "Musical Work" entity. This provides better organization, querying capabilities, and UI presentation of renditions.

## Key Concepts

### Musical Work
A **Musical Work** represents the canonical idea of a song or composition, independent of any particular recording. It's the abstract concept of the musical piece.

### Rendition
A **Rendition** is a concrete recording or performance of a musical work (represented by `SongModel`). Each rendition can have different characteristics:
- **Instrumentation**: Piano, Cello, Acoustic Guitar, Orchestra, etc.
- **Type**: Studio, Live, Acoustic, Remix, Cover
- **Performance Style**: Instrumental, Vocal, etc.

## Architecture

### Database Models

#### MusicalWorkModel
Located in: `Dimmer/Dimmer/Data/Models/MusicalWorkModel.cs`

Core properties:
- `Title`: Canonical title of the work
- `Composer`: Primary composer or songwriter
- `CanonicalArtist`: Primary artist commonly associated with this work
- `Renditions`: Backlink to all songs linked to this work
- Aggregated statistics (play counts, popularity score)

#### SongModel Extensions
New properties added to `SongModel`:
- `MusicalWork`: Link to the parent Musical Work (optional)
- `RenditionType`: Type of rendition (e.g., "Studio", "Live", "Acoustic")
- `Instrumentation`: Specific instruments used (e.g., "Piano", "Cello")
- `IsLivePerformance`: Whether this is a live recording
- `IsRemix`: Whether this is a remix or remaster
- `IsCover`: Whether this is a cover by a different artist
- `RenditionNotes`: Additional notes about this rendition

### Service Layer

#### IMusicalWorkService
Located in: `Dimmer/Dimmer/Interfaces/Services/IMusicalWorkService.cs`

Key operations:
1. **Create Work**: `CreateWork(title, composer?, canonicalArtist?)`
2. **Link Song**: `LinkSongToWork(songId, workId)`
3. **Unlink Song**: `UnlinkSongFromWork(songId)`
4. **Query Renditions**: `GetRenditions(workId)`
5. **Filter Renditions**: `GetFilteredRenditions(workId, instrumentalOnly?, liveOnly?, instrumentation?)`
6. **Suggest Matches**: `SuggestMatchingWorks(songId)` and `SuggestMatchingSongs(workId)`
7. **Update Statistics**: `UpdateWorkStatistics(workId)`
8. **Update Metadata**: `UpdateRenditionMetadata(songId, ...)`

## Usage Examples

### Creating a Musical Work

```csharp
var workService = serviceProvider.GetRequiredService<IMusicalWorkService>();

var work = workService.CreateWork(
    title: "Bohemian Rhapsody",
    composer: "Freddie Mercury",
    canonicalArtist: "Queen"
);
```

### Linking Songs to a Work

```csharp
// Link studio version
workService.LinkSongToWork(studioVersionSongId, work.Id);

// Link live version
workService.LinkSongToWork(liveVersionSongId, work.Id);

// Update rendition metadata
workService.UpdateRenditionMetadata(
    liveVersionSongId,
    renditionType: "Live",
    isLive: true,
    notes: "Recorded at Wembley Stadium, 1986"
);
```

### Querying Renditions

```csharp
// Get all renditions
var allRenditions = workService.GetRenditions(work.Id);

// Get only instrumental versions
var instrumentalVersions = workService.GetFilteredRenditions(
    work.Id,
    instrumentalOnly: true
);

// Get only live performances
var liveVersions = workService.GetFilteredRenditions(
    work.Id,
    liveOnly: true
);

// Get piano versions specifically
var pianoVersions = workService.GetFilteredRenditions(
    work.Id,
    instrumentation: "Piano"
);
```

### Finding Suggestions

```csharp
// Find potential works for an unlinked song
var suggestions = workService.SuggestMatchingWorks(songId, maxResults: 5);

foreach (var suggestion in suggestions)
{
    Console.WriteLine($"Work: {suggestion.Work.Title}");
    Console.WriteLine($"Confidence: {suggestion.ConfidenceScore:P0}");
    Console.WriteLine($"Reason: {suggestion.Reason}");
}

// Find potential songs for a work
var songSuggestions = workService.SuggestMatchingSongs(work.Id, maxResults: 10);
```

### Updating Statistics

After adding/removing renditions or when songs are played:

```csharp
workService.UpdateWorkStatistics(work.Id);
```

This will aggregate:
- Total play count across all renditions
- Most recent play date
- Average popularity score
- Rendition count

## Suggestion Algorithm

The suggestion engine uses multiple factors to calculate similarity scores:

1. **Title Similarity (40% weight)**: 
   - Normalizes titles by removing common keywords (instrumental, live, remix, etc.)
   - Uses Levenshtein distance for fuzzy matching
   - Handles variations in parentheses and brackets

2. **Artist Match (30% weight)**:
   - Compares artist names between song and work

3. **Composer Match (20% weight)**:
   - Compares composer names when available

4. **Genre Match (10% weight)**:
   - Exact genre matching

Suggestions are returned with confidence scores and human-readable reasons.

## Database Relationships

```
MusicalWork (1) ─────< (many) SongModel
     ↑                           │
     │                           │
     │                           ↓
     └────── Backlink: Renditions
```

- **One-to-Many**: One Musical Work can have many Renditions (Songs)
- **Optional**: Songs don't need to be linked to a work
- **Backlink**: Works automatically know their renditions through Realm backlinks

## Query Performance

The implementation uses efficient indexing:
- `MusicalWorkModel.Title` is indexed for fast searches
- `MusicalWorkModel.TotalPlayCount` is indexed for sorting
- `MusicalWorkModel.LastPlayed` is indexed for recent activity queries
- `MusicalWorkModel.RenditionCount` is indexed for filtering

## Migration Strategy

The feature is designed to be backward compatible:

1. **Existing Songs**: All existing songs remain unchanged. The `MusicalWork` field is nullable.
2. **No Data Loss**: Songs can exist without being linked to any work.
3. **Manual Linking**: Users explicitly link renditions (recommended approach).
4. **Suggestion System**: Assists users in finding potential matches but never auto-commits.

## Future Enhancements

Potential improvements (not included in this implementation):

1. **UI Components**: 
   - Dedicated UI for creating and managing works
   - Rendition picker/selector
   - Visual indication of rendition types

2. **Auto-Suggestion UI**:
   - Display suggestions in the main library view
   - One-click linking from suggestions

3. **Advanced Analytics**:
   - "Most popular rendition" queries
   - Trending rendition types
   - Work-level listening habits

4. **Bulk Operations**:
   - Merge works
   - Bulk link/unlink operations
   - Import/export work definitions

5. **Metadata Enrichment**:
   - User-defined rendition tags
   - Rating per rendition type
   - Recommended renditions

## Testing

Comprehensive unit tests are provided in `Dimmer.Tests/MusicalWorkServiceTests.cs`:

- Creation and deletion of works
- Linking and unlinking songs
- Filtering renditions by various criteria
- Suggestion algorithm accuracy
- Statistics aggregation
- Edge cases (null values, empty collections, etc.)

Run tests with:
```bash
dotnet test Dimmer.Tests/Dimmer.Tests.csproj --filter "FullyQualifiedName~MusicalWorkServiceTests"
```

## Integration with Existing Features

The Musical Work feature integrates seamlessly with existing Dimmer features:

- **Play History**: Works aggregate play statistics from all renditions
- **Favorites**: Can mark works as favorite in addition to individual songs
- **Tags**: Both works and songs support tagging
- **User Notes**: Both works and songs can have user notes
- **Search**: Works are searchable by title, artist, and composer

## Best Practices

1. **Create Works First**: Before linking, create the musical work entity.
2. **Use Suggestions**: Leverage the suggestion engine to find matches.
3. **Update Metadata**: Fill in rendition metadata for better filtering.
4. **Regular Statistics Updates**: Call `UpdateWorkStatistics` periodically.
5. **Consistent Naming**: Use consistent naming for instrumentation and rendition types.
6. **Avoid Duplicates**: Check for existing works before creating new ones.

## API Reference

See the full interface documentation in:
- `Dimmer/Dimmer/Interfaces/Services/IMusicalWorkService.cs`
- `Dimmer/Dimmer/Interfaces/Services/MusicalWorkService.cs`

---

**Note**: This feature is designed to be user-driven. While the suggestion engine provides automated assistance, it never automatically creates links. This ensures data accuracy and gives users full control over their library organization.
