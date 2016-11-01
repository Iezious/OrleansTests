using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Contracts;
using Orleans;

namespace TestGrains
{
    public interface IActionGrain : IGrainWithIntegerKey
    {
        Task Touch();

        Task Execute(Payload data);
    }

    public class ActionGrain : Grain, IActionGrain
    {
        public async Task Touch()
        {
            await Task.Yield();
        }

        public async Task Execute(Payload data)
        {
            await Task.Yield();
            //            Console.WriteLine($"{this.GetPrimaryKeyLong()} : {data.DataFieldInt}");
        }
    }
}
