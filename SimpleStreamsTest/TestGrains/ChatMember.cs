using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Contracts;
using DataObjects;
using Orleans;
using Orleans.Streams;

namespace TestGrains
{
    public class ChatMember : Orleans.Grain, IChatMember
    {
        private bool _isActive;
        private DateTime _lastPing;
        private IDisposable _timer;
        private IChatMemberObserver _callbacks;
        private string _chatRoom = null;
        private Guid _chatRoomId;

        private StreamSubscriptionHandle<ChatMessage> _subscriptionMessages;
        private StreamSubscriptionHandle<ChatMessage> _subscriptionJoins;
        private StreamSubscriptionHandle<ChatMessage> _subscriptionLeaves;


        public override Task OnActivateAsync()
        {
            _timer = RegisterTimer(Invalidate, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
            _lastPing = DateTime.Now;

            return base.OnActivateAsync();
        }


        public override Task OnDeactivateAsync()
        {
            _timer.Dispose();
            _timer = null;

            return base.OnDeactivateAsync();
        }

        public async Task SendMessage(string message)
        {
            await GrainFactory.GetGrain<IChat>(_chatRoom).SendMessage(this.GetPrimaryKeyString(), DateTime.Now, message);
        }

        public async Task Join(string chat, IChatMemberObserver callbacks)
        {
            _isActive = true;
            _lastPing = DateTime.Now;
            _chatRoom = chat;
            _chatRoomId = new Guid(MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(chat)));
            _callbacks = callbacks;

            await SubscribeStreams();
            await GrainFactory.GetGrain<IChat>(_chatRoom).Join(this.GetPrimaryKeyString(), DateTime.Now);
        }

        public async Task Leave()
        {
            _isActive = false;

            await Task.WhenAll(
                _subscriptionMessages?.UnsubscribeAsync(),
                _subscriptionJoins?.UnsubscribeAsync(),
                _subscriptionLeaves?.UnsubscribeAsync());

            _subscriptionJoins = null;
            _subscriptionLeaves = null;
            _subscriptionMessages = null;

            await GrainFactory.GetGrain<IChat>(_chatRoom).Leave(this.GetPrimaryKeyString(), DateTime.Now);

            DeactivateOnIdle();
        }

        public Task Ping()
        {
            _lastPing = DateTime.Now;
            return Task.CompletedTask;
        }

        private Task Invalidate(object state)
        {
            if (_isActive && (DateTime.Now - _lastPing).TotalSeconds > 60)
            {
                return Leave();
            }

            return Task.CompletedTask;
        }

        private async Task SubscribeStreams()
        {
            _subscriptionMessages = await GetStreamProvider("CHAT_PROVIDER").GetStream<ChatMessage>(_chatRoomId, "Messages").SubscribeAsync(PrecessMessage);
            _subscriptionLeaves = await GetStreamProvider("CHAT_PROVIDER").GetStream<ChatMessage>(_chatRoomId, "Leaves").SubscribeAsync(PrecessLeave);
            _subscriptionJoins = await GetStreamProvider("CHAT_PROVIDER").GetStream<ChatMessage>(_chatRoomId, "Joins").SubscribeAsync(PrecessJoin);
        }

        private Task PrecessMessage(ChatMessage message, StreamSequenceToken token)
        {
            _callbacks.MessageRecieved(message);
            return Task.CompletedTask;
        }
        private Task PrecessJoin(ChatMessage message, StreamSequenceToken token)
        {
            _callbacks.ChatLeft(message);
            return Task.CompletedTask;
        }
        private Task PrecessLeave(ChatMessage message, StreamSequenceToken token)
        {
            _callbacks.ChatJoined(message);
            return Task.CompletedTask;
        }
    }
}
