# Song Comments, Reactions & Timestamped Notes - Implementation Documentation

## Overview

This feature adds comprehensive public and private commenting functionality to songs in Dimmer, with support for:
- Public/Private visibility toggle
- Timestamped notes that allow seeking to specific positions
- Reaction system (like, fire, heart, sad)
- Live updates via Parse Server
- Cross-platform UI (Android & WinUI)

## Architecture

### Data Layer

#### Realm Models (Local Storage)
- **UserNoteModel** (in `SongModel.cs`): Extended with new fields
  - `IsPublic`: Boolean flag for public/private
  - `TimestampMs`: Optional timestamp in milliseconds
  - `AuthorId`: Author's user ID
  - `AuthorUsername`: Author's display name
  - `ReactionsJson`: Serialized reactions dictionary

#### Parse Models (Cloud Storage)
- **SongComment** (in `DimmerLive/Models/SongComment.cs`): Full-featured comment model
  - All UserNoteModel fields plus:
  - `Reactions`: Dictionary<string, int> for reaction counts
  - `ReactionUsers`: Dictionary<string, IList<string>> to track who reacted

### Service Layer

#### ISongCommentService
Interface defining all comment operations:
- CRUD operations (Create, Read, Update, Delete)
- Reaction management (Add, Remove, Toggle)
- Live query subscriptions
- Sync between local and cloud

#### ParseSongCommentService
Implementation with Parse backend:
- Uses Parse SDK for cloud operations
- Implements live queries for real-time updates
- Handles ACL (Access Control Lists) for privacy
- Manages reaction atomicity

### ViewModel Layer

#### SongCommentsViewModel
Main ViewModel for comment management:
- Observable collections for public/private comments
- Commands for all user actions
- Integration with playback service for timestamp seeking
- Authentication state management

#### SongCommentView
View wrapper for Parse SongComment objects with computed properties

### UI Layer

#### Android
- **SongNotesRecyclerViewAdapter**: Enhanced RecyclerView adapter with:
  - Timestamp badges with click-to-seek
  - Reaction buttons with counts
  - Public/Private indicators
  - Author display
  - Edit/Delete actions
  
- **SongCommentDialogFragment**: Dialog for creating/editing comments:
  - Text input with multi-line support
  - Public/Private toggle with chips
  - Timestamp picker using current playback position
  - Clear timestamp option

#### WinUI (To Be Implemented)
- Comment view control
- Comment editor dialog
- Similar features to Android implementation

## Usage

### For Developers

#### Adding Comments to a Song View

1. **Inject the ViewModel**:
```csharp
public class MySongView
{
    private readonly SongCommentsViewModel _commentsVM;
    
    public MySongView(SongCommentsViewModel commentsVM)
    {
        _commentsVM = commentsVM;
    }
}
```

2. **Load Comments for a Song**:
```csharp
await _commentsVM.LoadCommentsForSongAsync(song);
```

3. **Subscribe to Changes**:
```csharp
_commentsVM.AllComments.CollectionChanged += (s, e) =>
{
    // Update UI
};
```

#### Creating a Comment

```csharp
_commentsVM.NewCommentText = "Great song!";
_commentsVM.NewCommentIsPublic = true;
_commentsVM.NewCommentTimestamp = 92340; // 1:32.340

await _commentsVM.CreateCommentCommand.ExecuteAsync(null);
```

#### Toggle Reaction

```csharp
await _commentsVM.ToggleReactionCommand.ExecuteAsync(
    (commentId: "abc123", reactionType: "like")
);
```

#### Seek to Timestamp

```csharp
await _commentsVM.OnTimestampClickCommand.ExecuteAsync(timestampMs);
```

### For Users

#### Creating a Comment (Android)

1. Navigate to song details
2. Tap "Add Note" button
3. Enter your comment text
4. Choose Public or Private visibility
5. (Optional) Tap "Use Current" to add a timestamp
6. Tap "Create"

#### Reacting to Comments

1. Find a comment
2. Tap any reaction button (👍, 🔥, ❤️, 😢)
3. Tap again to remove your reaction

#### Seeking via Timestamps

1. Find a comment with a timestamp badge
2. Tap the timestamp badge
3. Playback will seek to that position

## Parse Server Setup

### Database Schema

#### SongComment Class
- Fields: songId, songTitle, artistName, author (Pointer<_User>), text, timestampMs, isPublic, reactions, reactionUsers, isPinned, imagePath, audioPath, userRating, messageColor

