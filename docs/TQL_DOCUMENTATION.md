# TQL (Text Query Language) - Complete Documentation

## Introduction

TQL (Text Query Language) is Dimmer's powerful, human-friendly query language for searching and filtering your music library. Unlike traditional search boxes that only match keywords, TQL allows you to build complex queries with precision and flexibility.

Think of TQL as a conversation with your music library, where you can ask specific questions like:
- "Find all rock songs from the 90s with a rating above 4 stars"
- "Show me pop music by Dua Lipa or Lady Gaga, but exclude remixes"
- "Give me 50 random high-energy tracks I haven't played recently"

## Quick Start

### Basic Searches

Search anywhere in the library:
```
love
```
Finds any song with "love" in title, artist, album, or genre.

Search specific fields:
```
artist:Drake
album:"Take Care"
title:Started
```

Combine multiple criteria:
```
artist:Drake album:"Take Care"
```
Finds all songs by Drake from the "Take Care" album.

### Common Patterns

**Find favorites:**
```
fav:true rating:5
```

**Find recent music:**
```
year:>2020
```

**Find long songs:**
```
len:>5:00
```

**Random playlist:**
```
genre:Rock random 50
```

## Syntax Reference

### Field Prefixes

Use a prefix followed by `:` to search specific fields.

| Prefix | Field | Description | Example |
|--------|-------|-------------|---------|
| `t`, `title` | Song Title | Search song titles | `t:Hello` |
| `ar`, `artist` | Artist Name | Search artist names | `ar:Drake` |
| `al`, `album` | Album Name | Search album names | `al:"Dark Side"` |
| `genre` | Genre | Search by genre | `genre:Rock` |
| `year` | Release Year | Filter by year | `year:2020` |
| `bpm` | Bitrate | Filter by bitrate | `bpm:320` |
| `len` | Duration | Filter by length | `len:>3:00` |
| `rating` | User Rating | Filter by rating (0-5) | `rating:5` |
| `fav` | Is Favorite | Filter favorites | `fav:true` |
| `lyrics` | Has Lyrics | Has any lyrics | `lyrics:true` |
| `synced` | Has Synced Lyrics | Has synced/LRC lyrics | `synced:true` |
| `any` | All Text Fields | Search everywhere | `any:"wish you were here"` |

### Operators

#### Comparison Operators (Numeric Fields)

| Operator | Meaning | Example | Description |
|----------|---------|---------|-------------|
| `:` | Equals | `year:2020` | Exact match |
| `:>` | Greater Than | `year:>2015` | After 2015 |
| `:>=` | Greater or Equal | `rating:>=4` | 4 stars or more |
| `:<` | Less Than | `bpm:<100` | Under 100 bitrate |
| `:<=` | Less or Equal | `len:<=3:00` | 3 minutes or less |
| `:-` | Range | `year:1990-1999` | Between 1990-1999 |

#### Text Operators

| Operator | Meaning | Example | Description |
|----------|---------|---------|-------------|
| `"` | Exact Phrase | `ar:"Pink Floyd"` | Exact match, case-insensitive |
| `^` | Starts With | `t:^Hello` | Title starts with "Hello" |
| `$` | Ends With | `al:$Remastered` | Album ends with "Remastered" |
| `~` | Fuzzy Match | `ar:~beatels` | Finds "Beatles" (typo-tolerant) |
| `\|` | OR | `ar:Drake\|Rihanna` | Drake OR Rihanna |

### Logical Operators

#### Include (Default Behavior)

```
artist:Drake genre:Hip-Hop
```
Finds songs that match Drake AND Hip-Hop genre.

You can explicitly use `include`:
```
include artist:Drake
```

#### Exclude

```
exclude artist:Drake
```
Excludes all Drake songs.

Or use `!` prefix:
```
artist:!Drake
```

#### Combining Include and Exclude

```
genre:Rock year:>2000 exclude artist:"Linkin Park"
```
Rock songs after 2000, but not by Linkin Park.

### Sort Directives

Control the order of results:

| Directive | Description | Example |
|-----------|-------------|---------|
| `asc` | Ascending order | `year asc` |
| `desc` | Descending order | `rating desc` |

Sort applies to the **last mentioned field**:

```
year:>2000 rating desc
```
Sorts by rating (descending).

To sort by a different field, mention it:
```
year:>2000 rating:>3 year desc
```
Sorts by year (descending).

### Limit Directives

Control how many results to return:

| Directive | Description | Example |
|-----------|-------------|---------|
| `first [n]` | Take first N results | `first 10` |
| `last [n]` | Take last N results | `last 5` |
| `random [n]` | Take N random results | `random 50` |

**Note**: Use limit directives **after** sorting to get expected results.

