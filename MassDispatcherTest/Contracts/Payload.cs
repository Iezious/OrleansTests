using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts
{
    [Serializable]
    public class Payload
    {
        public int DataFieldInt { get; set; }
        public double DataFieldDouble { get; set; }
        public string DataFieldString { get; set; }
    }
}
