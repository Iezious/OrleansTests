using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Contracts;
using Orleans;
using Orleans.Concurrency;

namespace ClientApp
{
    class SendClientCallBack : IOrchestratorCallBack
    {
        private readonly string _operation;

        public SendClientCallBack(string operation)
        {
            _operation = operation;
            Console.WriteLine($"Operation {operation} started");
        }

        private void WriteColored(string message, ConsoleColor color)
        {
            var scolor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ForegroundColor = scolor;
        }

        public void OnComplete(TimeSpan time, bool success)
        {
            if (success)
                WriteColored($"{_operation} finised in {time}", ConsoleColor.DarkGreen);
            else
                WriteColored($"{_operation} failed in {time}", ConsoleColor.DarkRed);
        }

        public void OnProgress(int done, int total)
        {
            Console.WriteLine($"{done} of {total} done");
        }
    }

    class SendClient
    {
        private readonly string _username;
        private readonly bool _wait;
        private readonly string _chatname;
        private IOrchestrator _manager;

        public SendClient(string username, bool wait, string chatname)
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
            _manager = GrainClient.GrainFactory.GetGrain<IOrchestrator>("_manager");
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

        public async Task Run()
        {
            while (true)
            {
                Console.WriteLine();
                Console.Write("~ >");

                var line = await Task.Run(() => Console.ReadLine());

                if (line == "quit" || line == "q")
                {
                    break;
                }

                if (line == "t" || line == "touch")
                {
                    await Touch();
                }

                if (line == "w" || line == "s")
                {
                    await Work();
                }
            }
        }

        private async Task Touch()
        {
            await _manager.StartCreation(await GrainClient.GrainFactory.CreateObjectReference<IOrchestratorCallBack>(new SendClientCallBack("'check nodes'")));
        }

        private async Task Work()
        {
            var ra = new Random((int)DateTime.Now.Ticks);

            var data = new Payload
            {
                DataFieldInt = ra.Next(0, int.MaxValue),
                DataFieldDouble = ra.NextDouble()*1000000,
                DataFieldString = Encoding.ASCII.GetString(Enumerable.Range(ra.Next(100, 200), ra.Next(ra.Next(10, 1000))).Select(i => (byte) ra.Next('0', 'z')).ToArray())
            };

            await _manager.StartNotification(data, await GrainClient.GrainFactory.CreateObjectReference<IOrchestratorCallBack>(new SendClientCallBack("'exec nodes'")));
        }
    }
}
