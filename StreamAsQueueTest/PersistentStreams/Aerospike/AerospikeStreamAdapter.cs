using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aerospike.Client;
using Microsoft.SqlServer.Server;
using Neo.IronLua;
using Newtonsoft.Json;
using Orleans.Providers;
using Orleans.Providers.Streams.Common;
using Orleans.Runtime;
using Orleans.Streams;

namespace SBtech.Orleans.PersistentStreams.Aerospike
{
    public class AerospikeStreamAdapter : IQueueAdapter
    {
        private readonly string _name;
        private readonly string _namespace;
        private readonly string _collection;

        private readonly AsyncClient _client;
        private readonly IStreamQueueMapper _mapper;
        private readonly JsonSerializer _serializer;

        public string Name => _name;
        public bool IsRewindable => false;
        public StreamProviderDirection Direction => StreamProviderDirection.ReadWrite;
        
        public AerospikeStreamAdapter(IProviderConfiguration config, AsyncClient client, IStreamQueueMapper mapper)
        {
            _name = config.Name;

            _namespace = config.GetProperty(AerospikeStreamConfig.Namespace, null);
            if (_namespace == null) throw new ArgumentException("Aerospike namespace is not found");

            _collection = config.GetProperty(AerospikeStreamConfig.Collection, null);
            _client = client;
            _mapper = mapper;
            _serializer = JsonSerializer.Create();
        }

        public Task QueueMessageBatchAsync<T>(Guid streamGuid, string streamNamespace, IEnumerable<T> events, StreamSequenceToken token, Dictionary<string, object> requestContext)
        {
            var queueid = _mapper.GetQueueForStream(streamGuid, streamNamespace);
            var key = new Key(_namespace, _collection, queueid.ToString());

            var ops = ListOperation.AppendItems(AerospikeStreamConfig.BinName, 
                events.Select((ev, i) => new AerospikeEventPayload
                {
                    StreamID = streamGuid,
                    StreamName = streamNamespace,
                    Sequence = DateTime.Now.Ticks,
                    EventID = i,
                    Payload = ev
                }).Select(JsonConvert.SerializeObject).ToArray());

            return _client.Operate(null, CancellationToken.None, key, ops);
        }

        public IQueueAdapterReceiver CreateReceiver(QueueId queueId)
        {
            return new AerospikeStreamReciever(_client, _namespace, _collection, queueId);
        }
    }
}
