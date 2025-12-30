# Custom App Icon Feature

## Overview
This feature allows users to customize the app's launcher icon on Android devices, similar to popular apps like Telegram and WhatsApp.

## How It Works

### Android Implementation
The feature uses Android's `activity-alias` mechanism to provide multiple launcher icons:

1. **Default Icon** - The standard Dimmer app icon
2. **Music Icon** - Alternative music-themed icon
3. **Vinyl Icon** - Vinyl record themed icon
4. **Headphones Icon** - Headphones themed icon

### Usage
1. Open the app and navigate to **Settings**
2. Scroll to the **Appearance & Behavior** section
3. Tap on **App Icon**
4. Select your preferred icon from the dialog
5. Restart the app to see the new icon in your launcher

## Technical Details

### Files Changed
- `AndroidManifest.xml` - Added activity-alias entries for each icon variant
- `AppSettingsService.cs` - Added AppIconPreference for storing user selection
- `AppIconManager.cs` - New utility class to manage icon switching
- `SettingsFragment.cs` - Added UI for icon selection
- Icon resources in all density folders (mdpi, hdpi, xhdpi, xxhdpi, xxxhdpi)

### How Activity-Alias Works
When a user selects an icon:
1. The app disables all other activity-alias components
2. Enables the selected activity-alias component
3. Android's launcher picks up the change and updates the icon
4. The preference is saved for persistence across app restarts

### Limitations
- Icon change requires the user to restart the app for the launcher to update
- Only works on Android (Windows implementation TBD)
- Custom user-provided icons are not supported yet (future enhancement)

## Future Enhancements
- Windows platform support
- Allow users to upload their own custom icons
- More built-in icon variants
- Dynamic icon themes based on app theme
