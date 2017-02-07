using System.Threading.Tasks;
using Orleans;

namespace StreamAsQueueTest
{
    public interface ITestGrain : IGrainWithGuidKey
    {
        Task Execute(string data);
    }
}