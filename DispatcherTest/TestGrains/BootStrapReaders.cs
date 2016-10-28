using System;
using System.Threading;
using System.Threading.Tasks;
using Orleans.Providers;
using Orleans.Runtime;

namespace TestGrains
{
    public class BootStrapReaders : IBootstrapProvider
    {
        private IFetcher _fetcher;
        private CancellationTokenSource _stopperTokenSource;

        public string Name => "ReadersBootstrap";

        public Task Init(string name, IProviderRuntime providerRuntime, IProviderConfiguration config)
        {
            _stopperTokenSource = new CancellationTokenSource();
            _fetcher = providerRuntime.GrainFactory.GetGrain<IFetcher>("fetcher");

            Task.Factory.StartNew(
                () => RunFetchers(_fetcher, providerRuntime, _stopperTokenSource.Token), 
                CancellationToken.None, 
                TaskCreationOptions.RunContinuationsAsynchronously, 
                TaskScheduler.Current);

            return Task.CompletedTask;
        }

        async Task RunFetchers(IFetcher fetcher, IProviderRuntime providerRuntime, CancellationToken stopperToken)
        {
            await Task.Yield();

            while (!stopperToken.IsCancellationRequested)
            {
                try
                {
                    Console.WriteLine("Fetcher checking");
                    await fetcher.Check();
                    Console.WriteLine("Fetcher checked");
                    await Task.Delay(TimeSpan.FromSeconds(20), stopperToken);

                }
                catch (Exception exx)
                {
                    providerRuntime.GetLogger("ReadersBootstrap").Error(exx.HResult, exx.Message, exx);
                }
            }
        }

        public async Task Close()
        {
            _stopperTokenSource.Cancel();
            await _fetcher.Stop();
        }
    }
}
