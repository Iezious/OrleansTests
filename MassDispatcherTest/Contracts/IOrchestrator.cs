using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orleans;

namespace Contracts
{
    public interface IOrchestratorCallBack : IGrainObserver
    {
        void OnComplete(TimeSpan time, bool success);
        void OnProgress(int done, int total);
    }

    public interface IOrchestrator : IGrainWithStringKey
    {
        Task StartCreation(IOrchestratorCallBack onFinished);
        Task StartNotification(Payload data, IOrchestratorCallBack onFinished);
    }
}
