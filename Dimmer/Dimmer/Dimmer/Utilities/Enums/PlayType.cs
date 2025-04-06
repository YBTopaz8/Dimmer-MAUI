using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Utilities.Enums;


/// <summary>
/// Indicates the type of play action performed.    
/// Possible VALID values for <see cref="PlayType"/>:
/// <list type="bullet">
/// <item><term>0</term><description>Play</description></item>
/// <item><term>1</term><description>Pause</description></item>
/// <item><term>2</term><description>Resume</description></item>
/// <item><term>3</term><description>Completed</description></item>
/// <item><term>4</term><description>Seeked</description></item>
/// <item><term>5</term><description>Skipped</description></item>
/// <item><term>6</term><description>Restarted</description></item>
/// <item><term>7</term><description>SeekRestarted</description></item>
/// <item><term>8</term><description>CustomRepeat</description></item>
/// <item><term>9</term><description>Previous</description></item>
/// </list>
/// </summary>
public enum PlayType
{
    Play = 0,
    Pause = 1,
    Resume = 2,
    Completed = 3,
    Seeked = 4,
    Skipped = 5,
    Restarted = 6,
    SeekRestarted = 7,
    CustomRepeat = 8,
    Previous = 9,
    LogEvent = 10,
    ChatSent = 11,
    ChatReceived = 12,
    ChatDeleted = 13,
    ChatEdited = 14,
    ChatPinned = 15,
    ChatUnpinned = 16,
    ChatLiked = 17,
    ChatUnliked = 18,
    ChatShared = 19,

    ChatUnread = 20,
    ChatRead = 21,
    ChatMentioned = 22,
    ChatUnmentioned = 23,
    ChatReplied = 24,
    ChatUnreplied = 25,
    ChatForwarded = 26,
    ChatUnforwarded = 27,
    ChatSaved = 28,
    ChatUnsaved = 29,
    ChatReported = 30,
    ChatUnreported = 31,
    ChatBlocked = 32,
    ChatUnblocked = 33,
    ChatMuted = 34,
    ChatUnmuted = 35,
    ChatPinnedMessage = 36,

}