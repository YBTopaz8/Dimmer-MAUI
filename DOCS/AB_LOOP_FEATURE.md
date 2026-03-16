# A-B Loop Feature

## Overview
The A-B Loop feature allows you to loop a specific section of a song repeatedly. This is useful when you want to focus on a particular part of a song, such as:
- Learning lyrics or melodies
- Practicing along with a specific musical phrase
- Enjoying your favorite chorus or solo
- Language learning with music

## How to Use

### Basic Usage
1. **Play a song** - Start playing any song in your library
2. **Set Point A** - When you reach the start of the section you want to loop, click the "Set A" button
3. **Set Point B** - When you reach the end of the section, click the "Set B" button
4. **Loop activates** - The song will automatically loop between Point A and Point B

### Visual Indicators
- When Point A is set, the "Set A" button will have a green border
- When Point B is set, the "Set B" button will have a green border
- A status text will appear showing:
  - The time range of the loop (e.g., "01:23 - 01:45")
  - The current iteration count (e.g., "3/infinite")

### Clearing the Loop
- Click the "Clear" button to stop the loop and return to normal playback
- The loop will automatically clear when you:
  - Skip to the next song
  - Skip to the previous song
  - Select a different song to play

### Loop Count (Future Enhancement)
Currently, the loop runs infinitely until you clear it or change songs. In a future update, you may be able to specify a specific number of times to loop (e.g., loop 5 times then continue playing).

## Use Cases

### Music Practice
Set a loop around a guitar solo or drum fill that you're trying to learn, allowing you to practice along with it repeatedly without manually rewinding.

### Language Learning
Loop a verse with foreign language lyrics to help memorize pronunciation and meaning.

### Appreciation
Create a loop of your favorite 30 seconds of a song and just enjoy it on repeat.

### DJ/Production
Isolate specific sections to study song structure, transitions, or production techniques.

## Tips
- The loop will work best with sections that are at least 5-10 seconds long
- Point B must be set after Point A (later in the song)
- You can adjust the loop points while the loop is active by clicking the buttons again
- The song position updates in real-time, so you can set precise loop points

## Technical Details
- Loop detection happens with a 0.1 second tolerance to prevent rapid seeking
- Loop state is cleared when starting a new song to avoid confusion
- The feature works on all supported platforms (Windows, Android)
