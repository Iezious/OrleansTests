using System;
using System.Linq;
using System.Threading.Tasks;
using Orleans;

namespace StreamAsQueueTest
{
    /// <summary>
    /// Grain implementation class Runner.
    /// </summary>
    public class Runner : Grain, IGrainRunner
    {
        public async Task Execute(int number)
        {
            var grain = GrainFactory.GetGrain<ITestGrain>(new Guid());
            Enumerable.Range(0, 100).Select(i => grain.Execute("i" + i)).ToArray();
        }
    }
}
