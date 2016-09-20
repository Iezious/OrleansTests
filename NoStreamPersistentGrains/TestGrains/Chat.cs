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
    public class Chat : Grain<Chat.ChatState>, IChat
    {
        public class ChatState
        {
            public List<string> Subscriptions = new List<string>();
        }

        private Task SendMessage(ChatMessage message)
        {

            return Task.WhenAll(
                State.Subscriptions.Select(id =>
                     {
                         var memeber = GrainFactory.GetGrain<IChatMember>(id);
                         return memeber?.OnMessage(message);
                     })
            );
        }

        public Task Join(string username, string key, DateTime date)
        {
            if (!State.Subscriptions.Contains(username))
                State.Subscriptions.Add(username);

            return SendMessage(new ChatMessage {Action = EAction.Join, Date = date, Sender = username});
        }

        public Task Leave(string username, string key, DateTime date)
        {
            if (State.Subscriptions.Contains(username))
                State.Subscriptions.Remove(username);

            return SendMessage(new ChatMessage { Action = EAction.Leave, Date = date, Sender = username });
        }

        public Task SendMessage(string username, DateTime date, string text)
        {
            return SendMessage(new ChatMessage { Action = EAction.Message, Date = date, Sender = username, Text = text});
         
        }
    }
}
