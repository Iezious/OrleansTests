using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aerospike.Client;
using Orleans.Providers;
using Orleans.Runtime;
using Orleans.Streams;

namespace SBtech.Orleans.PersistentStreams.Aerospike
{
    public class AerospikeStreamFailureHandler : IStreamFailureHandler
    {
        private Logger _logger;
        private AsyncClient _client;
        private string _namespace;
        private string _collection;

        public AerospikeStreamFailureHandler(Logger logger, AsyncClient client, string ns, string collection, bool shouldFaultSubsriptionOnError)
        {
            _logger = logger;
            _client = client;
            _namespace = ns;
            _collection = collection;

            ShouldFaultSubsriptionOnError = shouldFaultSubsriptionOnError;
        }

        public AerospikeStreamFailureHandler(Logger logger, AsyncClient client, IProviderConfiguration config)
        {
            _logger = logger;
            _client = client;
            ShouldFaultSubsriptionOnError = false;
        }

        public Task OnDeliveryFailure(GuidId subscriptionId, string streamProviderName, IStreamIdentity streamIdentity,
            StreamSequenceToken sequenceToken)
        {
            _logger.Error(1001, $"Delivery failed for stream {streamProviderName}:{subscriptionId} on {streamIdentity.Namespace} for message {sequenceToken}");
            return Task.CompletedTask;
        }

        public Task OnSubscriptionFailure(GuidId subscriptionId, string streamProviderName, IStreamIdentity streamIdentity,
            StreamSequenceToken sequenceToken)
        {
            _logger.Error(1001, $"Subscription failed for stream {streamProviderName}:{subscriptionId} on {streamIdentity.Namespace}");
            return Task.CompletedTask;
        }

        public bool ShouldFaultSubsriptionOnError { get; }
    }
}
