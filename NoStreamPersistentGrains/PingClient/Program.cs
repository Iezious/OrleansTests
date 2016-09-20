using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PingClient
{
    class Program
    {
        static void Main(string[] args)
        {
            var client = new PingClient(!args.Contains("-nowait"));
            client.Init().Wait();

            Console.ReadLine();

            client.Stop().Wait();
        }
    }
}
