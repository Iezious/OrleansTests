using System;
using System.Threading;
using System.Threading.Tasks;
using Contracts;
using Orleans;
using Orleans.Runtime;
using Orleans.Runtime.Configuration;
using TestGrains;

namespace HostApp
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

            var stopper = new CancellationTokenSource();

//            GrainClient.Initialize();
//            RunFetchers(stopper.Token);
            Console.WriteLine("Orleans Silo is running.\nPress Enter to terminate...");
            Console.ReadLine();
//            StopFetchers();
            hostDomain.DoCallBack(ShutdownSilo);
        }

        


        static void RunFetchers(CancellationToken stopperToken)
        {
            Task.Run(async () =>
            {
                while (!stopperToken.IsCancellationRequested)
                {
                    try
                    {
                        var fetcher = GrainClient.GrainFactory.GetGrain<IFetcher>("fetcher");
                        Console.WriteLine("Fetcher checking");
                        await fetcher.Check();
                        Console.WriteLine("Fetcher checked");
                        await Task.Delay(TimeSpan.FromSeconds(20), stopperToken);

                    }
                    catch (Exception exx)
                    {
                        Console.WriteLine(exx.Message);
                    }
                }
            });
        }
        private static void StopFetchers()
        {
            var fetcher = GrainClient.GrainFactory.GetGrain<IFetcher>("fetcher");
            fetcher.Stop();
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
