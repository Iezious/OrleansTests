using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SBtech.Orleans.PersistentStreams.Aerospike
{
    public static class AerospikeStreamConfig
    {
        public const string Namespace = "Namespace";
        public const string Collection = "Collection";
        public const string CacheSize = "CacheSize";
        public const string QueueCount = "QueueCount";
        public const string Hosts = "Hosts";
        public const string MaxRetries = "MaxRetries";
        public const string Timeout = "Timeout";
        public const string BinName = "Inbox";
    }
}
