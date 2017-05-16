using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SBtech.Orleans.PersistentStreams.Aerospike
{
    public class AerospikeEventPayload
    {
        public Guid StreamID { get; set; }

        public string StreamName { get; set; }

        public long Sequence { get; set; }

        public object Payload { get; set; }
        public int EventID { get; set; }
    }
}
