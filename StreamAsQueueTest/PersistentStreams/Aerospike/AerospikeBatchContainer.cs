using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orleans.Providers.Streams.Common;
using Orleans.Streams;

namespace SBtech.Orleans.PersistentStreams.Aerospike
{
    internal class AerospikeBatchContainer : IBatchContainer
    {
        private readonly AerospikeEventPayload _payload;

        public AerospikeBatchContainer(AerospikeEventPayload payload)
        {
            _payload = payload;
        }

        public Guid StreamGuid => _payload.StreamID;
        public string StreamNamespace => _payload.StreamName;
        public StreamSequenceToken SequenceToken => new EventSequenceToken(_payload.Sequence);

        public IEnumerable<Tuple<T, StreamSequenceToken>> GetEvents<T>() 
        {
            yield return new Tuple<T, StreamSequenceToken>((T)_payload.Payload, new EventSequenceToken(_payload.Sequence, _payload.EventID));
        }

        public bool ImportRequestContext()
        {
            return false;
        }

        public bool ShouldDeliver(IStreamIdentity stream, object filterData, StreamFilterPredicate shouldReceiveFunc)
        {
            return true;
        }

    }
}