#### Indexes (Recommended)
- `songId` + `isPublic` (compound index)
- `songId` + `timestampMs` (compound index)
- `author`

#### Class-Level Permissions (CLP)
- Get: Public can read where `isPublic == true`
- Find: Public can find where `isPublic == true`
- Create: Authenticated users only
- Update: Only author (set via ACL)
- Delete: Only author (set via ACL)

### Cloud Code Functions

#### incrementReaction
```javascript
Parse.Cloud.define("incrementReaction", async (request) => {
    const { commentId, reactionType, userId } = request.params;
    
    const query = new Parse.Query("SongComment");
    const comment = await query.get(commentId);
    
    const reactions = comment.get("reactions") || {};
    const reactionUsers = comment.get("reactionUsers") || {};
    
    // Initialize if needed
    if (!reactions[reactionType]) reactions[reactionType] = 0;
    if (!reactionUsers[reactionType]) reactionUsers[reactionType] = [];
    
    // Check if user already reacted
    if (reactionUsers[reactionType].includes(userId)) {
        throw "User already reacted with this type";
    }
    
    // Add reaction
    reactions[reactionType]++;
    reactionUsers[reactionType].push(userId);
    
    comment.set("reactions", reactions);
    comment.set("reactionUsers", reactionUsers);
    
    await comment.save(null, { useMasterKey: true });
    return comment;
});
```

Similar functions can be created for `decrementReaction` to maintain atomicity.

## Testing

### Unit Tests

Tests are in `Dimmer.Tests/SongCommentsTests.cs`:
- Timestamp formatting
- Reaction counting
- View model property binding

### Integration Testing

1. **Create a comment**:
   - Verify it appears in the list
   - Check Parse dashboard for the record

2. **Add a reaction**:
   - Click reaction button
   - Verify count increments
   - Check Parse for updated reaction data

3. **Seek via timestamp**:
   - Create timestamped comment
   - Click timestamp badge
   - Verify playback seeks correctly

4. **Public/Private filtering**:
   - Create both public and private comments
   - Verify public comments visible to all
   - Verify private comments only visible to author

## Security Considerations

1. **ACL on Comments**: Each comment has an ACL restricting write access to the author
2. **Public Read**: Only comments with `isPublic = true` have public read access
3. **Reaction Validation**: Cloud Code should validate users can't react multiple times
4. **XSS Protection**: Comment text should be sanitized on display
5. **Rate Limiting**: Consider rate limiting comment creation and reactions

## Performance Considerations

1. **Pagination**: Implement pagination for songs with many comments
2. **Caching**: Local Realm cache reduces Parse queries
3. **Live Queries**: Used selectively to avoid overwhelming the client
4. **Reaction Updates**: Consider debouncing or batching for better performance

## Future Enhancements

### Planned (Out of Scope for Initial Release)
- Threaded replies to comments
- Live typing indicators
- Moderation tooling (report, flag, hide)
- Rich text formatting
- Mentions (@username)
- Comment search
- Sort options (newest, oldest, most reactions, timestamp order)

### Possible Extensions
- Comment attachments (images, audio clips)
- Comment sharing
- Comment notifications
- Comment pinning (for song owners)
- Comment reactions with custom emojis
- Comment history/versioning

## Troubleshooting

### Comments Not Appearing
- Check Parse connection: `ParseClient.IsConnected`
- Verify user authentication: `ParseUser.CurrentUser != null`
- Check Parse dashboard for records
- Review Parse logs for errors

### Reactions Not Updating
- Verify network connectivity
- Check Parse Cloud Code is deployed
- Review reaction user list for duplicates
- Check Parse ACL permissions

### Timestamp Seeking Not Working
- Verify AudioService is initialized
- Check song is currently loaded
- Ensure timestamp is within song duration
- Review audio service logs

## API Reference

See inline XML documentation in:
- `ISongCommentService.cs`
- `SongCommentsViewModel.cs`
- `SongCommentMapper.cs`

## Contributing

When adding features to this system:
1. Update both Realm and Parse models
2. Add corresponding ViewModel properties/commands
3. Update UI for both Android and WinUI
4. Add unit tests
5. Update this documentation
6. Test with Parse backend

## License

This feature is part of Dimmer-MAUI and follows the same license terms.
