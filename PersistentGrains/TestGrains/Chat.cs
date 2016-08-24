using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Contracts;
using DataObjects;
using Orleans;
using Orleans.Concurrency;
using Orleans.Core;
using Orleans.Providers.Streams.Common;
using Orleans.Runtime;

namespace TestGrains
{
    [Reentrant]
    public class Chat : Grain, IChat
    {
        private Guid? _chatID;
        private int _count = 0;
        private long _messageTokenId = 0;

        private Guid ChatID => (_chatID ?? (_chatID = new Guid(MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(this.GetPrimaryKeyString()))))).Value;

        public Task Join(string username, DateTime date)
        {
            _messageTokenId++;

            GetStreamProvider("CHAT_PROVIDER")
                .GetStream<ChatMessage>(ChatID, "Joins")
                .OnNextAsync(new ChatMessage {Date = date, Sender = username});

            _count++;

            return Task.CompletedTask;
        }

        public Task Leave(string username, DateTime date)
        {
            _messageTokenId++;

            GetStreamProvider("CHAT_PROVIDER")
                .GetStream<ChatMessage>(ChatID, "Leaves")
                .OnNextAsync(new ChatMessage { Date = date, Sender = username });

            _count--;

            return Task.CompletedTask;
        }

        public async Task SendMessage(string username, DateTime date, string text)
        {
            if(text == "fuck")
                throw new ArgumentException();

            _messageTokenId++;

            await GetStreamProvider("CHAT_PROVIDER")
                .GetStream<ChatMessage>(ChatID, "Messages")
                .OnNextAsync(new ChatMessage { Date = date, Sender = username, Text = text});
         
        }
    }
}