```
rating:5 rating desc first 20
```
Gets top 20 highest-rated 5-star songs.

## Advanced Examples

### Example 1: Building a Workout Playlist

**Goal**: High-energy songs, fast tempo, no ballads.

```
bpm:>120 genre:!ballad len:<4:00 rating:>=4 random 30
```

- `bpm:>120`: Fast tempo (>120 bitrate as proxy for high energy)
- `genre:!ballad`: Exclude ballads
- `len:<4:00`: Keep it under 4 minutes
- `rating:>=4`: Only good songs
- `random 30`: Give me 30 random tracks

### Example 2: Discovering New Music

**Goal**: Highly-rated songs I haven't played much.

```
rating:>=4 plays:<3 year:>2015 random 50
```

*(Assumes `plays` field exists)*

### Example 3: Cleaning Up the Library

**Goal**: Find songs with missing or bad metadata.

```
genre:empty rating:<2
```

- `genre:empty`: No genre set
- `rating:<2`: Low-rated (probably needs fixing)

### Example 4: Genre Deep Dive

**Goal**: Explore all rock subgenres from the 70s.

```
genre:rock year:1970-1979 year asc
```

### Example 5: Artist Comparison

**Goal**: Songs by either Led Zeppelin or Pink Floyd, but only albums from their classic period.

```
artist:"led zeppelin"|"pink floyd" year:1970-1979 album asc
```

### Example 6: Perfect Dinner Party Playlist

**Goal**: Relaxed, mature, high-quality music, avoiding explicit content.

```
genre:jazz|soul|r&b bpm:<110 rating:>=4 explicit:false random 40
```

*(Assumes `explicit` field exists)*

### Example 7: Lyrics-Based Search

**Goal**: Find songs with synced lyrics for karaoke.

```
synced:true rating:>=4 genre:pop random 20
```

### Example 8: Album-Oriented Listening

**Goal**: Find complete albums worth listening to.

```
album:"dark side of the moon" artist:"pink floyd" track asc
```

*(Assumes `track` field for track number)*

### Example 9: Time-Based Discovery

**Goal**: Rediscover music from a specific era.

```
year:2008-2012 rating:>=4 plays:<10 random 30
```

### Example 10: Complex Multi-Criteria Search

**Goal**: Ultimate 90s hip-hop playlist.

```
genre:"hip hop" year:1990-1999 rating:>=3 exclude artist:"vanilla ice" artist:!mc hammer bpm:>80 rating desc first 50
```

Breaking it down:
- `genre:"hip hop"`: Hip-hop genre
- `year:1990-1999`: The 90s
- `rating:>=3`: Decent quality
- `exclude artist:"vanilla ice"`: No Vanilla Ice
- `artist:!mc hammer`: Also no MC Hammer
- `bpm:>80`: Keep the tempo up
- `rating desc`: Best songs first
- `first 50`: Top 50 results

## Special Features

### Fuzzy Matching for Typos

Made a typo? No problem!

```
artist:~beatels
```
Finds "Beatles" even with misspelling.

### Multiple Artists

```
artist:Drake|Kendrick|Cole
```
Finds songs by Drake, Kendrick Lamar, or J. Cole.

### Exact Phrase Matching

```
album:"The Dark Side of the Moon"
```
Requires exact phrase (case-insensitive).

### Nested Logic

You can combine includes and excludes:

```
include genre:rock include year:>2000 exclude artist:nickelback
```

Equivalent to:
```
genre:rock year:>2000 artist:!nickelback
```

## Time Format

For duration (`len` field), use these formats:

| Format | Example | Meaning |
|--------|---------|---------|
| Seconds | `len:180` | 180 seconds (3 minutes) |
| MM:SS | `len:3:30` | 3 minutes 30 seconds |
| HH:MM:SS | `len:1:05:30` | 1 hour 5 minutes 30 seconds |

## Boolean Fields

For true/false fields (`fav`, `lyrics`, `synced`):

```
fav:true       # Is a favorite
fav:false      # Not a favorite
lyrics:true    # Has lyrics
synced:false   # Doesn't have synced lyrics
```

## Common Pitfalls

### 1. Sorting Confusion

**Problem**: Sort applies to last-mentioned field.

**Wrong**:
```
rating:5 year:>2010 desc
```
Sorts by year (desc), not rating.

**Right**:
```
rating:5 year:>2010 rating desc
```
Explicitly mention rating for sorting.

### 2. Spaces in Values

**Problem**: Spaces break parsing without quotes.

**Wrong**:
```
artist:Taylor Swift
```
Searches for artist "Taylor" and separate term "Swift".

**Right**:
```
artist:"Taylor Swift"
```

### 3. Order of Limit Directives

