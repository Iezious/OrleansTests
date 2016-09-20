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
    [Reentrant]
    public class ChatMember : Orleans.Grain<ChatMember.ChatMemberState>, IChatMember
    {

        public class ChatMemberState 
        {
            public DateTime LastOnline { get; set; }

            public int TotalMessages { get; set; }

            public string Chat { get; set; }
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
            await WriteStateAsync();

            await GrainFactory
                .GetGrain<IChat>(_chatRoom)
                .Join(this.GetPrimaryKeyString(), IdentityString,  DateTime.Now);
        }

        public async Task Leave()
        {
            _isActive = false;

            await WriteStateAsync();
            await GrainFactory.GetGrain<IChat>(_chatRoom)
                .Leave(this.GetPrimaryKeyString(), IdentityString, DateTime.Now);

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
                return Leave();

            return Task.CompletedTask;
        }

        public Task OnMessage(ChatMessage message)
        {
            switch (message.Action)
            {
                case EAction.Join:
                    _callbacks.ChatJoined(message);
                    break;

                case EAction.Leave:
                    _callbacks.ChatLeft(message);
                    break;

                case EAction.Message:
                    _callbacks.MessageRecieved(message);
                    break;
            }

            return Task.CompletedTask;
        }
    }
}
