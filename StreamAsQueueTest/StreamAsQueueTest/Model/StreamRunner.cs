using System;
using System.Linq;
using System.Threading.Tasks;
using Orleans;

namespace StreamAsQueueTest
{
    /// <summary>
    /// Grain implementation class StreamRunner.
    /// </summary>
    public class StreamRunner : Grain, IStreamRunner
    {
        public Task Execute(int number)
        {
            var stream = GetStreamProvider("Queue").GetStream<string>(new Guid(), "NAMER");
            Enumerable.Range(0, 100).All(i =>
            {
                stream.OnNextAsync("i" + i);
                return true;
            });

            return Task.CompletedTask;
        }
    }
}