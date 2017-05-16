using System;
using System.Linq;
using System.Threading.Tasks;
using Aerospike.Client;
using Orleans.Providers;
using Orleans.Providers.Streams.Common;
using Orleans.Runtime;
using Orleans.Streams;

namespace SBtech.Orleans.PersistentStreams.Aerospike
{
    public class AerospikeStreamAdapterFactory : IQueueAdapterFactory
    {
        private const int DEFAULT_CACHE_SIZE = 128;
        private const int DEFAULT_QUEUE_COUNT = 32;

        private string _providerName;
        private Logger _logger;
        private IStreamQueueMapper _mapper;
        private IQueueAdapterCache _cache;
        private IQueueAdapter _adapter;
        private IStreamFailureHandler _failureHandler;

        private AsyncClient _client;
        private IProviderConfiguration _config;


        public void Init(IProviderConfiguration config, string providerName, Logger logger, IServiceProvider serviceProvider)
        {
            _config = config;
            _providerName = providerName;
            _logger = logger;

            var cacheSize = config.GetIntProperty(AerospikeStreamConfig.CacheSize, DEFAULT_CACHE_SIZE);
            var queuesCount = config.GetIntProperty(AerospikeStreamConfig.QueueCount, DEFAULT_QUEUE_COUNT);

            _mapper = new HashRingBasedStreamQueueMapper(queuesCount, _providerName);
            _cache = new SimpleQueueAdapterCache(cacheSize, logger);

            var ashosts = config.GetProperty(AerospikeStreamConfig.Hosts, null);
            if (ashosts == null) throw new ArgumentException("Aerospike connection string is not found");

            var policy = new AsyncClientPolicy();
            policy.readPolicyDefault.maxRetries = config.GetIntProperty(AerospikeStreamConfig.MaxRetries, 1);
            policy.readPolicyDefault.retryOnTimeout = true;
            policy.readPolicyDefault.timeout = config.GetIntProperty(AerospikeStreamConfig.Timeout, 1000);

            policy.writePolicyDefault.maxRetries = config.GetIntProperty(AerospikeStreamConfig.MaxRetries, 1);
            policy.writePolicyDefault.retryOnTimeout = true;
            policy.writePolicyDefault.timeout = config.GetIntProperty(AerospikeStreamConfig.Timeout, 1000);

            _client = new AsyncClient(policy, ashosts.Split(',').Select(host => host.Split(':')).Select(hi => new Host(hi[0], hi.Length < 2 ? 3000 : int.Parse(hi[1]))).ToArray());
        }

        public Task<IQueueAdapter> CreateAdapter()
        {
            return Task.FromResult(_adapter ?? (_adapter = new AerospikeStreamAdapter(_config, _client, _mapper)));
        }

        public IQueueAdapterCache GetQueueAdapterCache()
        {
            return _cache;
        }

        public IStreamQueueMapper GetStreamQueueMapper()
        {
            return _mapper;
        }

        public Task<IStreamFailureHandler> GetDeliveryFailureHandler(QueueId queueId)
        {
            return Task.FromResult(_failureHandler ?? (_failureHandler = new AerospikeStreamFailureHandler(_logger, _client, _config)));
        }
    }
}