**Problem**: Limiting before sorting gives unexpected results.

**Wrong**:
```
rating desc first 10
```
Takes first 10, then sorts (only 10 items).

**Right**:
```
rating:>=1 rating desc first 10
```
Sorts all, then takes top 10.

### 4. Mixing Include/Exclude Logic

**Problem**: Confusing AND vs OR with exclusions.

**This**:
```
genre:rock artist:!metallica
```
Means: Rock songs, but not by Metallica.

**Not this**:
```
genre:rock OR artist is not Metallica
```

## Query Debugging

If your query doesn't return expected results:

1. **Start simple**: Begin with one criterion and add more.
   ```
   artist:Drake
   artist:Drake album:"Take Care"
   artist:Drake album:"Take Care" year:2011
   ```

2. **Check field names**: Ensure you're using correct prefixes.

3. **Use quotes**: For multi-word values, use quotes.

4. **Test operators**: Verify comparison operators work as expected.

5. **Check data**: Ensure your music library has the metadata you're querying.

## Future Features (Roadmap)

These features are planned or under consideration:

- **Date fields**: `added:today`, `played:this-week`
- **Play count**: `plays:>10`
- **Skip count**: `skips:<3`
- **Mood/Energy**: `mood:upbeat`, `energy:high`
- **Smart playlists**: Save queries as dynamic playlists
- **Nested queries**: `(genre:rock OR genre:metal) AND year:>2000`
- **Aggregations**: `count`, `sum`, `avg` functions

## Implementation Details

### Parser Location

The TQL parser is located in:
- **Parser**: `Dimmer/Dimmer/DimmerSearch/SemanticParser.cs`
- **Query Model**: `Dimmer/Dimmer/DimmerSearch/SemanticQuery.cs`
- **Actions**: `Dimmer/Dimmer/DimmerSearch/TQLActions/`
- **Documentation**: `Dimmer/Dimmer/DimmerSearch/TQLDoc/TqlDocumentation.cs`

### Adding New Features

To extend TQL:

1. Update `SemanticParser.cs` to recognize new syntax
2. Modify `SemanticQuery.cs` if new query structures are needed
3. Implement logic in `TQLActions/`
4. Add help documentation to `TqlDocumentation.cs`
5. Write tests in `DimmerTQLUnitTest/`

### Testing Queries

TQL has a dedicated test suite in `DimmerTQLUnitTest/` to ensure correctness.

## Comparison with Other Query Languages

| Feature | TQL | SQL WHERE | Lucene |
|---------|-----|-----------|--------|
| Human-friendly | ✅ | ❌ | ⚠️ |
| Field-specific | ✅ | ✅ | ✅ |
| Fuzzy matching | ✅ | ❌ | ✅ |
| Range queries | ✅ | ✅ | ✅ |
| Sorting | ✅ | ✅ (ORDER BY) | ✅ |
| Limiting | ✅ | ✅ (LIMIT) | ✅ |
| Boolean logic | ✅ | ✅ | ✅ |
| Nested queries | ⚠️ (planned) | ✅ | ✅ |

TQL prioritizes **ease of use** and **natural language** over SQL's expressiveness.

## Tips and Tricks

### 1. Save Common Queries

If you use a query frequently, save it as a note or bookmark:

```
# My Favorites Workout Mix
bpm:>120 rating:5 genre:rock|electronic random 30
```

### 2. Combine with Smart Playlists

Create dynamic playlists that update automatically based on queries.

### 3. Use Fuzzy Search Liberally

Don't worry about typos - fuzzy matching has your back.

### 4. Experiment with Ranges

Ranges work great for years, ratings, and durations:
```
year:2010-2020
rating:3-5
len:3:00-5:00
```

### 5. Random for Discovery

Use `random` to discover forgotten gems in your library.

## Glossary

- **Clause**: A single search criterion (e.g., `artist:Drake`)
- **Directive**: A command that modifies query behavior (e.g., `desc`, `first 10`)
- **Field**: A property of a song (e.g., title, artist, year)
- **Operator**: A symbol that modifies comparison (e.g., `>`, `~`, `|`)
- **Predicate**: A condition that evaluates to true/false for each song
- **Query**: The complete TQL search string

## Getting Help

- **In-App Help**: Type `?` or `help` in the search box (if implemented)
- **Examples**: Check `TQLDoc/TqlDocumentation.cs` for more examples
- **Issues**: Report bugs or request features on [GitHub Issues](https://github.com/YBTopaz8/Dimmer-MAUI/issues)

## Conclusion

TQL transforms music searching from a simple keyword match to a powerful, expressive conversation with your library. Master it, and you'll unlock new ways to discover, organize, and enjoy your music.

Happy querying! 🎵
