using System;
using System.Net;
using System.Threading;
using InsaneDev.Networking.Server;

namespace Example_Server
{
    class Program
    {
        private static readonly object _LockingObject = new object();
        static void Main(string[] args)
        {
            Base server = new Base();
            server.Init(new IPEndPoint(IPAddress.Any, 6789), typeof(ConnectingClient));
            server.StartListening();
            while (true) Thread.Sleep(5);
        }

        internal static void Write(string s)
        {
            lock (_LockingObject) Console.WriteLine(s);
        }
    }
}
