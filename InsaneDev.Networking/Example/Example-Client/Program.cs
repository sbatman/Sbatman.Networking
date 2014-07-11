using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using InsaneDev.Networking;
using InsaneDev.Networking.Client;

namespace Example_Client
{
    class Program
    {
        static void Main(string[] args)
        {
            InsaneDev.Networking.Client.Base client = new Base();
            client.Connect("127.0.0.1", 6789);
            while (client.IsConnected())
            {
                Packet p = new Packet(10);
                p.AddLong(DateTime.Now.Ticks);
                client.SendPacket(p);
                Thread.Sleep(20);
            }
        }
    }
}
