using System;
using System.Threading;
using System.Threading.Tasks;
using Contracts;
using DataObjects;
using Orleans;
using Orleans.Concurrency;

namespace ClientApp
{
    class ChatClient : IChatMemberObserver
    {
        private readonly string _username;
        private readonly bool _wait;
        private readonly string _chatname;
//        private IChatMember _chat;

        public ChatClient(string username, bool wait, string chatname)
        {
            this._username = username;
            this._wait = wait;
            this._chatname = chatname;
        }

        public async Task Init()
        {
            if (_wait)
            {
                Console.WriteLine("Waiting for Orleans Silo to start. Press Enter to proceed...");
                Console.ReadLine();
            }

            GrainClient.Initialize();

            var _chat = GrainClient.GrainFactory.GetGrain<IChatMember>(_username);
            await _chat.Join(_chatname, await GrainClient.GrainFactory.CreateObjectReference<IChatMemberObserver>(this));
            new Timer(DoPing, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
        }

        private async Task Execute(Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (Exception exx)
            {
                var color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(exx.Message);
                Console.ForegroundColor = color;
            }
        }

        private async void DoPing(object state)
        {
            var _chat = GrainClient.GrainFactory.GetGrain<IChatMember>(_username);
            await Execute(_chat.Ping);
        }

        public async Task Run()
        {
            while (true)
            {
                var _chat = GrainClient.GrainFactory.GetGrain<IChatMember>(_username);

                Console.WriteLine();
                Console.Write($"{_username}>");

                var line = await Task.Run(() => Console.ReadLine());

                if (line == "quit" || line == "q")
                {
                    await Exit();
                    break;
                }

                await Execute(async () =>
                {
                    await _chat.SendMessage(line);
                });
            }
        }

        private Task Exit()
        {
            var _chat = GrainClient.GrainFactory.GetGrain<IChatMember>(_username);
            return Execute(_chat.Leave);

        }

        public void MessageRecieved(ChatMessage msg)
        {
            Console.WriteLine();
            Console.WriteLine($"{msg.Sender} : {msg.Text}");

            Console.WriteLine();
            Console.Write($"{_username}>");
        }

        public void ChatJoined(ChatMessage msg)
        {
            var color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine();
            Console.WriteLine($"{msg.Sender} joined chat");
            Console.ForegroundColor = color;

            Console.WriteLine();
            Console.Write($"{_username}>");
        }

        public void ChatLeft(ChatMessage msg)
        {
            var color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine();
            Console.WriteLine($"{msg.Sender} left chat");
            Console.ForegroundColor = color;

            Console.WriteLine();
            Console.Write($"{_username}>");
        }
    }
}
