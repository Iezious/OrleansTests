using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataObjects;
using Orleans;

namespace Contracts
{
    public interface IChatMemberObserver : IGrainObserver
    {
        void MessageRecieved(ChatMessage msg);

        void ChatJoined(ChatMessage msg);

        void ChatLeft(ChatMessage msg);
    }
}
