﻿using DynamicData;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.DimmerLive.Interfaces;
public interface IChatService
{
    Task<ChatConversation?> GetGeneralChatAsync();
    IObservable<IChangeSet<ChatConversation, string>> Conversations { get; }

    // Get a live stream of messages for a specific conversation
    IObservable<IChangeSet<ChatMessage, string>> GetMessagesForConversation(ChatConversation conversation);

    // Create or find a 1-on-1 chat with another user
    Task<ChatConversation?> GetOrCreateConversationWithUserAsync(UserModelOnline otherUser);

    // Send a message
    Task SendTextMessageAsync(ChatConversation conversation, string text);

    // The key feature: Share a song
    Task ShareSongAsync(ChatConversation conversation, SongModelView song, double position);

    void StartListeners();
    void StopListeners();
}