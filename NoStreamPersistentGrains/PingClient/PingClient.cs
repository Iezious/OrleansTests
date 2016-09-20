using System;
using System.Threading;
using System.Threading.Tasks;
using Contracts;
using Orleans;

namespace PingClient
{
    class PingClient : IPingClient
    {
        private readonly bool _wait;

        private IPingWorker _pinger;
        private Timer _timer;

        public PingClient(bool wait)
        {
            _wait = wait;
        }

        //private IPingWorker _pinger => GrainClient.GrainFactory.GetGrain<IPingWorker>("mario");

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

        public async Task Init()
        {
            if (_wait)
            {
                Console.WriteLine("Waiting for Orleans Silo to start. Press Enter to proceed...");
                Console.ReadLine();
            }

            GrainClient.Initialize();


            _pinger = GrainClient.GrainFactory.GetGrain<IPingWorker>("mario");
            await _pinger.Join(await GrainClient.GrainFactory.CreateObjectReference<IPingClient>(this));

            
            _timer = new System.Threading.Timer(async (data) =>
            {
                Console.Write("Ping");
                await Execute(async () => await _pinger.Ping(DateTime.Now.Ticks.ToString()));
            }, null, 2000, 2000);
        }

        public void Response(string data)
        {
            Console.Write("Pong : " + data);
        }

        public Task Stop()
        {
            return Execute(
                async () =>
                {
                    await _pinger?.Leave(await GrainClient.GrainFactory.CreateObjectReference<IPingClient>(this));
                });
        }
    }
}
