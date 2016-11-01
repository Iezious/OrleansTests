using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Contracts;
using MongoDB.Driver;
using Orleans;
using Orleans.Concurrency;
using Orleans.Runtime;

namespace TestGrains
{
    public class ManagementGrain : Grain, IOrchestrator
    {
        private const int BATCH_SIZE = 500;
        private const int BATCH_COUNT = 200;

        public Task StartCreation(IOrchestratorCallBack onFinished)
        {
            var orleansScheduler = TaskScheduler.Current;
            var done = 0;

            Task.Run(async () =>
            {
                var callback = onFinished;

                var timer = new Stopwatch();
                timer.Start();
                try
                {
                    var todo = Enumerable
                        .Range(0, BATCH_COUNT)
                        .ToArray()
                        .Select(async st =>
                        {
                            var ids = Enumerable.Range(st * BATCH_SIZE, BATCH_SIZE).ToArray();
                            Console.WriteLine($"Enter batch {st}");
                            await (await Task.Factory.StartNew(async () => await GrainFactory.GetGrain<IBatchWorker>(Guid.NewGuid()).Touch(ids), CancellationToken.None, TaskCreationOptions.None, orleansScheduler));
                            Console.WriteLine($"Finish batch {st}");
                            //                                    Interlocked.Increment(ref done);
                            //                                    onFinished.OnProgress(done, BATCH_COUNT);

                        })
                        .ToArray();

                    await Task.WhenAll(todo);
                    timer.Stop();
                    Console.WriteLine("Finished job");

                    await Task.Factory.StartNew(() => { callback.OnComplete(timer.Elapsed, true); }, CancellationToken.None, TaskCreationOptions.None, orleansScheduler);
                }
                catch (Exception)
                {
                    timer.Stop();
                    await Task.Factory.StartNew(() => { callback.OnComplete(timer.Elapsed, false); }, CancellationToken.None, TaskCreationOptions.None, orleansScheduler);
                }
            });

            return Task.CompletedTask;
        }

        public Task StartNotification(Payload data, IOrchestratorCallBack onFinished)
        {
            var orleansScheduler = TaskScheduler.Current;
            var done = 0;

            Task.Run(async () =>
            {
                var callback = onFinished;

                var timer = new Stopwatch();
                timer.Start();
                try
                {
                    var todo = Enumerable
                        .Range(0, BATCH_COUNT)
                        .ToArray()
                        .Select(async st =>
                        {
                            var ids = Enumerable.Range(st * BATCH_SIZE, BATCH_SIZE).ToArray();
                            Console.WriteLine($"Enter batch {st}");
                            await (await Task.Factory.StartNew(async () => await GrainFactory.GetGrain<IBatchWorker>(Guid.NewGuid()).RunBatch(ids, data), CancellationToken.None, TaskCreationOptions.None, orleansScheduler));
                            Console.WriteLine($"Finish batch {st}");
                            //                                    Interlocked.Increment(ref done);
                            //                                    onFinished.OnProgress(done, BATCH_COUNT);

                        })
                        .ToArray();

                    await Task.WhenAll(todo);
                    timer.Stop();
                    Console.WriteLine("Finished job");

                    await Task.Factory.StartNew(() => { callback.OnComplete(timer.Elapsed, true); }, CancellationToken.None, TaskCreationOptions.None, orleansScheduler);
                }
                catch (Exception)
                {
                    timer.Stop();
                    await Task.Factory.StartNew(() => { callback.OnComplete(timer.Elapsed, false); }, CancellationToken.None, TaskCreationOptions.None, orleansScheduler);
                }
            });

            return Task.CompletedTask;
        }
    }
}
