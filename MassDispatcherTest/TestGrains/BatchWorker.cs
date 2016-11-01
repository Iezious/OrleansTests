using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Contracts;
using Orleans;

namespace TestGrains
{
    public interface IBatchWorker : IGrainWithGuidKey
    {
        Task Touch(int[] ids);
        Task RunBatch(int[] ids, Payload data);
    }

    public class BatchWorker : Grain, IBatchWorker
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
    }
}
