using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Orleans;
using Orleans.Streams;

namespace StreamAsQueueTest
{
    [ImplicitStreamSubscription("NAMER")]
    public class TestStreamGrain : Grain, ITestGrain
    {
        private int index = 0;

        public override async Task OnActivateAsync()
        {
            var guid = this.GetPrimaryKey();
            var streamProvider = GetStreamProvider("Queue");
            var stream = streamProvider.GetStream<string>(guid, "NAMER");
            await stream.SubscribeAsync<string>(OnData, OnError, OnComplete);

            await base.OnActivateAsync();
        }

        private Task OnComplete()
        {
            Console.WriteLine("Complete => " + this.GetPrimaryKey());
            return Task.CompletedTask;
        }

        private Task OnError(Exception ex)
        {
            var color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            
            Console.WriteLine($"Stream error : {ex.Message}");
            Console.WriteLine(ex.StackTrace);

            Console.ForegroundColor = color;
            return Task.CompletedTask;
        }

        private async Task OnData(string data, StreamSequenceToken token)
        {
            await Task.Delay(1000);
            index++;

            Console.WriteLine(index + " => " + data + " : " + this.GetPrimaryKey());
        }

        public async Task Execute(string data)
        {
            await Task.Delay(1000);
            index++;

            Console.WriteLine(index + " => " + data);
        }
    }
}
