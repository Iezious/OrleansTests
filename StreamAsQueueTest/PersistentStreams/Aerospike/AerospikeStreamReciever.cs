using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aerospike.Client;
using Orleans.Streams;
using Newtonsoft.Json;

namespace SBtech.Orleans.PersistentStreams.Aerospike
{
    public class AerospikeStreamReciever : IQueueAdapterReceiver
    {
        private readonly AsyncClient _client;
        private readonly string _ns;
        private readonly string _collection;
        private readonly QueueId _queueId;

        private readonly List<Task> _awaitingTasks;


        public AerospikeStreamReciever(AsyncClient client, string ns, string collection, QueueId queueId)
        {
            _client = client;
            _ns = ns;
            _collection = collection;
            _queueId = queueId;
            _awaitingTasks = new List<Task>();
        }

        public Task Initialize(TimeSpan timeout)
        {
            return Task.CompletedTask;
        }

        public async Task<IList<IBatchContainer>> GetQueueMessagesAsync(int maxCount)
        {
            var key = new Key(_ns, _collection, _queueId.ToString());

            var resSize = await Execute(_client.Operate(null, CancellationToken.None, key, ListOperation.Size(AerospikeStreamConfig.BinName)));
            var total = resSize?.GetInt(AerospikeStreamConfig.BinName);

            if (total == null || total == 0) return new IBatchContainer[0];

            var resData = await Execute(_client.Operate(null, CancellationToken.None, key, ListOperation.PopRange(AerospikeStreamConfig.BinName, 0, maxCount)));

            var lst = resData.GetList(AerospikeStreamConfig.BinName).AsQueryable().Cast<string>()
                             .Select(JsonConvert.DeserializeObject<AerospikeEventPayload>)
                             .Select(pl => new AerospikeBatchContainer(pl))
                             .Cast<IBatchContainer>()
                             .ToArray();

            return lst;
        }

        public Task MessagesDeliveredAsync(IList<IBatchContainer> messages)
        {
            return Task.CompletedTask;
#if FALSE
            var key = new Key(_ns, _collection, _queueId.ToString());

            var resSize = await Execute(_client.Operate(null, CancellationToken.None, key, ListOperation.Size(AerospikeStreamConfig.BinName)));
            var total = resSize?.GetInt(AerospikeStreamConfig.BinName);

            if (total == null) return;

            if (messages.Count < total)
                await Execute(_client.Operate(null, CancellationToken.None, key, ListOperation.RemoveRange(AerospikeStreamConfig.BinName, 0, messages.Count)));
            else
                await Execute(_client.Delete(null, CancellationToken.None, key));
#endif
        }

        public async Task Shutdown(TimeSpan timeout)
        {
            try
            {
                if (_awaitingTasks.Count != 0)
                    await Task.WhenAll(_awaitingTasks);
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch (Exception)
            {
               
            }
        }

        private async Task<T> Execute<T>(Task<T> task)
        {
            try
            {
                _awaitingTasks.Add(task);
                T res = await task;
                return res;
            }
            finally
            {
                _awaitingTasks.Remove(task);
            }
        }
    }
}
