using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using Orleans;
using Orleans.Concurrency;
using Orleans.Runtime;

namespace TestGrains
{
    public interface IFetcher : IGrainWithStringKey
    {
        Task Check();
        Task Stop();
    }

    [Reentrant]
    public class FetchGrain : Grain, IFetcher
    {
        private IDisposable _fetcher = null;
        private GrainCancellationTokenSource _stoper = new GrainCancellationTokenSource();

        public override async Task OnActivateAsync()
        {
            _fetcher = RegisterTimer(Fetch, null, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(-1));
            await base.OnActivateAsync();
        }


        public Task Check()
        {
            return Task.CompletedTask;
        }

        public Task Stop()
        {
            _stoper.Cancel();

            return Task.CompletedTask;
        }

        private async Task Fetch(object state)
        {
            _fetcher.Dispose();

            if (!_stoper.IsCancellationRequested)
            {
                try
                {
                    await FetchData();
                }
                catch (Exception exx)
                {
                    GetLogger().Error(exx.HResult, exx.Message, exx);
                }

                _fetcher = RegisterTimer(Fetch, null, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(-1));
            }

        }

        private async Task FetchData()
        {
            var cursor = await new DataDriver().FetchInputs();

            List<Task> todo = new List<Task>((await cursor.ToListAsync()).Select(item => TasksEx.Ensure(async () =>
            {
                await GrainFactory.GetGrain<IProfileGrain>(item.Actor).AddLog();
                await new DataDriver().MarkInput(item.Id);
            })));

            await Task.WhenAll(todo);
        }
    }
}
