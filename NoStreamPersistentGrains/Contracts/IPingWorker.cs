using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orleans;

namespace Contracts
{
    public interface IPingWorker : IGrainWithStringKey
    {
        Task Join(IPingClient client);
        Task Ping(string data);
        Task Leave(IPingClient client);
    }

    public interface IPingClient : IGrainObserver
    {
        void Response(string data);
    }
}


