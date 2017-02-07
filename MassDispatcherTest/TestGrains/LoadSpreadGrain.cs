using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Contracts;
using Orleans;
using Orleans.Concurrency;
using Orleans.Placement;

namespace TestGrains
{
    public interface ILoadSpreader : IGrainWithIntegerKey
    {
        Task RunBatch(int[] ids, Payload data);
        Task Touch(int[] ids);
    }

    [Reentrant, RandomPlacement]
    public class LoadSpreadGrain : Grain, ILoadSpreader
    {
        public async Task Touch(int[] ids)
        {
            await Task.WhenAll(ids.Select(async (id) =>
            {
                await Task.Yield();
                await GrainFactory.GetGrain<IActionGrain>(id).Touch();
            }));
        }

        public async Task RunBatch(int[] ids, Payload data)
        {
            await Task.WhenAll(ids.Select(async (id) =>
            {
                await Task.Yield();
                await GrainFactory.GetGrain<IActionGrain>(id).Execute(data);
            }));
        }

        /*
        public async Task Touch(int[] ids)
        {
            await GrainFactory.GetGrain<IBatchWorker>(this.GetPrimaryKeyLong()).Touch(ids);
        }

        public async Task RunBatch(int[] ids, Payload data)
        {
            await GrainFactory.GetGrain<IBatchWorker>(this.GetPrimaryKeyLong()).RunBatch(ids, data);
        }*/
    }
}
