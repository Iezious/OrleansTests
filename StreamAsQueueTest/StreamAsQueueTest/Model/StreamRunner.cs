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
        public Task Execute(int nodes, int messages)
        {
            var grainIds = Enumerable.Range(0, nodes).Select(n => Guid.NewGuid()).ToArray();
            var ra = new Random();

            Enumerable.Range(0, messages).All(i =>
            {
                var id = grainIds[ra.Next(nodes)];
                var stream = GetStreamProvider("Queue").GetStream<string>(id, "NAMER");
                stream.OnNextAsync("v" + i);
                return true;
            });

            return Task.CompletedTask;
        }
    }
}