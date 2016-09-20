using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataObjects
{
    public enum EAction
    {
        Join,
        Leave, 
        Message
    }


    public class ChatMessage
    {
        public string Sender { get; set; }
        public EAction Action { get; set; }

        public DateTime Date { get; set; }
        public string Text { get; set; }
    }
}
