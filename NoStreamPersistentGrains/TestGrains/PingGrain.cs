using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Contracts;
using Orleans;
using Orleans.Concurrency;
using Orleans.Providers;
using Orleans.Runtime;

namespace TestGrains
{
    [Serializable]
    public class PingerState
    {
        public ObserverSubscriptionManager<IPingClient> Observers { get; set; } = new ObserverSubscriptionManager<IPingClient>();
    }


    [Reentrant, StorageProvider(ProviderName = "PING_STORE")]
    public class PingGrain : Grain<PingerState>, IPingWorker
    {
        public override async Task OnActivateAsync()
        {
            await base.OnActivateAsync();
            await ReadStateAsync();
        }

        public async Task Join(IPingClient client)
        {
            State.Observers.Subscribe(client);
            await WriteStateAsync();
        }

        public async Task Leave(IPingClient client)
        {
            State.Observers.Unsubscribe(client);
            await WriteStateAsync();
        }

        public async Task Ping(string data)
        {
            await Task.Delay(100);
            State.Observers.Notify((client) =>
            {
                client.Response(data);
            });
        }
    }
}
