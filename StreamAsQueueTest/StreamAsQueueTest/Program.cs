#define STREAM
using System;
using System.Linq;
using System.Threading.Tasks;

using Orleans;
using Orleans.Runtime.Configuration;
using Orleans.Streams;

namespace StreamAsQueueTest
{
    /// <summary>
    /// Orleans test silo host
    /// </summary>
    public class Program
    {
        static void Main(string[] args)
        {
            // The Orleans silo environment is initialized in its own app domain in order to more
            // closely emulate the distributed situation, when the client and the server cannot
            // pass data via shared memory.
            AppDomain hostDomain = AppDomain.CreateDomain("OrleansHost", null, new AppDomainSetup
            {
                AppDomainInitializer = InitSilo,
                AppDomainInitializerArguments = args,
            });

            var config = ClientConfiguration.LocalhostSilo();
#if STREAM
            config.AddSimpleMessageStreamProvider("Queue", true, true, StreamPubSubType.ImplicitOnly);
#endif
            GrainClient.Initialize(config);

#if STREAM
            var stream = GrainClient.GetStreamProvider("Queue").GetStream<string>(new Guid(), "NAMER");
#endif

#if STREAM
            Enumerable.Range(0, 100).All(i =>
            {
                stream.OnNextAsync("i" + i);
                return true;
            });
#else
            var grain = GrainClient.GrainFactory.GetGrain<IGrainRunner>(new Guid());
            grain.Execute(100);
#endif

            Console.WriteLine("Orleans Silo is running.\nPress Enter to terminate...");
            Console.ReadLine();

            hostDomain.DoCallBack(ShutdownSilo);
        }

        static void InitSilo(string[] args)
        {
            hostWrapper = new OrleansHostWrapper(args);

            if (!hostWrapper.Run())
            {
                Console.Error.WriteLine("Failed to initialize Orleans silo");
            }
        }

        static void ShutdownSilo()
        {
            if (hostWrapper != null)
            {
                hostWrapper.Dispose();
                GC.SuppressFinalize(hostWrapper);
            }
        }

        private static OrleansHostWrapper hostWrapper;
    }
}
