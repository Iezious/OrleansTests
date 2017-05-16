using System.Threading.Tasks;
using Orleans;

namespace StreamAsQueueTest
{
    /// <summary>
    /// Grain interface IStreamRunner
    /// </summary>
	public interface IStreamRunner : IGrainWithGuidKey
    {
        Task Execute(int nodes, int messages);
    }
}
