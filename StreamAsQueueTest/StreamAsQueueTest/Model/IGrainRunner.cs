using System.Threading.Tasks;
using Orleans;

namespace StreamAsQueueTest
{
    /// <summary>
    /// Grain interface IGrainRunner
    /// </summary>
	public interface IGrainRunner : IGrainWithGuidKey
    {
        Task Execute(int number);
    }
}
