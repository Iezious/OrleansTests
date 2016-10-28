using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orleans;

namespace Contracts
{
    public interface IChatMember : IGrainWithStringKey
    {
        Task SendMessage(string message);

        Task Join(string chat, IChatMemberObserver callbacks);

        Task Leave();

        Task Ping();
    }
}
