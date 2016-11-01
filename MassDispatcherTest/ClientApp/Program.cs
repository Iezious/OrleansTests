using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Contracts;
using Orleans;

namespace ClientApp
{
    static class Program
    {
        private static string username = "anonimous";
        private static bool wait = true;
        private static string chatname = "common";

        static void Main(string[] args)
        {
            ParseParams(args);

            var client = new SendClient(username, wait, chatname);

            client.Init().Wait();
            client.Run().Wait();
        }

        private static void ParseParams(string[] args)
        {
            foreach (var arg in args)
            {
                if (arg == "-nowait")
                {
                    wait = false;
                    continue;
                }

                if (arg.StartsWith("chat:") && arg.Length > 5)
                {
                    chatname = arg.Substring(5);
                    continue;
                }

                username = arg;
            }
        }
    }
}
