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
using Orleans.Providers;
using Orleans.Streams;

namespace TestGrains
{
    [Reentrant, StorageProvider(ProviderName = "CHAT_STORAGE")]
    public class ChatMember : Orleans.Grain<ChatMember.ChatMemberState>, IChatMember
    {

        public class ChatMemberState 
        {
            public DateTime LastOnline { get; set; }

            public int TotalMessages { get; set; }

            public string Chat { get; set; }

            public Guid ChatRoomId { get; set; }
        }

        private bool _isActive;
        private DateTime _lastPing;
        private IDisposable _timer;
        private IChatMemberObserver _callbacks;
        private string _chatRoom ;
        private Guid _chatRoomId;

        public override async Task OnActivateAsync()
        {
            _timer = RegisterTimer(Invalidate, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
            _lastPing = DateTime.Now;

            State = new ChatMemberState { TotalMessages = 0, LastOnline = DateTime.Now };
            await ReadStateAsync();

            _chatRoom = State.Chat;
            _chatRoomId = State.ChatRoomId;

            await Task.WhenAll((await GetStreamProvider("CHAT_PROVIDER").GetStream<ChatMessage>(_chatRoomId, "Messages").GetAllSubscriptionHandles()).NullSafe().Where(s => s != null).Select(subs => subs.ResumeAsync(PrecessMessage)));
            await Task.WhenAll((await GetStreamProvider("CHAT_PROVIDER").GetStream<ChatMessage>(_chatRoomId, "Leaves").GetAllSubscriptionHandles()).NullSafe().Where(s => s != null).Select(subs => subs.ResumeAsync(PrecessLeave)));
            await Task.WhenAll((await GetStreamProvider("CHAT_PROVIDER").GetStream<ChatMessage>(_chatRoomId, "Joins").GetAllSubscriptionHandles()).NullSafe().Where(s => s != null).Select(subs => subs.ResumeAsync(PrecessJoin)));

            await base.OnActivateAsync();
        }

        public override async Task OnDeactivateAsync()
        {
            _timer.Dispose();
            _timer = null;

            await WriteStateAsync();
            await base.OnDeactivateAsync();
        }

        public async Task SendMessage(string message)
        {
            await GrainFactory.GetGrain<IChat>(_chatRoom).SendMessage(this.GetPrimaryKeyString(), DateTime.Now, message);
            State.TotalMessages++;
            await WriteStateAsync();
        }

        public async Task Join(string chat, IChatMemberObserver callbacks)
        {
            _isActive = true;
            _lastPing = DateTime.Now;
            _chatRoom = chat;
            _chatRoomId = new Guid(MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(chat)));
            _callbacks = callbacks;

            State.LastOnline = DateTime.Now;
            State.Chat = chat;
            State.ChatRoomId = _chatRoomId;
            await WriteStateAsync();

            await SubscribeStreams();
            await GrainFactory.GetGrain<IChat>(_chatRoom).Join(this.GetPrimaryKeyString(), DateTime.Now);
        }

        public async Task Leave()
        {
            _isActive = false;

            await Task.WhenAll(
                Task.WhenAll((await GetStreamProvider("CHAT_PROVIDER").GetStream<ChatMessage>(_chatRoomId, "Messages").GetAllSubscriptionHandles()).NullSafe().Select(subs => subs.UnsubscribeAsync())),
                Task.WhenAll((await GetStreamProvider("CHAT_PROVIDER").GetStream<ChatMessage>(_chatRoomId, "Leaves").GetAllSubscriptionHandles()).NullSafe().Select(subs => subs.UnsubscribeAsync())),
                Task.WhenAll((await GetStreamProvider("CHAT_PROVIDER").GetStream<ChatMessage>(_chatRoomId, "Joins").GetAllSubscriptionHandles()).NullSafe().Select(subs => subs.UnsubscribeAsync()))
            );

            await WriteStateAsync();
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
            await GetStreamProvider("CHAT_PROVIDER").GetStream<ChatMessage>(_chatRoomId, "Messages").SubscribeAsync(PrecessMessage);
            await GetStreamProvider("CHAT_PROVIDER").GetStream<ChatMessage>(_chatRoomId, "Leaves").SubscribeAsync(PrecessLeave);
            await GetStreamProvider("CHAT_PROVIDER").GetStream<ChatMessage>(_chatRoomId, "Joins").SubscribeAsync(PrecessJoin);
        }

        private Task PrecessMessage(ChatMessage message, StreamSequenceToken token)
        {
            if(message.Text == this.GetPrimaryKeyString())
                throw new ArithmeticException();

            _callbacks.MessageRecieved(message);
            return Task.CompletedTask;
        }
        private Task PrecessJoin(ChatMessage message, StreamSequenceToken token)
        {
            _callbacks.ChatJoined(message);
            return Task.CompletedTask;
        }
        private Task PrecessLeave(ChatMessage message, StreamSequenceToken token)
        {
            _callbacks.ChatLeft(message);
            return Task.CompletedTask;
        }
    }
}
