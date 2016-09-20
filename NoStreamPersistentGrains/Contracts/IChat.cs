using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataObjects;
using Orleans;

namespace Contracts
{
    public interface IChat : IGrainWithStringKey
    {
        Task Join(string username, string grainid, DateTime date);

        Task Leave(string username, string grainid, DateTime date);
            
        Task SendMessage(string username, DateTime date, string text);
    }
}
