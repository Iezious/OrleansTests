﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Contracts;
using Orleans;
using Orleans.Concurrency;

namespace TestGrains
{
    public interface IBatchWorker : IGrainWithIntegerKey
    {
        Task Touch(int[] ids);
        Task RunBatch(int[] ids, Payload data);
    }

    [StatelessWorker, Reentrant]
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
