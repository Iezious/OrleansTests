using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Castle.Core.Internal;
using Contracts;
using Orleans;
using Orleans.Placement;
using Orleans.Streams;

namespace TestGrains
{
    public interface IActionGrain : IGrainWithIntegerKey
    {
        Task Touch();

        Task Execute(Payload data);
    }

    [PreferLocalPlacement]
    public class ActionGrain : Grain, IActionGrain
    {
        public static Guid StreamID = new Guid("{939281DB-7922-492E-860E-5737CAF67B9E}");

        public override async Task OnActivateAsync()
        {
            await base.OnActivateAsync();
//
//            var stream = GetStreamProvider("Notifier").GetStream<Payload>(StreamID, "MyStreamNamespace");
//            var subscriptionHandles = await stream.GetAllSubscriptionHandles();
//
//            if (!subscriptionHandles.IsNullOrEmpty())
//                subscriptionHandles.ForEach(async x => await x.ResumeAsync(OnNextAsync));
//            else
//                await stream.SubscribeAsync(OnNextAsync);
        }

        private async Task OnNextAsync(Payload payload, StreamSequenceToken streamSequenceToken)
        {
            await Task.Yield();
        }

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
