# In-App Feedback Board

## Overview

The In-App Feedback Board allows authenticated Dimmer users to submit bug reports and feature requests directly within the app. Non-authenticated users are directed to GitHub Issues as a fallback.

## Features

### For Authenticated Users
- **Submit Feedback**: Report bugs or request features with title, description, platform, and type
- **Browse Issues**: View all feedback submissions with filtering and sorting options
- **Search**: Find specific issues by title or description
- **Upvote**: Vote for features or bugs you want prioritized
- **Comment**: Discuss issues with other users and developers
- **Notifications**: Opt-in to receive updates when issue status changes or new comments are added
- **Track Status**: See real-time status updates (open, planned, in-progress, shipped, rejected)

### For Non-Authenticated Users
- **GitHub Fallback**: Automatic redirect to Dimmer's GitHub Issues page
- **Clear Messaging**: Users are informed they need a Dimmer account for in-app feedback

## User Interface

### Main Feedback Board
- **Dense Card Layout**: Issues displayed in cards with elevation 12 for depth
- **Status Badges**: Color-coded badges showing current issue status
- **Type Indicators**: Visual distinction between bugs (red) and features (green)
- **Filter Options**: 
  - Type: All, Bug, Feature
  - Status: All, Open, Planned, In-Progress, Shipped, Rejected
- **Sort Options**: Recent, Upvotes, Status
- **Search Bar**: Real-time search across titles and descriptions

### Submission Form
- **Duplicate Detection**: Automatically suggests similar issues while typing title
- **Required Fields**: Title, Type, Description, Platform
- **Auto-filled Data**: Platform and app version automatically attached
- **Validation**: Client-side validation before submission

### Issue Detail View
- **Full Information**: Title, description, author, platform, app version
- **Interaction Controls**: Upvote, comment, delete (for author)
- **Comment Thread**: Chronological list of all comments
- **Notification Settings**: Per-issue notification preferences
- **GitHub Link**: Quick access to view on GitHub

## Technical Architecture

### Data Models (Parse Backend)

#### FeedbackIssue
```csharp
- Title: string
- Type: string (Bug/Feature)
- Description: string
- Status: string (open/planned/in-progress/shipped/rejected)
- UpvoteCount: int
- CommentCount: int
- Platform: string
- AppVersion: string
- Author: UserModelOnline (relation)
- AuthorUsername: string
```

#### FeedbackComment
```csharp
- Issue: FeedbackIssue (relation)
- Text: string
- Author: UserModelOnline (relation)
- AuthorUsername: string
```

#### FeedbackVote
```csharp
- Issue: FeedbackIssue (relation)
- User: UserModelOnline (relation)
- UserId: string
```

#### FeedbackNotificationSettings
```csharp
- User: UserModelOnline (relation)
- Issue: FeedbackIssue (relation)
- UserId: string
- IssueId: string
- NotifyOnStatusChange: bool
- NotifyOnComment: bool
```

### Service Layer

**IFeedbackService** provides:
- Issue CRUD operations
- Voting functionality
- Comment management
- Notification preferences
- GitHub URL fallback

**ParseFeedbackService** implements all operations using Parse SDK with proper error handling and logging.

### ViewModels (MVVM Pattern)

1. **FeedbackBoardViewModel**
   - Loads and filters issues
   - Handles search
   - Manages navigation to submission/detail

2. **FeedbackSubmissionViewModel**
   - Manages form state
   - Duplicate detection
   - Issue creation

3. **FeedbackDetailViewModel**
   - Displays issue details
   - Manages upvotes and comments
   - Handles notifications

### Platform-Specific UI

#### WinUI (Windows)
- XAML-based pages with modern Fluent Design
- Custom converters for status/type colors
- Navigation via Frame
- Material card elevation: 12

#### Android
- Native Fragment-based UI with Material Design
- RecyclerView with custom adapters
- MaterialCardView with elevation 12dp
- Fragment transactions for navigation

## Color Scheme

### Status Colors
- **Open**: Blue (#3B82F6)
- **Planned**: Purple (#A855F7)
- **In-Progress**: Orange (#FB923C)
- **Shipped**: Green (#22C55E)
- **Rejected**: Red (#EF4444)

### Type Colors
- **Bug**: Red (#EF4444)
- **Feature**: Green (#22C55E)

## Navigation

### Windows
Settings Page → Feedback Button → FeedbackBoardPage

### Android
Main Activity → Settings → Feedback → FeedbackBoardFragment

## Security Considerations

1. **Authentication Required**: Only authenticated users can submit/interact
2. **Author Verification**: Users can only delete their own content
3. **Input Validation**: All inputs validated before submission
4. **Parse ACL**: User-level access control on Parse objects
5. **Rate Limiting**: Handled by Parse server configuration

## Future Enhancements

- Push notifications for status changes
- Email notifications
- Admin dashboard for managing feedback
- Analytics on popular requests
- Attachment support (screenshots, logs)
- Vote history tracking
- Feedback categories beyond Bug/Feature

## Testing

Unit tests cover:
- Data model initialization
- Constant validation
- Type/status validation
- Service interface contracts

Integration testing should verify:
- Authentication flows
- GitHub fallback
- Parse CRUD operations
- Real-time updates
- Cross-platform consistency

## GitHub Fallback

Non-authenticated users see:
```
"You need a Dimmer account to submit feedback in-app."
[Open GitHub Issues] [Cancel]
```

Clicking "Open GitHub Issues" opens: https://github.com/YBTopaz8/Dimmer-MAUI/issues
