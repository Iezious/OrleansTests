using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Contracts;
using DataObjects;
using Orleans;
using Orleans.Core;
using Orleans.Providers.Streams.Common;
using Orleans.Runtime;

namespace TestGrains
{
    public class Chat : Grain, IChat
    {
        private readonly Guid _chatID;
        private int _count = 0;
        private long _messageTokenId = 0;

        public Chat(IGrainIdentity identity, IGrainRuntime runtime) : base(identity, runtime)
        {
            _chatID = new Guid(MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(identity.PrimaryKeyString)));
        }

        public Task Join(string username, DateTime date)
        {
            _messageTokenId++;

            GetStreamProvider("CHAT_PROVIDER")
                .GetStream<ChatMessage>(_chatID, "Joins")
                .OnNextAsync(new ChatMessage {Date = date, Sender = username}, new EventSequenceToken(_messageTokenId));

            _count++;

            return Task.CompletedTask;
        }

        public Task Leave(string username, DateTime date)
        {
            _messageTokenId++;

            GetStreamProvider("CHAT_PROVIDER")
                .GetStream<ChatMessage>(_chatID, "Leaves")
                .OnNextAsync(new ChatMessage { Date = date, Sender = username }, new EventSequenceToken(_messageTokenId));

            _count--;

            return Task.CompletedTask;
        }

        public Task SendMessage(string username, DateTime date, string text)
        {
            _messageTokenId++;

            GetStreamProvider("CHAT_PROVIDER")
                .GetStream<ChatMessage>(_chatID, "Messages")
                .OnNextAsync(new ChatMessage { Date = date, Sender = username }, new EventSequenceToken(_messageTokenId));

            return Task.CompletedTask;
        }
    }
}
